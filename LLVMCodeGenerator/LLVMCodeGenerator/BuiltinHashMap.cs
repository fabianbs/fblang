using CompilerInfrastructure;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Utils;
using static NativeManagedContext;
namespace LLVMCodeGenerator {
    partial class LLVMCodeGenerator {
        readonly Dictionary<IType, (IntPtr, IntPtr)> slotBucketType= new Dictionary<IType, (IntPtr, IntPtr)>();

        private bool TryGetBuiltinHashMap(IType hmTy, IType keyTy, IType valTy, out IntPtr ret) {

            var hashMapTy = ctx.GetStruct(hmTy.Signature.ToString());
            types[hmTy.AsValueType()] = hashMapTy;
            types[hmTy.AsReferenceType()] = ctx.GetPointerType(hashMapTy);

            // first build the slot-type and the bucket-type
            // The slot-type will be the element-ytpe of the actual hash-table
            var slotTy = ctx.GetStruct(hmTy.Signature.ToString() + "::slotTy");
            // The bucket-type will be the element-type of the vector holding the key-value pairs
            var bucketTy = ctx.GetStruct(hmTy.Signature.ToString() + "::bucketTy");
            // struct slotTy {index, hashCode, state} where state: 0=>free, 1=>full, 2=>deleted
            ctx.CompleteStruct(slotTy, new[] { ctx.GetSizeTType(), ctx.GetSizeTType(), ctx.GetByteType() });
            bool succ = TryGetType(keyTy, out var ky) & TryGetType(valTy, out var val);
            if (succ)
                ctx.CompleteStruct(bucketTy, new[] { ky, val });
            // struct HashMap {slots, values, capacity, size, deletedCount}
            ctx.CompleteStruct(hashMapTy, new[] { ctx.GetPointerType(slotTy), ctx.GetPointerType(bucketTy), ctx.GetSizeTType(), ctx.GetSizeTType(), ctx.GetSizeTType() });
            if (hmTy.IsValueType())
                ret = hashMapTy;
            else
                ret = ctx.GetPointerType(hashMapTy);
            slotBucketType[hmTy] = (slotTy, bucketTy);
            return succ;
        }
        private bool ImplementBuiltinHashMap(IType tp, IType keyTy, IType valTy) {
            // implement all the methods

            var getHashCode = tp.Context.InstanceContext.MethodsByName("getHashCode").FirstOrDefault(x =>
                x.Arguments.Length == 0 &&
                x.ReturnType == PrimitiveType.SizeT &&
                x.Visibility == Visibility.Public
            );
            if (getHashCode is null && !tp.IsIntegerType() && !tp.IsString()) {
                $"The type {keyTy.Signature} does not provide  a publicly visible method getHashCode -> zint and can therefore not be used as key in an associative array".Report();
            }
            bool succ = TryGetType(tp.AsReferenceType(), out var ty) & TryGetType(keyTy, out var keyt) & TryGetType(valTy, out var valt);
            if (!succ)
                return false;
            IMethod ctor1;
            succ &= ImplementBuiltinHashMapCtor1(ctor1 = tp.Context.InstanceContext.MethodsByName("ctor").First(x => x.Arguments.Length == 1), tp, ty);
            succ &= ImplementBuiltinHashMapCtor0(tp.Context.InstanceContext.MethodsByName("ctor").First(x => x.Arguments.Length == 0), ctor1, tp, ty);
            succ &= ImplementBuiltinHashMapClear(tp.Context.InstanceContext.MethodsByName("clear").First(), tp, ty);

            var search = ImplementBuiltinHashMapSearch(tp, ty, keyt, valt, keyTy);
            tp.OverloadsOperator(OverloadableOperator.Indexer, out var indexer);
            succ &= ImplementBuiltinHashMapOperatorIndexerGet((IMethod) indexer.First(x => x.Arguments.Length == 1), tp, ty, keyt, valt, search, getHashCode);
            //TODO more methods
            throw new NotImplementedException();
        }
        private IntPtr GetHashCodeFrom(IMethod getHashCode, IType tp, IntPtr obj, IntPtr irb, IntPtr fn) {
            if (getHashCode is null) {
                if (tp.IsIntegerType()) {
                    return ctx.ForceCast(obj, ctx.GetSizeTType(), true, irb);
                }
                else {
                    var ghc = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.str_hash);
                    var strVal = ctx.ExtractValue(obj, 0, irb);
                    var strLen = ctx.ExtractValue(obj, 1, irb);
                    return ctx.GetCall(ghc, new[] { strVal, strLen }, irb);
                }
            }
            else {
                TryGetMethod(getHashCode, out var ghc);
                if (!tp.IsNotNullable()) {
                    var currBB = ctx.GetCurrentBasicBlock(irb);
                    var notNull = new BasicBlock(ctx, "notNull", fn);
                    var nullMerge = new BasicBlock(ctx, "nullMerge", fn);
                    var nullCheck = ctx.IsNotNull(obj, irb);
                    ctx.ConditionalBranch(notNull, nullMerge, nullCheck, irb);

                    ctx.ResetInsertPoint(notNull, irb);
                    var hash = ctx.GetCall(ghc, new[] { obj }, irb);
                    ctx.Branch(nullMerge, irb);
                    ctx.ResetInsertPoint(nullMerge, irb);
                    var ret = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
                    ret.AddMergePoint(ctx.GetIntSZ(0), currBB);
                    ret.AddMergePoint(hash, notNull);
                    return ret;
                }
                else
                    return ctx.GetCall(ghc, new[] { obj }, irb);
            }
        }
        private bool ImplementBuiltinHashMapOperatorIndexerGet(IMethod met, IType tp, IntPtr ty, IntPtr keyTy, IntPtr valTy, IntPtr search, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetPointerType(valTy),
                new[]{ ty, keyTy },
                new[]{ "this", "key" },
                false);
            methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var ky = ctx.GetArgument(fn, 1);
                var hashCode = GetHashCodeFrom(getHashCode,tp, ky, irb, fn);
                var thisptr = ctx.GetArgument(fn,0);
                var slotPtr = ctx.GetCall(search, new[]{ thisptr, ky, hashCode}, irb);
                var state = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 2u}, irb);
                var isFull = ctx.CompareOp(state, ctx.GetInt8(1), (sbyte)'!', true, true, irb);
                var full = new BasicBlock(ctx, "full", fn);
                var notFull = new BasicBlock(ctx, "notFull", fn);
                ctx.ConditionalBranch(full, notFull, isFull, irb);

                ctx.ResetInsertPoint(notFull, irb);
                var throwFn = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.throwException);
                var msg = ctx.GetString("Key not found in associated array", irb);
                var msgVal = ctx.ExtractValue(msg, 0, irb);
                var msgLen = ctx.ExtractValue(msg, 1, irb);
                ctx.GetCall(throwFn, new[] { msgVal, msgLen }, irb);
                ctx.Unreachable(irb);

                ctx.ResetInsertPoint(full, irb);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u},irb);
                var ret = ctx.GetElementPtr(buckets, new[]{ ctx.LoadFieldConstIdx(slotPtr, new[] { 0u, 0u},irb), ctx.GetInt32(1)},irb);
                ctx.ReturnValue(ret, irb);
            }
            return true;
        }

        private IntPtr CompareEQ(IType tp, IntPtr obj1, IntPtr obj2, IntPtr irb) {
            // operator==
            IMethod equalsMet=null;
            if (tp.OverloadsOperator(OverloadableOperator.Equal, out var ov, CompilerInfrastructure.Contexts.SimpleMethodContext.VisibleMembers.Instance)) {
                equalsMet = semantics.BestFittingMethod(default, ov, new[] { tp }, PrimitiveType.Bool, new CompilerInfrastructure.Utils.ErrorBuffer());
            }
            if (equalsMet is null || equalsMet.IsError()) {
                if (tp.OverloadsOperator(OverloadableOperator.Equal, out var ovStat, CompilerInfrastructure.Contexts.SimpleMethodContext.VisibleMembers.Static)) {
                    equalsMet = semantics.BestFittingMethod(default, ovStat, new[] { tp, tp }, PrimitiveType.Bool, new CompilerInfrastructure.Utils.ErrorBuffer());
                }
            }
            if (equalsMet is null || equalsMet.IsError())
                return ctx.CompareOp(obj1, obj2, (sbyte) '!', true, false, irb);
            else {
                TryGetMethod(equalsMet, out var met);
                return ctx.GetCall(met, new[] { obj1, obj2 }, irb);
            }
        }
        private IntPtr ImplementBuiltinHashMapSearch(IType tp, IntPtr ty, IntPtr keyTy, IntPtr valTy, IType keyTp) {
            var irb = IntPtr.Zero;
            var (slotTy, bucketTy) = slotBucketType[tp];
            var fn = ctx.DeclareFunction(tp.FullName()+".search",
                ctx.GetPointerType(slotTy),
                new[]{ ty, keyTy, ctx.GetSizeTType() },
                new[]{ "this", "key", "hashCode" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var cap = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 2u}, irb);
                var hc = ctx.ArithmeticBinOp(ctx.GetArgument(fn,2),cap,(sbyte)'%', true, irb);
                var firstLoop = new BasicBlock(ctx, "firstLoop", fn);
                var firstLoopEnd = new BasicBlock(ctx, "firstLoopEnd", fn);
                var secondLoop = new BasicBlock(ctx, "secondLoop", fn);
                var secondLoopEnd = new BasicBlock(ctx, "secondLoopEnd", fn);
                var slots = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 0u},irb);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u}, irb);

                var hashCode = ctx.GetArgument(fn, 2);

                void Loop(IntPtr indexStart, IntPtr indexEnd, IntPtr loopBlock, IntPtr loopEnd) {
                    var iLTcap = ctx.CompareOp(indexStart, indexEnd, (sbyte)'<',false,true, irb);
                    ctx.ConditionalBranch(loopBlock, loopEnd, iLTcap, irb);

                    ctx.ResetInsertPoint(loopBlock, irb);
                    var i = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
                    i.AddMergePoint(indexStart, entry);


                    var state = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(2) }, irb);
                    var isStateFree = ctx.CompareOp(state, ctx.GetInt8(1), (sbyte)'!',true, true, irb);
                    var stateFree = new BasicBlock(ctx, "stateFree",fn);
                    var stateNotFree = new BasicBlock(ctx, "stateNotFree",fn);
                    var stateFreeMerge = new BasicBlock(ctx, "stateFreeMerge", fn);
                    ctx.ConditionalBranch(stateFree, stateNotFree, isStateFree, irb);

                    ctx.ResetInsertPoint(stateNotFree, irb);
                    var slotHashcode = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(1)},irb);
                    var areHashCodesEqual = ctx.CompareOp(slotHashcode, hashCode, (sbyte)'!',true, true, irb);
                    var hashCodesEqual = new BasicBlock(ctx, "hashCodesEqual", fn);
                    ctx.ConditionalBranch(hashCodesEqual, stateFreeMerge, areHashCodesEqual, irb);

                    ctx.ResetInsertPoint(hashCodesEqual, irb);
                    // buckets[slots[i].index].key
                    var bucketKey = ctx.LoadField(buckets, new[]{ ctx.LoadField(slots, new[] { i, ctx.GetInt32(0)},irb), ctx.GetInt32(0)},irb);
                    var areKeysEqual = CompareEQ(keyTp, bucketKey, ctx.GetArgument(fn, 1),irb);
                    var keysEqual = new BasicBlock(ctx, "keysEqual", fn);
                    ctx.ConditionalBranch(keysEqual, stateFreeMerge, areKeysEqual, irb);

                    ctx.ResetInsertPoint(keysEqual, irb);
                    ctx.ReturnValue(ctx.GetElementPtr(slots, new[] { (IntPtr) i }, irb), irb);

                    ctx.ResetInsertPoint(stateFree, irb);
                    ctx.ReturnValue(ctx.GetElementPtr(slots, new[] { (IntPtr) i }, irb), irb);

                    ctx.ResetInsertPoint(stateFreeMerge, irb);
                    var newI = ctx.ArithmeticBinOp(i, ctx.GetIntSZ(1), (sbyte)'+', true, irb);
                    i.AddMergePoint(newI, stateFreeMerge);
                    var doLoop = ctx.CompareOp(newI, indexEnd, (sbyte)'<',false, true, irb);
                    ctx.ConditionalBranch(loopBlock, loopEnd, doLoop, irb);
                }

                Loop(hc, cap, firstLoop, firstLoopEnd);
                ctx.ResetInsertPoint(firstLoopEnd, irb);
                Loop(ctx.GetIntSZ(0), hc, secondLoop, secondLoopEnd);
                ctx.ResetInsertPoint(secondLoopEnd, irb);
                // we will never reach the end of the second loop
                ctx.Unreachable(irb);
            }
            return fn;
        }
        private bool ImplementBuiltinHashMapClear(IMethod met, IType tp, IntPtr ty) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetVoidType(),
                new[]{ ty },
                new[]{ "this" },
                false);
            methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var (slotTy, bucketTy) = slotBucketType[tp];
                var initCap = ctx.GetArgument(fn, 1);
                var initSlots = MallocCArray(slotTy, initCap, irb);
                var initBuckets = MallocCArray(bucketTy, initCap, irb);
                var thisptr = ctx.GetArgument(fn, 0);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 0u }, initSlots, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 1u }, initBuckets, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 2u }, initCap, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 3u }, ctx.GetIntSZ(0), irb);// size = 0
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 4u }, ctx.GetIntSZ(0), irb);// deletedCount = 0
                ctx.ReturnVoid(irb);
            }
            return true;
        }

        private bool ImplementBuiltinHashMapCtor1(IMethod met, IType tp, IntPtr ty) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetVoidType(),
                new[]{ ty, ctx.GetSizeTType() },
                new[]{ "this", "initialCapacity" },
                false);
            methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var (slotTy, bucketTy) = slotBucketType[tp];
                var initCap = ctx.GetArgument(fn, 1);
                var initSlots = MallocCArray(slotTy, initCap, irb);
                var initBuckets = MallocCArray(bucketTy, initCap, irb);
                var thisptr = ctx.GetArgument(fn, 0);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 0u }, initSlots, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 1u }, initBuckets, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 2u }, initCap, irb);
                // rest is zero-initialized
                ctx.ReturnVoid(irb);
            }
            return true;
        }

        private IntPtr MallocCArray(IntPtr itemTy, IntPtr arrLen, IntPtr irb) {
            var itemTySz = ctx.GetSizeOf(itemTy);
            var sz = ctx.ArithmeticBinOp(itemTySz, arrLen,(sbyte)'*',true,irb);

            var mallocFn = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.gc_new);
            var ret =  ctx.GetCall(mallocFn, new[] { sz }, irb);
            return ctx.ForceCast(ret, ctx.GetPointerType(itemTy), false, irb);
        }
        private bool ImplementBuiltinHashMapCtor0(IMethod met, IMethod ctor1, IType tp, IntPtr ty) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetVoidType(),
                new[]{ ty },
                new[]{ "this" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var ctor1Fn = methods[ctor1];
                ctx.GetCall(ctor1Fn, new[] { ctx.GetArgument(fn, 0), ctx.GetIntSZ(5) }, irb);
                // rest is zero-initialized
                ctx.ReturnVoid(irb);
            }
            return true;
        }
    }
}
