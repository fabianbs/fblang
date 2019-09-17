/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Utils;
using static NativeManagedContext;
using CompilerInfrastructure.Contexts;

namespace LLVMCodeGenerator {
    class BuiltinHashMap {
        LLVMCodeGenerator gen;
        readonly Dictionary<IType, (IntPtr, IntPtr)> slotBucketType= new Dictionary<IType, (IntPtr, IntPtr)>();
        readonly LazyDictionary<IMethod, IntPtr> methods;
        public BuiltinHashMap(LLVMCodeGenerator gen) {
            this.gen = gen;
            methods = new LazyDictionary<IMethod, IntPtr>(GetMethod);
        }
        ManagedContext ctx => gen.ctx;
        private IntPtr GetMethod(IMethod met) {
            if (gen.methods.TryGetValue(met, out var ret))
                return ret;

            var tp = (met.NestedIn as ITypeContext).Type;

            var keyTp = (IType)tp.Signature.GenericActualArguments.First();
            var valTp = (IType)tp.Signature.GenericActualArguments.ElementAt(1);
            bool succ = gen.TryGetType(tp.AsReferenceType(), out var ty) & gen.TryGetType(keyTp, out var keyTy) & gen.TryGetType(valTp, out var valTy);
            var getHashCode = keyTp.Context.InstanceContext.MethodsByName("getHashCode").FirstOrDefault(x =>
                x.Arguments.Length == 0 &&
                x.ReturnType == PrimitiveType.SizeT &&
                x.Visibility == Visibility.Public
            );
            if (getHashCode is null && !keyTp.IsIntegerType() && !keyTp.IsString()) {
                $"The type {keyTp.Signature} does not provide  a publicly visible method getHashCode -> zint and can therefore not be used as key in an associative array".Report();
            }
            //TODO: other methods
            switch (met.Signature.Name) {
                case "ctor":
                    if (met.Arguments.Length == 0) {
                        var ctor1 = tp.Context.InstanceContext.MethodsByName("ctor").First(x=>x.Arguments.Length==1);
                        var unused = methods[ctor1];
                        ImplementBuiltinHashMapCtor0(met, ctor1, tp, ty);
                        return gen.methods[met];
                    }
                    else if (met.Arguments.Length == 1) {
                        ImplementBuiltinHashMapCtor1(met, tp, ty);
                        return gen.methods[met];
                    }
                    break;
                case "clear":
                    ImplementBuiltinHashMapClear(met, tp, ty);
                    return gen.methods[met];
                case "operator []": {
                    var search = ImplementBuiltinHashMapSearch(tp, ty, keyTy, valTy, keyTp);
                    if (met.Arguments.Length == 1) {
                        ImplementBuiltinHashMapOperatorIndexerGet(met, tp, ty, keyTp, keyTy, valTy, search, getHashCode);
                        return gen.methods[met];
                    }
                    else if (met.Arguments.Length == 2) {
                        var rehashInsert = RehashInsert(tp, ty, keyTy, search);
                        var rehash = ImplementBuiltinHashMapRehash(tp, ty, rehashInsert);
                        var ensureCapacity = ImplementBuiltinHashMapEnsureCapacity(tp, ty, rehash);
                        var insertInternal = ImplementBuiltinHashMapInsertInternal(tp, ty, keyTy, valTy, search);
                        ImplementBuiltinHashMapOperatorIndexerSet(met, tp, ty, keyTp, keyTy, valTy, ensureCapacity, insertInternal, getHashCode);
                        return gen.methods[met];
                    }

                    break;
                }
                case "tryGetNext":
                    return ImplementBuiltinHashMapTryGetNext(met, tp, ty, keyTy, valTy);
                case "insert": {
                    var search = ImplementBuiltinHashMapSearch(tp, ty, keyTy, valTy, keyTp);
                    var rehashInsert = RehashInsert(tp, ty, keyTy, search);
                    var rehash = ImplementBuiltinHashMapRehash(tp, ty, rehashInsert);
                    var ensureCapacity = ImplementBuiltinHashMapEnsureCapacity(tp, ty, rehash);
                    var insertInternal = ImplementBuiltinHashMapInsertInternal(tp, ty, keyTy, valTy, search);
                    ImplementBuiltinHashMapInsert(met, tp, ty, keyTp, keyTy, valTy, ensureCapacity, insertInternal, getHashCode);
                    return gen.methods[met];
                }
                case "tryGetValue": {
                    var search = ImplementBuiltinHashMapSearch(tp, ty, keyTy, valTy, keyTp);
                    ImplementBuiltinHashMapTryGetValue(met, tp, ty, keyTp, keyTy, valTy, search, getHashCode);
                    return gen.methods[met];
                }
                case "getOrElse": {
                    var search = ImplementBuiltinHashMapSearch(tp, ty, keyTy, valTy, keyTp);
                    ImplementBuiltinHashMapGetOrElse(met, tp, ty, keyTp, keyTy, valTy, search, getHashCode);
                    return gen.methods[met];
                }
            }
            throw new NotImplementedException($"The builtin hashmap function {met.Signature} is not implemented yet");
        }


        internal bool TryGetBuiltinHashMap(IType hmTy, IType keyTy, IType valTy, out IntPtr ret) {

            var hashMapTy = ctx.GetStruct(hmTy.Signature.ToString());
            gen.types[hmTy.AsValueType()] = hashMapTy;
            gen.types[hmTy.AsReferenceType()] = ctx.GetPointerType(hashMapTy);

            // first build the slot-type and the bucket-type
            // The slot-type will be the element-ytpe of the actual hash-table
            var slotTy = ctx.GetStruct(hmTy.Signature.ToString() + "::slotTy");
            // The bucket-type will be the element-type of the vector holding the key-value pairs
            var bucketTy = ctx.GetStruct(hmTy.Signature.ToString() + "::bucketTy");
            // struct slotTy {index, hashCode, state} where state: 0=>free, 1=>full, 2=>deleted
            ctx.CompleteStruct(slotTy, new[] { ctx.GetSizeTType(), ctx.GetSizeTType(), ctx.GetByteType() });
            bool succ = gen.TryGetType(keyTy, out var ky) & gen.TryGetType(valTy, out var val);
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
        internal bool ImplementBuiltinHashMap(IType tp, IType keyTy, IType valTy) {
            // implement all the methods

            var getHashCode = keyTy.Context.InstanceContext.MethodsByName("getHashCode").FirstOrDefault(x =>
                x.Arguments.Length == 0 &&
                x.ReturnType == PrimitiveType.SizeT &&
                x.Visibility == Visibility.Public
            );
            if (getHashCode is null && !keyTy.IsIntegerType() && !keyTy.IsString()) {
                $"The type {keyTy.Signature} does not provide  a publicly visible method getHashCode -> zint and can therefore not be used as key in an associative array".Report();
            }
            bool succ = gen.TryGetType(tp.AsReferenceType(), out var ty) & gen.TryGetType(keyTy, out var keyt) & gen.TryGetType(valTy, out var valt);
            if (!succ)
                return false;
            /*IMethod ctor1;
            succ &= ImplementBuiltinHashMapCtor1(ctor1 = tp.Context.InstanceContext.MethodsByName("ctor").First(x => x.Arguments.Length == 1), tp, ty);
            succ &= ImplementBuiltinHashMapCtor0(tp.Context.InstanceContext.MethodsByName("ctor").First(x => x.Arguments.Length == 0), ctor1, tp, ty);
            succ &= ImplementBuiltinHashMapClear(tp.Context.InstanceContext.MethodsByName("clear").First(), tp, ty);

            var search = ImplementBuiltinHashMapSearch(tp, ty, keyt, valt, keyTy);
            tp.OverloadsOperator(OverloadableOperator.Indexer, out var indexer);
            succ &= ImplementBuiltinHashMapOperatorIndexerGet((IMethod) indexer.First(x => x.Arguments.Length == 1), tp, ty, keyTy, keyt, valt, search, getHashCode);

            var insert = ImplementBuiltinHashMapInsertInternal(tp, ty, keyt, valt, search);
            var rehashinsert = RehashInsert(tp, ty, keyt, search);
            var rehash = ImplementBuiltinHashMapRehash(tp, ty, rehashinsert);
            var ensureCapacity = ImplementBuiltinHashMapEnsureCapacity(tp, ty, rehash);

            succ &= ImplementBuiltinHashMapOperatorIndexerSet((IMethod) indexer.First(x => x.Arguments.Length == 2), tp, ty, keyTy, keyt, valt, ensureCapacity, insert, getHashCode);
            ImplementBuiltinHashMapTryGetNext(tp.Context.InstanceContext.MethodsByName("tryGetNext").First(), tp, ty, keyt, valt);
            //TODO more methods
            throw new NotImplementedException();*/
            return succ;
        }
        internal bool TryGetMethod(IMethod met, out IntPtr ret) {
            ret = methods[met];
            return true;
        }
        private void ImplementBuiltinHashMapGetOrElse(IMethod met, IType tp, IntPtr ty, IType keyTp, IntPtr keyTy, IntPtr valTy, IntPtr search, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                valTy,
                new[]{ ty, keyTy, valTy },
                new[]{ "this", "key", "orElse" },
                false);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 0);
            var entry = new BasicBlock(ctx, "entry",fn);
            gen.methods.TryAdd(met, fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var ky = ctx.GetArgument(fn, 1);
                var orElse = ctx.GetArgument(fn, 2);

                var hashCode = GetHashCodeFrom(getHashCode, keyTp, ky, irb, fn);

                var slotPtr = ctx.GetCall(search, new[]{ thisptr, ky, hashCode}, irb);
                var state = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 2u}, irb);
                var isFull = ctx.CompareOp(state, ctx.GetInt8(1), (sbyte)'!', true, true, irb);
                var full = new BasicBlock(ctx, "full", fn);
                var notFull = new BasicBlock(ctx, "notFull", fn);
                ctx.ConditionalBranch(full, notFull, isFull, irb);

                ctx.ResetInsertPoint(notFull, irb);
                ctx.ReturnValue(orElse, irb);

                ctx.ResetInsertPoint(full, irb);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u},irb);
                var ret = ctx.LoadField(buckets, new[]{ ctx.LoadFieldConstIdx(slotPtr, new[] { 0u, 0u}, irb), ctx.GetInt32(1)}, irb);
                ctx.ReturnValue(ret, irb);
            }
        }

        private void ImplementBuiltinHashMapTryGetValue(IMethod met, IType tp, IntPtr ty, IType keyTp, IntPtr keyTy, IntPtr valTy, IntPtr search, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetBoolType(),
                new[]{ ty, keyTy, ctx.GetPointerType(valTy) },
                new[]{ "this", "key", "out_value" },
                false);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 0);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "writeonly", "notnull" }, 2);
            var entry = new BasicBlock(ctx, "entry",fn);
            gen.methods.TryAdd(met, fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var ky = ctx.GetArgument(fn, 1);
                var valPtr = ctx.GetArgument(fn, 2);

                var hashCode = GetHashCodeFrom(getHashCode, keyTp, ky, irb, fn);

                var slotPtr = ctx.GetCall(search, new[]{ thisptr, ky, hashCode}, irb);
                var state = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 2u}, irb);
                var isFull = ctx.CompareOp(state, ctx.GetInt8(1), (sbyte)'!', true, true, irb);
                var full = new BasicBlock(ctx, "full", fn);
                var notFull = new BasicBlock(ctx, "notFull", fn);
                ctx.ConditionalBranch(full, notFull, isFull, irb);

                ctx.ResetInsertPoint(notFull, irb);
                ctx.ReturnValue(ctx.False(), irb);

                ctx.ResetInsertPoint(full, irb);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u},irb);
                var ret = ctx.LoadField(buckets, new[]{ ctx.LoadFieldConstIdx(slotPtr, new[] { 0u, 0u}, irb), ctx.GetInt32(1)}, irb);
                ctx.Store(valPtr, ret, irb);
                ctx.ReturnValue(ctx.True(), irb);
            }
        }
        private IntPtr ImplementBuiltinHashMapTryGetNext(IMethod met, IType tp, IntPtr ty, IntPtr keyTy, IntPtr valTy) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetBoolType(),
                new[]{ ty, ctx.GetPointerType(ctx.GetSizeTType()), ctx.GetPointerType(keyTy), ctx.GetPointerType(valTy) },
                new[]{ "this", "state", "key", "value" },
                false);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 0);
            ctx.AddParamAttributes(fn, new[] { "nocapture" }, 1);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 2);
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 3);
            var entry = new BasicBlock(ctx, "entry",fn);
            gen.methods.TryAdd(met, fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var idxptr = ctx.GetArgument(fn, 1);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u }, irb);

                BasicBlock
                    hasNext = new BasicBlock(ctx, "hasNext",fn),
                    hasNotNext = new BasicBlock(ctx, "hasNotNext", fn);
                var size = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 3u }, irb);
                var index= ctx.Load(idxptr, irb);

                var sizeGTidx = ctx.CompareOp(size, index, (sbyte)'>', false, true, irb);
                ctx.ConditionalBranch(hasNext, hasNotNext, sizeGTidx, irb);

                ctx.ResetInsertPoint(hasNotNext, irb);
                ctx.ReturnValue(ctx.False(), irb);

                ctx.ResetInsertPoint(hasNext, irb);
                var ky = ctx.LoadField(buckets, new[]{ index, ctx.GetInt32(0) }, irb);
                var val = ctx.LoadField(buckets, new[]{ index, ctx.GetInt32(1) }, irb);

                //TODO copy ctor
                ctx.Store(ctx.GetArgument(fn, 2), ky, irb);
                ctx.Store(ctx.GetArgument(fn, 3), val, irb);

                var nxtIdx = ctx.ArithmeticBinOp(index, ctx.GetIntSZ(1), (sbyte)'+', true, irb);
                ctx.Store(idxptr, nxtIdx, irb);
                ctx.ReturnValue(ctx.True(), irb);
            }
            return fn;
        }

        private IntPtr RehashInsert(IType tp, IntPtr ty, IntPtr keyTy, IntPtr search) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(tp.FullName()+".rehashInsert",
                ctx.GetVoidType(),
                new[]{ ty, keyTy, ctx.GetSizeTType(), ctx.GetSizeTType() },
                new[]{ "this", "key", "hashCode", "valueIndex" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var key = ctx.GetArgument(fn, 1);
                var hashCode = ctx.GetArgument(fn, 2);
                var valIndex = ctx.GetArgument(fn, 3);

                var slotPtr = ctx.GetCall(search, new[]{ thisptr, key, hashCode }, irb);
                ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 0u }, valIndex, irb);
                ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 1u }, hashCode, irb);
                ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 2u }, ctx.GetInt8(1), irb);
                ctx.ReturnVoid(irb);
            }
            return fn;
        }
        private IntPtr ImplementBuiltinHashMapRehash(IType tp, IntPtr ty, IntPtr rehashInsert) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(tp.FullName()+".rehash",
                ctx.GetVoidType(),
                new[]{ ty, ctx.GetBoolType() },
                new[]{ "this", "increaseCapacity" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var increaseCapacity = ctx.GetArgument(fn, 1);
                var oldCap = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 2u }, irb);

                var doIncreaseCapacity = new BasicBlock(ctx, "doIncreaseCapacity", fn);
                var doRehash = new BasicBlock(ctx, "doRehash", fn);
                ctx.ConditionalBranch(doIncreaseCapacity, doRehash, increaseCapacity, irb);

                ctx.ResetInsertPoint(doIncreaseCapacity, irb);
                var nextPrime = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.nextPrime);
                var doubleCap = ctx.ShiftOp(oldCap, ctx.GetIntSZ(1), true, true, irb);
                var doubleCapPrime = ctx.GetCall(nextPrime, new[]{ doubleCap }, irb);
                ctx.Branch(doRehash, irb);

                ctx.ResetInsertPoint(doRehash, irb);
                var newCap = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
                newCap.AddMergePoint(oldCap, entry);
                newCap.AddMergePoint(doubleCapPrime, doIncreaseCapacity);

                var slots = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 0u }, irb);
                var buckets = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u }, irb);
                var size = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 3u }, irb);
                var (slotTy, bucketTy) = slotBucketType[tp];

                var nwSlots = MallocCArray(slotTy, newCap, irb);
                var nwBuckets = MallocCArray(bucketTy, newCap, irb);

                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 0u }, nwSlots, irb);
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 1u }, nwBuckets, irb);
                var bucketSize = ctx.GetSizeOf(bucketTy);
                var bucketsBytes = ctx.ArithmeticBinOp(size, bucketSize, (sbyte)'*', true, irb);
                ctx.MemoryCopy(nwBuckets, buckets, bucketsBytes, false, irb);

                var loop = new BasicBlock(ctx, "loop", fn);
                var loopEnd = new BasicBlock(ctx, "loopEnd", fn);
                var full = new BasicBlock(ctx, "full", fn);
                var loopCond = new BasicBlock(ctx, "loopCond", fn);

                var sizeIs0 = ctx.CompareOp(size, ctx.GetIntSZ(0), (sbyte)'!', true, true, irb);
                ctx.ConditionalBranch(loopEnd, loop, sizeIs0, irb);

                ctx.ResetInsertPoint(loop, irb);
                var i = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
                i.AddMergePoint(ctx.GetIntSZ(0), doRehash);
                var state = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(2)}, irb);
                var isFull = ctx.CompareOp(state, ctx.GetIntSZ(1), (sbyte)'!', true, true, irb);
                ctx.ConditionalBranch(full, loopCond, isFull, irb);

                ctx.ResetInsertPoint(full, irb);
                {
                    var valInd = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(0)}, irb);
                    var hashCode = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(1)}, irb);
                    var ky = ctx.LoadField(buckets, new[]{ valInd, ctx.GetInt32(0)}, irb);
                    ctx.GetCall(rehashInsert, new[] { thisptr, ky, hashCode, valInd }, irb);
                }
                ctx.Branch(loopCond, irb);

                ctx.ResetInsertPoint(loopCond, irb);
                var iPlus1 = ctx.ArithmeticBinOp(i, ctx.GetIntSZ(1),(sbyte)'+', true, irb);
                i.AddMergePoint(iPlus1, loopCond);
                var nextIteration = ctx.CompareOp(iPlus1, newCap, (sbyte)'<',false, true, irb);
                ctx.ConditionalBranch(loop, loopEnd, nextIteration, irb);

                ctx.ResetInsertPoint(loopEnd, irb);
                // set deletedCount to 0
                ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 4u }, ctx.GetIntSZ(0), irb);
                ctx.ReturnVoid(irb);
            }

            return fn;
        }
        private IntPtr ImplementBuiltinHashMapEnsureCapacity(IType tp, IntPtr ty, IntPtr rehash) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(tp.FullName()+".ensureCapacity",
                ctx.GetVoidType(),
                new[]{ ty},
                new[]{ "this" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                //load factor = 0,8
                var cap = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 2u }, irb);
                var bound = ctx.ArithmeticBinOp(ctx.ForceCast(cap, ctx.GetFloatType(), true, irb), ctx.GetFloat(0.8f),(sbyte)'*', true, irb);
                var size = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 3u }, irb);
                var del = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 4u }, irb);
                var load = ctx.ArithmeticBinOp(size, del, (sbyte)'+', true, irb);
                var loadFP = ctx.ForceCast(load, ctx.GetFloatType(), true, irb);
                var needRehash = ctx.CompareOp(loadFP, bound, (sbyte)'>', true, true, irb);

                var retBB = new BasicBlock(ctx, "exit", fn);
                var doRehash = new BasicBlock(ctx, "doRehash", fn);

                ctx.ConditionalBranch(doRehash, retBB, needRehash, irb);

                ctx.ResetInsertPoint(retBB, irb);
                ctx.ReturnVoid(irb);

                ctx.ResetInsertPoint(doRehash, irb);

                // increase the capacity iff the size is at least as large as the deletedCount (otherwise 
                // even without resizing the capacity the available space is at least as big in relation to size
                // as if resizing when del==0)
                var sizeGEdel = ctx.CompareOp(size, del, (sbyte)'>',true,true,irb);
                ctx.GetCall(rehash, new[] { thisptr, sizeGEdel }, irb);
                ctx.Branch(retBB, irb);
            }
            return fn;
        }

        private IntPtr ImplementBuiltinHashMapInsertInternal(IType tp, IntPtr ty, IntPtr keyTy, IntPtr valTy, IntPtr search) {
            var irb = IntPtr.Zero;
            var retTy = ctx.GetUnnamedStruct(new[]{ctx.GetPointerType(valTy), ctx.GetBoolType() });
            var fn = ctx.DeclareFunction(tp.FullName()+".insertInternal",
                retTy,
                new[]{ ty, keyTy, valTy, ctx.GetSizeTType(), ctx.GetBoolType() },
                new[]{ "this", "key", "value", "hashCode", "replace" },
                false);
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn,0);
                var ky = ctx.GetArgument(fn, 1);
                var val = ctx.GetArgument(fn, 2);
                var hashCode = ctx.GetArgument(fn, 3);
                var replace = ctx.GetArgument(fn, 4);
                var size = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 3u }, irb);

                var slotPtr = ctx.GetCall(search, new[]{ thisptr, ky, hashCode}, irb);
                var state = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 2u}, irb);
                var isFull = ctx.CompareOp(state, ctx.GetInt8(1), (sbyte)'!', true, true, irb);
                var full = new BasicBlock(ctx, "full", fn);
                var notFull = new BasicBlock(ctx, "notFull", fn);
                var fullNotFullMerge = new BasicBlock(ctx, "full-notFull-merge", fn);
                var fullReplace = new BasicBlock(ctx, "full-replace", fn);
                var decrDel = new BasicBlock(ctx, "decrease-deletedCount", fn);
                ctx.ConditionalBranch(full, notFull, isFull, irb);

                ctx.ResetInsertPoint(notFull, irb);
                // fill slot, add kvp, increase size, NOT ensureCapacity
                {
                    // index is the old size
                    ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 0u }, size, irb);
                    ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 1u }, hashCode, irb);
                    ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 2u }, ctx.GetInt8(1), irb);

                    var kvpPtr = ctx.GetElementPtr(ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u }, irb), new[]{ size }, irb);
                    ctx.StoreFieldConstIdx(kvpPtr, new[] { 0u, 0u }, ky, irb);
                    ctx.StoreFieldConstIdx(kvpPtr, new[] { 0u, 1u }, val, irb);

                    var sizePlus1 = ctx.ArithmeticBinOp(size, ctx.GetIntSZ(1), (sbyte)'+', true, irb);
                    ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 3u }, sizePlus1, irb);

                    // if state == deleted then del-- ;
                    var needDecrDel = ctx.CompareOp(state, ctx.GetInt8(2), (sbyte)'!', true, true, irb);
                    ctx.ConditionalBranch(decrDel, fullNotFullMerge, needDecrDel, irb);
                }

                ctx.ResetInsertPoint(decrDel, irb);
                {
                    var del = ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 4u }, irb);
                    var delMinus1 = ctx.ArithmeticBinOp(del, ctx.GetIntSZ(1), (sbyte)'-', true, irb);
                    ctx.StoreFieldConstIdx(thisptr, new[] { 0u, 4u }, delMinus1, irb);
                }
                ctx.Branch(fullNotFullMerge, irb);

                ctx.ResetInsertPoint(full, irb);
                ctx.ConditionalBranch(fullReplace, fullNotFullMerge, replace, irb);

                ctx.ResetInsertPoint(fullReplace, irb);
                // fill slot, fill kvp, NOT increase size, NOT ensureCapacity
                {
                    // index remains the same
                    //ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 0u }, size, irb);
                    // we find full slots only when hashCode and key are equal
                    //ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 1u }, hashCode, irb);
                    // obviously the slotfill is already full and remains full
                    //ctx.StoreFieldConstIdx(slotPtr, new[] { 0u, 2u }, ctx.GetInt8(1), irb);
                    var index = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 0u }, irb);

                    var kvpPtr = ctx.GetElementPtr(ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u }, irb), new[]{ index }, irb);
                    // key has to be reinserted, because the equals function may
                    // treat two keys as equal, which have not the same bit-pattern
                    ctx.StoreFieldConstIdx(kvpPtr, new[] { 0u, 0u }, ky, irb);
                    ctx.StoreFieldConstIdx(kvpPtr, new[] { 0u, 1u }, val, irb);
                }
                ctx.Branch(fullNotFullMerge, irb);

                ctx.ResetInsertPoint(fullNotFullMerge, irb);
                var boolRet = new PHINode(ctx, ctx.GetBoolType(), 3, irb);
                boolRet.AddMergePoint(ctx.True(), notFull);
                boolRet.AddMergePoint(ctx.True(), fullReplace);
                boolRet.AddMergePoint(ctx.False(), full);
                boolRet.AddMergePoint(ctx.True(), decrDel);
                var ind = ctx.LoadFieldConstIdx(slotPtr, new[]{ 0u, 0u }, irb);
                var valPtr = ctx.GetElementPtr(ctx.LoadFieldConstIdx(thisptr, new[]{ 0u, 1u }, irb), new[]{ ind, ctx.GetInt32(1)}, irb);
                ctx.ReturnValue(ctx.GetStructValue(retTy, new[] { valPtr, boolRet }, irb), irb);
            }
            return fn;
        }
        private bool ImplementBuiltinHashMapOperatorIndexerSet(IMethod met, IType tp, IntPtr ty, IType keyTp, IntPtr keyTy, IntPtr valTy, IntPtr ensureCapacity, IntPtr insertInternal, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetPointerType(valTy),
                new[]{ ty, keyTy, valTy },
                new[]{ "this", "key", "value" },
                false);
            gen.methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var key = ctx.GetArgument(fn, 1);
                var val = ctx.GetArgument(fn, 2);
                ctx.GetCall(ensureCapacity, new[] { thisptr }, irb);
                var hashCode = GetHashCodeFrom(getHashCode, keyTp, key, irb, fn);
                var retTup = ctx.GetCall(insertInternal, new[]{ thisptr, key, val, hashCode, ctx.True() }, irb);
                var ret = ctx.ExtractValue(retTup, 0, irb);
                ctx.ReturnValue(ret, irb);
            }
            return true;
        }
        private bool ImplementBuiltinHashMapInsert(IMethod met, IType tp, IntPtr ty, IType keyTp, IntPtr keyTy, IntPtr valTy, IntPtr ensureCapacity, IntPtr insertInternal, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetBoolType(),
                new[]{ ty, keyTy, valTy, ctx.GetBoolType() },
                new[]{ "this", "key", "value", "replace" },
                false);
            gen.methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var thisptr = ctx.GetArgument(fn, 0);
                var key = ctx.GetArgument(fn, 1);
                var val = ctx.GetArgument(fn, 2);
                var replace = ctx.GetArgument(fn, 3);
                ctx.GetCall(ensureCapacity, new[] { thisptr }, irb);
                var hashCode = GetHashCodeFrom(getHashCode, keyTp, key, irb, fn);
                var retTup = ctx.GetCall(insertInternal, new[]{ thisptr, key, val, hashCode, replace }, irb);
                var ret = ctx.ExtractValue(retTup, 1, irb);
                ctx.ReturnValue(ret, irb);
            }
            return true;
        }
        private IntPtr GetHashCodeFrom(IMethod getHashCode, IType keyTp, IntPtr obj, IntPtr irb, IntPtr fn) {
            if (getHashCode is null) {
                if (keyTp.IsIntegerType() || !keyTp.IsValueType()) {
                    return ctx.ForceCast(obj, ctx.GetSizeTType(), true, irb);
                }
                else if (keyTp.IsString()) {
                    var ghc = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.str_hash);
                    var strVal = ctx.ExtractValue(obj, 0, irb);
                    var strLen = ctx.ExtractValue(obj, 1, irb);
                    return ctx.GetCall(ghc, new[] { strVal, strLen }, irb);
                }
                else {
                    // should be already caught
                    return $"The value-type {keyTp.Signature} does not implement the method 'zint getHashCode()' and can therefore not be used as key in a hash-map".Report(default, ctx.GetIntSZ(0));
                }
            }
            else {
                gen.TryGetMethod(getHashCode, out var ghc);
                if (!keyTp.IsNotNullable()) {
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
        private bool ImplementBuiltinHashMapOperatorIndexerGet(IMethod met, IType tp, IntPtr ty, IType keyTp, IntPtr keyTy, IntPtr valTy, IntPtr search, IMethod getHashCode) {
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetPointerType(valTy),
                new[]{ ty, keyTy },
                new[]{ "this", "key" },
                false);
            gen.methods[met] = fn;
            var entry = new BasicBlock(ctx, "entry",fn);
            using (ctx.PushIRBContext(entry, irb)) {
                var ky = ctx.GetArgument(fn, 1);
                var hashCode = GetHashCodeFrom(getHashCode, keyTp, ky, irb, fn);
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
            // TODO handle nullptrs

            if (tp.IsString()) {
                // handle string equality
                var op = InstructionGenerator.GetOrCreateInternalFunction(ctx, InstructionGenerator.InternalFunction.strequals);
                return ctx.GetCall(op, new[] {
                    ctx.ExtractValue(obj1, 0, irb),
                    ctx.ExtractValue(obj1, 1, irb),
                    ctx.ExtractValue(obj2, 0, irb),
                    ctx.ExtractValue(obj2, 1, irb),
                }, irb);
            }
            IMethod equalsMet=null;
            if (tp.OverloadsOperator(OverloadableOperator.Equal, out var ov, CompilerInfrastructure.Contexts.SimpleMethodContext.VisibleMembers.Instance)) {
                equalsMet = gen.semantics.BestFittingMethod(default, ov, new[] { tp }, PrimitiveType.Bool, new CompilerInfrastructure.Utils.ErrorBuffer());
            }
            if (equalsMet is null || equalsMet.IsError()) {
                if (tp.OverloadsOperator(OverloadableOperator.Equal, out var ovStat, CompilerInfrastructure.Contexts.SimpleMethodContext.VisibleMembers.Static)) {
                    equalsMet = gen.semantics.BestFittingMethod(default, ovStat, new[] { tp, tp }, PrimitiveType.Bool, new CompilerInfrastructure.Utils.ErrorBuffer());
                }
            }
            if (equalsMet is null || equalsMet.IsError())
                return ctx.CompareOp(obj1, obj2, (sbyte) '!', true, false, irb);
            else {
                gen.TryGetMethod(equalsMet, out var met);
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
            ctx.AddParamAttributes(fn, new[] { "nocapture", "readonly" }, 0);
            ctx.AddReturnNotNullAttribute(fn);
            ctx.AddFunctionAttributes(fn, new[] { "argmemonly", "readonly" });
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
                    var loopEntry = ctx.GetCurrentBasicBlock(irb);
                    var iLTcap = ctx.CompareOp(indexStart, indexEnd, (sbyte)'<',false,true, irb);
                    ctx.ConditionalBranch(loopBlock, loopEnd, iLTcap, irb);

                    ctx.ResetInsertPoint(loopBlock, irb);
                    var i = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
                    i.AddMergePoint(indexStart, loopEntry);


                    var state = ctx.LoadField(slots, new[]{ i, ctx.GetInt32(2) }, irb);
                    var isStateFree = ctx.CompareOp(state, ctx.GetInt8(0), (sbyte)'!',true, true, irb);
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
            //TODO handle dtors
            var irb = IntPtr.Zero;
            var fn = ctx.DeclareFunction(DefaultNameMangler.Instance.MangleFunctionName(met),
                ctx.GetVoidType(),
                new[]{ ty },
                new[]{ "this" },
                false);
            gen.methods[met] = fn;
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
            gen.methods[met] = fn;
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
            gen.methods[met] = fn;
            using (ctx.PushIRBContext(entry, irb)) {
                var ctor1Fn = gen.methods[ctor1];
                ctx.GetCall(ctor1Fn, new[] { ctx.GetArgument(fn, 0), ctx.GetIntSZ(5) }, irb);
                // rest is zero-initialized
                ctx.ReturnVoid(irb);
            }
            return true;
        }
    }

}
