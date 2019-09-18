/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CompilerInfrastructure;
using CompilerInfrastructure.Utils;
using CompilerInfrastructure.Structure.Types;
using static NativeManagedContext;
using Type = CompilerInfrastructure.Type;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Semantics;
using static CompilerInfrastructure.Structure.Types.ArrayType.ArrayContext;
using static CompilerInfrastructure.Structure.Types.SpanType.SpanContext;
using CompilerInfrastructure.Structure;

namespace LLVMCodeGenerator {
    public partial class InstructionGenerator {

        protected readonly Dictionary<string, IntPtr> stringLiterals = new Dictionary<string, IntPtr>();
        protected readonly Dictionary<(IType, IType), IntPtr> interfaceVTables = new Dictionary<(IType, IType), IntPtr>();
        readonly Dictionary<IType, IntPtr> tempMemoryCache = new Dictionary<IType, IntPtr>();
        //private readonly Stack<(IType, IntPtr)> memoryForward = new Stack<(IType, IntPtr)>();
        private readonly StackMap<IType, (IExpression, IntPtr)> memoryForward
            = new StackMap<IType, (IExpression, IntPtr)>(Comparer<IType>.Create((x, y) => { x.IsSubTypeOf(y, out var ret); return ret; }));
        /// <summary>
        /// Tries the consume the memory-forward.
        /// This is useful for assignments, where the righthandside needs to know, where the result will be stored (zB: value-type initialization)
        /// </summary>
        /// <param name="mem">The top-forwarded memory.</param>
        /// <returns>True, iff there is forwarded memory and the top-forwarded memory is still unused</returns>
        bool TryConsumeMemoryForward(IType ty, out IntPtr mem) {
            /* if (memoryForward.TryPop(out var ret) && ret.Item2 != IntPtr.Zero && ty == ret.Item1) {
                 memoryForward.Push((Type.Error, IntPtr.Zero));
                 mem = ret.Item2;
                 return true;
             }
             mem = IntPtr.Zero;
             return false;*/
            if (memoryForward.TryPopValue(ty, out var ret, out var actTy) && ret.Item2 != IntPtr.Zero) {
                memoryForward.Push(actTy, (Expression.Error, IntPtr.Zero));
                mem = ret.Item2;
                return true;
            }
            mem = IntPtr.Zero;
            return false;
        }
        bool TryConsumeMemoryForward(IExpression resultOf, out IntPtr mem) {
            if (memoryForward.TryPopValue(resultOf.ReturnType, out var ret, out var actTy) && ret.Item2 != IntPtr.Zero) {
                memoryForward.Push(actTy, (resultOf, IntPtr.Zero));
                mem = ret.Item2;
                return true;
            }
            mem = IntPtr.Zero;
            return false;
        }
        /// <summary>
        /// The provider of the top-forwarded memory now takes this memory back
        /// and determines whether or not the forwarded memory is still unused
        /// </summary>
        /// <returns>True if the memory is still unused, otherwise false</returns>
        bool ReturnMemoryForward(IExpression resultOf) {
            return memoryForward.TryPopValue(resultOf.ReturnType, out var res) && (res.Item1 != resultOf || res.Item2 != IntPtr.Zero);
        }
        void ProvideMemoryForward(IType tp, IntPtr mem) {
            memoryForward.Push(tp, (null, mem));
        }
        protected IntPtr GetMemory(IExpression forResult) {
            if (forResult is null)
                throw new ArgumentNullException(nameof(forResult));
            if (!TryConsumeMemoryForward(forResult, out var ret) && !tempMemoryCache.TryGetValue(forResult.ReturnType, out ret)) {
                // no need to initialize the alloca, since this method will only used for temporary memory
                gen.TryGetType(forResult.ReturnType, out var llty);
                ret = ctx.DefineAlloca(fn, llty, "tmp");
                tempMemoryCache[forResult.ReturnType] = ret;
            }
            return ret;
        }
        protected IntPtr GetMemory(IType type) {
            if (!TryConsumeMemoryForward(type, out var ret) && !tempMemoryCache.TryGetValue(type, out ret)) {
                // no need to initialize the alloca, since this method will only used for temporary memory
                gen.TryGetType(type, out var llty);
                ret = ctx.DefineAlloca(fn, llty, "tmp");
                tempMemoryCache[type] = ret;
            }
            return ret;
        }
        /// <summary>
        /// Like <see cref="TryExpressionCodeGen(IExpression, out IntPtr)"/>, but uses <see cref="TryGetMemoryLocation(IExpression, out IntPtr)"/> when 
        /// the resulttype is a locally allocated struct
        /// </summary>
        /// <param name="exp">The expressino.</param>
        /// <param name="ret">The return-value.</param>
        /// <returns></returns>
        bool TryRefExpressionCodeGen(IExpression exp, out IntPtr ret) {
            if (IsReference(exp) || exp.ReturnType.IsInterface())
                return TryExpressionCodeGen(exp, out ret);
            else
                return TryGetMemoryLocation(exp, out ret);
        }
        protected bool TryRangeExpressionCodeGen(IExpression exp, out IntPtr ret) {
            if (exp.ReturnType.IsArray() || exp.ReturnType.IsFixedSizedArray())
                return TryRefExpressionCodeGen(exp, out ret);
            else // vararg, span
                return TryExpressionCodeGen(exp, out ret);
        }
        bool IsReference(IExpression exp) {
            if (exp is VariableAccessExpression vr) {
                if (vr.Variable.IsLocallyAllocated())
                    return false;
            }
            return gen.IsReferenceType(exp.ReturnType);
        }
        /// <summary>
        /// Tries the get indexer expression memory location.
        /// Does not evaluate or store the value of an indexer-set
        /// </summary>
        /// <param name="arrInd">The array inddx.</param>
        /// <param name="ret">the return-value.</param>
        /// <returns></returns>
        bool TryGetIndexerExpressionMemoryLocation(IndexerExpression arrInd, out IntPtr ret) {
            if (arrInd.Indices.Length > 2) {
                // kann nicht 0 sein, sonst wäre der Konstruktor gescheitert
                "An array cannot be indexed with multiple indices".Report(arrInd.Position, false);
            }
            if (!arrInd.IsLValue(method)) {
                ret = IntPtr.Zero;
                return "The array-element cannot be assigned, since the array is constant".Report(arrInd.Position, false);
            }
            if (!TryExpressionCodeGen(arrInd.Indices[0], out var index)) {
                ret = IntPtr.Zero;
                return false;
            }

            bool index64BitRequired = (arrInd.Indices[0].ReturnType is PrimitiveType prim) ? prim.PrimitiveName > PrimitiveName.UInt : true;
            if (arrInd.ParentExpression.ReturnType.IsArray()) {
                if (!TryRefExpressionCodeGen(arrInd.ParentExpression, out var array)) {
                    ret = IntPtr.Zero;
                    return false;
                }

                var arrLenPtr = ctx.GetElementPtrConstIdx(array, new[] { 0u, 0u }, irb);

                //if (!arrInd.ParentExpression.IsNotNullable())
                //    ThrowIfNull(array);

                //ThrowIfGreaterEqual(index, length, index64BitRequired);
                if (!arrInd.ParentExpression.IsNotNullable())
                    ThrowIfNullOrGreaterEqual(index, arrLenPtr, index64BitRequired);
                else
                    ThrowIfGreaterEqual(index, ctx.Load(arrLenPtr, irb), index64BitRequired);

                ret = ctx.GetElementPtr(array, new[] { ctx.GetInt32(0), ctx.GetInt32(1), index }, irb);
                return true;
            }
            else if (arrInd.ParentExpression.ReturnType.IsArraySlice()) {
                if (!TryExpressionCodeGen(arrInd.ParentExpression, out var span)) {
                    ret = IntPtr.Zero;
                    return false;
                }
                var arr = ctx.ExtractValue(span, 0, irb);
                var length = ctx.ExtractValue(span, 1u, irb);
                ThrowIfGreaterEqual(index, length, index64BitRequired);
                ret = ctx.GetElementPtr(arr, new[] { index }, irb);
                return true;
            }
            else if (arrInd.ParentExpression.ReturnType.IsVarArg()) {
                // vararg indexer
                if (!TryExpressionCodeGen(arrInd.ParentExpression, out var vaa)) {
                    ret = IntPtr.Zero;
                    return false;
                }
                var spanIdx = ctx.DefineAlloca(fn, ctx.GetIntType(), "spanIdx");
                var offset = ctx.DefineAlloca(fn, ctx.GetSizeTType(), "offset");
                VarArgStartIndex(vaa, spanIdx, offset, index);
                offset = ctx.Load(offset, irb);
                spanIdx = ctx.Load(spanIdx, irb);
                var ptr = ctx.LoadField(ctx.ExtractValue(vaa, 0, irb), new[] { spanIdx, ctx.GetInt32(0) }, irb);
                ret = ctx.GetElementPtr(ptr, new[] { offset }, irb);
                return true;
            }
            else {

                ret = IntPtr.Zero;
                return $"The {arrInd.ParentExpression.GetType().Name} cannot be indexed".Report(arrInd.Position, false);
            }
        }
        protected virtual bool TryGetMemoryLocation(IExpression exp, out IntPtr ret) {
            bool succ = true;
            
            switch (exp) {
                case VariableAccessExpression vr: {
                    if (variables.TryGetValue(vr.Variable, out ret)) {// local variable
                        // the memory-location is the alloca
                        return true;
                    }
                    else if (coro.LocalVariableIndex != null && coro.LocalVariableIndex.TryGetValue(vr.Variable, out var ind)) {
                        ret = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, ind }, irb);
                        return true;
                    }
                    else if (vr.ParentExpression != null) {
                        // instance-field
                        var par = vr.Variable.DefinedInType;
                        if (par is null) {
                            return $"The instance-field {vr.Variable.Signature} must be declared inside a class".Report(vr.Position, false);
                        }
                        else if (!vr.ParentExpression.ReturnType.IsSubTypeOf(par)) {
                            return $"The object does not contain an instance-field {vr.Variable.Signature}".Report(vr.Position, false);
                        }
                        succ &= TryExpressionCodeGen(vr.ParentExpression, out var parent);
                        var gepIndices = new List<uint>();
                        bool isRef = IsReference(vr.ParentExpression);
                        if (isRef && !vr.ParentExpression.IsNotNullable())
                            ThrowIfNull(parent);
                        // not indexing an array
                        gepIndices.Add(0);
                        var parTmp = vr.ParentExpression.ReturnType;
                        while (par != parTmp) {
                            //since retTy is subclass of par, retTy must have an object of type par inside
                            gepIndices.Add(gen.GetSuperPosition(parTmp));
                            parTmp = (parTmp as IHierarchialType).SuperType;
                        }
                        gepIndices.Add(gen.instanceFields[vr.Variable]);
                        if (!isRef) {
                            var llPar = ctx.DefineTypeInferredInitializedAlloca(fn, parent, "tmp", irb, false);
                            parent = llPar;
                        }
                        ret = ctx.GetElementPtrConstIdx(parent, gepIndices.ToArray(), irb);

                        return succ;
                    }
                    else if (gen.staticVariables.TryGetValue(vr.Variable, out ret)) {
                        // static field
                        // the memory-location is the global-variable
                        return true;
                    }
                    else {
                        return "Invalid instance-field access".Report(vr.Position, false);
                    }
                }
                case IndexerExpression arrInd: {
                    if (arrInd.Indices.Length == 2) {
                        if (!TryGetIndexerExpressionMemoryLocation(arrInd, out ret) | !TryExpressionCodeGen(arrInd.Indices[1], out var nwElem)) {
                            return false;
                        }
                        ctx.Store(ret, nwElem, irb);
                        return true;
                    }
                    else
                        return TryGetIndexerExpressionMemoryLocation(arrInd, out ret);
                }
                case CallExpression call: {
                    return TryCallCodeGen(call.Position, call.Callee, call.ParentExpression, call.Arguments, out ret, call.IsCallVirt, true);
                }
                default: {
                    succ &= TryExpressionCodeGen(exp, out var val);
                    ret = ctx.DefineTypeInferredInitializedAlloca(fn, val, "tmp", irb, false);
                    return succ;
                }
            }
            // mem
            throw new NotImplementedException();
        }

        IntPtr CastPrimitiveToString(IntPtr val, PrimitiveType valTy) {
            switch (valTy.PrimitiveName) {
                case PrimitiveName.Bool: {
                    var one = ctx.GetString("true", irb);
                    var zero = ctx.GetString("false", irb);
                    return ctx.ConditionalSelect(one, zero, val, irb);
                }
                case PrimitiveName.BigLong:
                case PrimitiveName.UBigLong: {
                    // biglong to string
                    var tmp = ctx.DefineTypeInferredInitializedAlloca(fn, val, "tmp", irb, false);
                    var hiloTp = ctx.GetPointerType(ctx.GetArrayType(ctx.GetLongType(), 2));
                    ctx.TryCast(tmp, hiloTp, ref tmp, true, irb);
                    var hiPtr = ctx.GetElementPtrConstIdx(tmp, new[] { 0u, 0u }, irb);
                    var loPtr = ctx.GetElementPtrConstIdx(tmp, new[] { 0u, 1u }, irb);
                    var hi = ctx.Load(hiPtr, irb);
                    var lo = ctx.Load(loPtr, irb);

                    var intnl = PrimitiveToStringFunction(valTy.PrimitiveName);
                    var castFn = GetOrCreateInternalFunction(ctx, intnl.ToString());

                    var ret = GetMemory(PrimitiveType.String);
                    ctx.GetCall(castFn, new[] { hi, lo, ret }, irb);
                    return ctx.Load(ret, irb);
                }


                case PrimitiveName.String:
                    return val;
                default: {
                    var intnl = PrimitiveToStringFunction(valTy.PrimitiveName);
                    var castFn = GetOrCreateInternalFunction(ctx, intnl.ToString());
                    var ret = GetMemory(PrimitiveType.String);
                    switch (valTy.PrimitiveName) {
                        case PrimitiveName.Byte:
                        case PrimitiveName.Char:
                        case PrimitiveName.Short:
                        case PrimitiveName.UShort:
                            ctx.TryCast(val, ctx.GetIntType(), ref val, false, irb);
                            break;
                    }
                    ctx.GetCall(castFn, new[] { val, ret }, irb);
                    return ctx.Load(ret, irb);
                }
            }

            throw new NotImplementedException();
        }
        protected internal bool TryCast(Position pos, IntPtr val, IType valTy, IType destTy, out IntPtr ret, bool? locallyAllocated = null, bool throwOnError = true) {
            return TryCastInternal(pos, val, valTy, destTy, locallyAllocated ?? valTy.IsValueType(), out ret, throwOnError);
        }
        bool TryCast(IntPtr val, IntPtr tar, bool isUnsigned, out IntPtr ret) {
            ret = IntPtr.Zero;
            return ctx.TryCast(val, tar, ref ret, isUnsigned, irb);
        }
        bool TryCastInternal(Position pos, IntPtr val, IType valTy, IType destTy, bool isLocallyAllocated, out IntPtr ret, bool throwOnError) {
            if (valTy == destTy && !(destTy is ClassType && isLocallyAllocated) || destTy.IsTop()) {
                ret = val;
                return true;
            }
            if (valTy == destTy.AsByRef()) {
                ret = ctx.Load(val, irb);
                return true;
            }
            if (destTy.IsPrimitive(PrimitiveName.String)) {
                if (!valTy.IsPrimitive()) {
                    if (valTy.OverloadsOperator(OverloadableOperator.String, out var _mets)) {
                        //assert, that operator string - overloads are never generic
                        var mets = _mets.OfType<IMethod>();
                        if (mets.First().IsStatic()) {
                            if (mets.First().Arguments[0].IsNotNull() && !valTy.IsNotNullable())
                                $"The nullable value of type {valTy.Signature} cannot be cast to string, since the converter-function needs a nonull-value".Report(pos);
                            return TryCallCodeGen(pos, mets.First(), valTy, default, new[] { val }, out ret, false);
                        }
                        else {
                            // operator string -overloads are non-virtual
                            if (!valTy.IsNotNullable()) {

                                var notnull = ctx.IsNotNull(val, irb);
                                IntPtr bNull = new BasicBlock(ctx, "isNull", fn),
                                    bNotNull = new BasicBlock(ctx, "isNotNull", fn),
                                    bMerge = new BasicBlock(ctx, "nullMerge", fn);
                                ctx.ConditionalBranch(bNotNull, bNotNull, notnull, irb);
                                ctx.ResetInsertPoint(bNull, irb);
                                bool succ = TryExpressionCodeGen(new DefaultValueExpression(pos, destTy), out var dflt);
                                ctx.Branch(bMerge, irb);
                                ctx.ResetInsertPoint(bNotNull, irb);
                                succ &= TryCallCodeGen(pos, mets.First(), valTy, val, Array.Empty<IntPtr>(), out var retVal, false);
                                ctx.Branch(bMerge, irb);
                                ctx.ResetInsertPoint(bMerge, irb);
                                if (succ) {
                                    var phi = new PHINode(ctx, ctx.GetStringType(), 2, irb);
                                    phi.AddMergePoint(dflt, bNull);
                                    phi.AddMergePoint(retVal, bNotNull);
                                    ret = phi;
                                }
                                else
                                    ret = ctx.GetNullPtr();
                                return succ;
                            }
                            else if (valTy.IsValueType() && !valTy.IsPrimitive()) // cannot call functions on values, only on references
                                val = ctx.DefineTypeInferredInitializedAlloca(fn, val, "", irb, false);
                            return TryCallCodeGen(pos, mets.First(), valTy, val, Array.Empty<IntPtr>(), out ret, false);
                        }
                    }
                    else {
                        ret = IntPtr.Zero;
                        return $"The non-primitive type {valTy.Signature} cannot be cast to string".Report(pos, false);
                    }
                }
                ret = CastPrimitiveToString(val, (PrimitiveType) valTy);
                return true;
            }
            if (valTy.IsArray() && destTy.IsArraySlice()) {
                if (isLocallyAllocated) {
                    ret = val;
                    return "A fixed-sized array cannot be implicitly cast to a slice, but an explicit conversation is available".Report(pos, false);
                }
                if (!valTy.IsNotNullable()) {

                    var isnotnull = ctx.IsNotNull(val, irb);
                    IntPtr bNull = new BasicBlock(ctx, "isNull", fn),
                        bNotNull = new BasicBlock(ctx, "isNotNull", fn),
                        bMerge = new BasicBlock(ctx, "nullMerge", fn);
                    ctx.ConditionalBranch(bNotNull, bNull, isnotnull, irb);
                    ctx.ResetInsertPoint(bNotNull, irb);
                    bool succ = TryGEPFirstElement(valTy, val, out var gep);
                    succ &= TryGetArrayLength(pos, val, valTy, out var len);
                    succ &= gen.TryGetType(destTy, out var retTy);
                    var retVal = ctx.GetStructValue(retTy, new[] { gep, len }, irb);
                    ctx.Branch(bMerge, irb);
                    ctx.ResetInsertPoint(bNull, irb);
                    var dflt = ctx.GetStructValue(retTy, new[] { ctx.GetNullPtr(), ctx.GetInt32(0) }, irb);
                    ctx.Branch(bMerge, irb);
                    ctx.ResetInsertPoint(bMerge, irb);
                    var phi = new PHINode(ctx, retTy, 2, irb);
                    phi.AddMergePoint(retVal, bNotNull);
                    phi.AddMergePoint(dflt, bNull);
                    ret = phi;
                    return succ;
                }
                else {
                    bool succ = TryGEPFirstElement(valTy, val, out var gep);
                    succ &= TryGetArrayLength(pos, val, valTy, out var len);
                    succ &= gen.TryGetType(destTy, out var retTy);
                    ret = ctx.GetStructValue(retTy, new[] { gep, len }, irb);
                    return succ;
                }
            }
            else if (destTy.IsInterface()) {
                if (valTy.IsInterface()) {
                    // DOLATER interface-to-interface-cast
                    ret = val;
                    return $"The interface-type {valTy.Signature} cannot be cast to another interface-type {destTy.Signature}".Report(pos, false);
                }
                if (valTy.ImplementsInterface(destTy)) {
                    if (!valTy.IsNotNullable())
                        ThrowIfNull(val);
                    Vector<uint> gepIdx = default;
                    gepIdx.Add(0);
                    foreach (var sup in gen.GetAllSuperTypes(valTy)) {
                        if (sup.ImplementsInterface(sup)) {
                            gepIdx.Add(0);
                        }
                        else
                            break;
                    }
                    gepIdx.Add(gen.interfaceVTableSlots[(valTy, destTy)]);
                    var vtablePtrPtr = ctx.GetElementPtrConstIdx(val, gepIdx.AsArray(), irb);
                    var vtablePtr = ctx.ForceCast(ctx.Load(vtablePtrPtr, irb), ctx.GetVoidPtr(), false, irb);
                    var basePtr = ctx.ForceCast(val, ctx.GetVoidPtr(), false, irb);

                    var mem = GetMemory(destTy);

                    ctx.StoreField(mem, new[] { ctx.GetInt32(0), ctx.GetInt32(0) }, basePtr, irb);
                    ctx.StoreField(mem, new[] { ctx.GetInt32(0), ctx.GetInt32(1) }, vtablePtr, irb);
                    ret = ctx.Load(mem, irb);
                    return true;
                }
                else {
                    ret = val;
                    const string helpMsg = "Try adding the interface into the interface-list in the class-header.";
                    return $"An object of the type {valTy.Signature} cannot be cast to the interface-type {destTy.Signature}, because it does not implement it explicitly. {(valTy is ClassType && !valTy.IsImport() ? helpMsg : "")}".Report(pos, false);
                }
            }
            else if (destTy.IsPrimitive(PrimitiveName.Bool)) {
                // cast to bool (operator bool)
                if (valTy.IsPrimitive()) {
                    ret = ctx.IsNotNull(val, irb);
                    return true;
                }
                else if (valTy.OverloadsOperator(OverloadableOperator.Bool, out var _mets)) {
                    // assert, that operator bool - overloads are never generic
                    var mets = _mets.OfType<IMethod>();
                    if (mets.First().IsStatic()) {
                        if (mets.First().Arguments[0].IsNotNull() && !valTy.IsNotNullable())
                            $"The nullable value of type {valTy.Signature} cannot be cast to bool, since the converter-function needs a nonull-value".Report(pos);
                        return TryCallCodeGen(pos, mets.First(), valTy, default, new[] { val }, out ret, false);
                    }
                    else {// operator bool - overloads are non-virtual
                        if (!valTy.IsNotNullable()) {

                            var notnull = ctx.IsNotNull(val, irb);
                            IntPtr bNull = new BasicBlock(ctx, "isNull", fn),
                                bNotNull = new BasicBlock(ctx, "isNotNull", fn),
                                bMerge = new BasicBlock(ctx, "nullMerge", fn);
                            ctx.ConditionalBranch(bNotNull, bNotNull, notnull, irb);
                            ctx.ResetInsertPoint(bNull, irb);
                            var dflt = ctx.False();
                            ctx.Branch(bMerge, irb);
                            ctx.ResetInsertPoint(bNotNull, irb);
                            var succ = TryCallCodeGen(pos, mets.First(), valTy, val, Array.Empty<IntPtr>(), out var retVal, false);
                            ctx.Branch(bMerge, irb);
                            ctx.ResetInsertPoint(bMerge, irb);
                            if (succ) {
                                var phi = new PHINode(ctx, ctx.GetBoolType(), 2, irb);
                                phi.AddMergePoint(dflt, bNull);
                                phi.AddMergePoint(retVal, bNotNull);
                                ret = phi;
                            }
                            else
                                ret = ctx.GetNullPtr();
                            return succ;
                        }
                        else if (valTy.IsValueType() && !valTy.IsPrimitive()) // cannot call functions on values, only on references
                            val = ctx.DefineTypeInferredInitializedAlloca(fn, val, "", irb, false);
                        return TryCallCodeGen(pos, mets.First(), valTy, val, Array.Empty<IntPtr>(), out ret, false);
                    }
                }
                else if (!isLocallyAllocated) {
                    ret = ctx.IsNotNull(val, irb);
                    return true;
                }
                else {
                    ret = ctx.GetNullPtr();
                    return $"An value of type {valTy.Signature} cannot be cast to bool".Report(pos, false);
                }
            }
            else if (!isLocallyAllocated && valTy.TryCast<ClassType>(out _) && destTy.TryCast<ClassType>(out _)) {

                if (throwOnError) {
                    var isInst = IsInstanceOf(val, valTy, destTy, true);
                    IntPtr
                        bTrue = new BasicBlock(ctx, "successfulCast", fn),
                        bFalse = new BasicBlock(ctx, "castError", fn);
                    ctx.ConditionalBranch(bTrue, bFalse, isInst, irb);
                    ctx.ResetInsertPoint(bFalse, irb);
                    ThrowException($"Invalid cast to {destTy.FullName()}");
                    ctx.ResetInsertPoint(bTrue, irb);
                    ret = IntPtr.Zero;
                    return gen.TryGetVariableType(destTy, isLocallyAllocated, out var tar)
                        && TryCast(val, tar, false, out ret);
                }
                else {
                    if (destTy.IsNotNullable())
                        "The destination-type for the 'as'-cast must be nullable".Report(pos);
                    var isInst = IsInstanceOf(val, valTy, destTy, false);
                    IntPtr bTrue = new BasicBlock(ctx, "successfulCast", fn),
                        bFalse = new BasicBlock(ctx, "unsuccessfulCast", fn),
                        bMerge = new BasicBlock(ctx, "castMerge", fn);
                    ctx.ConditionalBranch(bTrue, bFalse, isInst, irb);
                    bool succ = gen.TryGetVariableType(destTy, isLocallyAllocated, out var tar);
                    ctx.ResetInsertPoint(bTrue, irb);
                    TryCast(val, tar, false, out var retVal);
                    ctx.Branch(bMerge, irb);
                    ctx.ResetInsertPoint(bFalse, irb);
                    TryExpressionCodeGen(new DefaultValueExpression(pos, destTy), out var dflt);
                    ctx.Branch(bMerge, irb);
                    ctx.ResetInsertPoint(bMerge, irb);
                    var phi = new PHINode(ctx, tar, 2, irb);
                    phi.AddMergePoint(retVal, bTrue);
                    phi.AddMergePoint(dflt, bFalse);
                    ret = phi;
                    return succ;
                }
            }
            else {
                ret = IntPtr.Zero;
                return gen.TryGetVariableType(destTy, isLocallyAllocated, out var tar)
                    && TryCast(val, tar, destTy is PrimitiveType prim && prim.IsUnsignedInteger, out ret);
            }
        }
        //both types are classTypes and not locally allocated
        protected IntPtr IsInstanceOf(IntPtr obj, IType objTy, IType testTy, bool orNull) {
            if (objTy == testTy || Type.IsAssignable(objTy, testTy)) {
                return orNull || objTy.IsNotNullable() ? ctx.True() : ctx.IsNotNull(obj, irb);
            }
            else if (Type.IsAssignable(testTy, objTy)) {
                if (objTy.CanBeInherited()) {

                    if (objTy.IsNotNullable()) {
                        //var vtablePtr = ctx.LoadFieldConstIdx(obj, new[] { 0u, 0u }, irb);
                        //return ctx.IsInst(vtablePtr, testTy.FullName(), irb);
                        return IsInst(obj, objTy, testTy);
                    }
                    else {

                        var isnotnull = ctx.IsNotNull(obj, irb);
                        IntPtr bNull = new BasicBlock(ctx, "isNull", fn),
                            bNotNull = new BasicBlock(ctx, "isNotNull", fn),
                            bMerge = new BasicBlock(ctx, "nullMerge", fn);
                        ctx.ConditionalBranch(bNotNull, bNull, isnotnull, irb);
                        ctx.ResetInsertPoint(bNotNull, irb);

                        //var vtablePtr = ctx.LoadFieldConstIdx(obj, new[] { 0u, 0u }, irb);
                        var isInst = //ctx.IsInst(vtablePtr, testTy.FullName(), irb);
                            IsInst(obj, objTy, testTy);
                        ctx.Branch(bMerge, irb);
                        ctx.ResetInsertPoint(bNull, irb);
                        var ret = orNull ? ctx.True() : ctx.False();
                        ctx.Branch(bMerge, irb);
                        ctx.ResetInsertPoint(bMerge, irb);
                        var phi = new PHINode(ctx, ctx.GetBoolType(), 2, irb);
                        phi.AddMergePoint(ret, bNull);
                        phi.AddMergePoint(isInst, bNotNull);
                        return phi;
                    }
                }
                else {
                    return ctx.False();
                }
            }
            else {
                return ctx.False();
            }
        }

        protected internal bool TryCast(Position pos, IntPtr val, IType valTy, IVariable destTy, out IntPtr ret, bool throwOnError = true) {

            return TryCastInternal(pos, val, valTy, destTy.Type, valTy.IsValueType(), out ret, throwOnError);
        }
        public virtual bool TryInternalCallCodeGen(IMethod intnl, Span<IExpression> args, IntPtr par, IType parentTy, out IntPtr ret) {
            var func = GetOrCreateInternalFunction(ctx, intnl);
            var llArgs = new List<IntPtr>(3 * args.Length / 2);// factor 1,5: string-arguments require 2 low-level args
            bool succ = true;
            int index = 0;
            if (!intnl.IsStatic() && parentTy != null) {
                if (parentTy.IsString()) {
                    // internal member-functions on string require to gep and load pointer and length
                    llArgs.Add(ctx.LoadFieldConstIdx(par, new[] { 0u, 0u }, irb));
                    llArgs.Add(ctx.LoadFieldConstIdx(par, new[] { 0u, 1u }, irb));
                }
                else
                    llArgs.Add(par);

            }
            foreach (var arg in args) {
                succ &= TryExpressionCodeGen(arg, out var argEx);
                var formalArg = intnl.Arguments[index];
                succ &= TryCast(arg.Position, argEx, arg.ReturnType.UnWrapAll(), formalArg, out argEx);
                if (formalArg.Type.IsPrimitive(PrimitiveName.String)) {
                    llArgs.Add(ctx.ExtractValue(argEx, 0, irb));
                    llArgs.Add(ctx.ExtractValue(argEx, 1, irb));
                }
                else {
                    llArgs.Add(argEx);
                }
                index++;
            }
            ret = ctx.GetCall(func, llArgs.ToArray(), irb);
            return succ;
        }
        unsafe bool TryLiteralCodeGen(ILiteral lit, out IntPtr ret) {
            switch (lit) {
                case BigLongLiteral bll: {
                    ulong hi, lo;
                    lo = (ulong) (bll.Value & ulong.MaxValue);
                    hi = (ulong) ((bll.Value >> 64) & ulong.MaxValue);
                    ret = ctx.GetInt128(hi, lo);
                    return true;
                }
                case StringLiteral strLit: {
                    if (!stringLiterals.TryGetValue(strLit.Value, out ret)) {
                        ret = ctx.GetString(strLit.Value, irb);
                        stringLiterals[strLit.Value] = ret;
                    }
                    return true;
                }
                case FloatLiteral fl: {
                    ret = ctx.GetFloat(fl.Value);
                    return true;
                }
                case DoubleLiteral dl: {
                    ret = ctx.GetDouble(dl.Value);
                    return true;
                }
                case BoolLiteral bl: {
                    ret = bl.Value ? ctx.True() : ctx.False();
                    return true;
                }
                case ByteLiteral btl: {
                    ret = ctx.GetInt8(btl.Value);
                    return true;
                }
                case CharLiteral chl: {
                    ret = ctx.GetInt8((byte) chl.Value);
                    return true;
                }
                case UShortLiteral ushl: {
                    ret = ctx.GetInt16(ushl.Value);
                    return true;
                }
                case ShortLiteral shl: {
                    short s = shl.Value;
                    ret = ctx.GetInt16(*((ushort*) &s));
                    return true;
                }
                case UIntLiteral uil: {
                    ret = ctx.GetInt32(uil.Value);
                    return true;
                }
                case IntLiteral il: {
                    int i = il.Value;
                    ret = ctx.GetInt32(*((uint*) &i));
                    return true;
                }
                case ULongLiteral ull: {
                    ret = ctx.GetInt64(ull.Value);
                    return true;
                }
                case LongLiteral ll: {
                    long l = ll.Value;
                    ret = ctx.GetInt64(*((ulong*) &l));
                    return true;
                }
                case NullLiteral _: {
                    ret = ctx.GetNullPtr();
                    return true;
                }
            }
            throw new NotImplementedException();
        }
        bool TryAssignmentCodeGen(BinOp ass, out IntPtr ret) {
            //DOLATER consider copy-constructors for $-types
            bool succ = true;

            succ &= TryGetMemoryLocation(ass.Left, out var lhs);
            if (ass.Operator == BinOp.OperatorKind.ASSIGN_OLD) {
                ret = ctx.Load(lhs, irb);
            }
            else
                ret = default;
            ProvideMemoryForward(ass.ReturnType, lhs);
            succ &= TryExpressionCodeGen(ass.Right, out var rhs);
            if (ReturnMemoryForward(ass.Right)) {
                if (ass.Right.MinimalType() != ass.ReturnType) {
                    if (Type.IsAssignable(ass.Right.MinimalType(), ass.ReturnType)) {
                        //succ &= gen.TryGetType(ass.ReturnType, out var retTy);
                        //succ &= ctx.TryCast(rhs, retTy, ref rhs, ass.ReturnType is PrimitiveType prim && prim.IsUnsignedInteger, irb);
                        succ &= TryCast(ass.Right.Position, rhs, ass.Right.MinimalType(), ass.ReturnType, out rhs);
                    }
                    else {
                        succ = $"An object or value of type {ass.Right.MinimalType().Signature} cannot be assigned to a variable of type {ass.Left.ReturnType.Signature}".Report(ass.Position, false);
                    }
                }
                ctx.Store(lhs, rhs, irb);
            }
            if (ass.Operator == BinOp.OperatorKind.ASSIGN_NEW) {
                ret = rhs;
            }
            return succ;
        }
        bool TryLogicalChainCodeGen(BinOp lOp, out IntPtr ret) {
            // land, lor
            bool succ = true;
            IntPtr start = ctx.GetCurrentBasicBlock(irb);
            BasicBlock checkrhs, end;
            checkrhs = new BasicBlock(ctx, "checkRHS", fn);
            end = new BasicBlock(ctx, $"end{lOp.Operator}", fn);

            succ &= TryExpressionCodeGen(lOp.Left, out var lhs);
            if (lOp.Operator == BinOp.OperatorKind.LAND) {
                ctx.ConditionalBranch(checkrhs, end, lhs, irb);
            }
            else {
                ctx.ConditionalBranch(end, checkrhs, lhs, irb);
            }
            ctx.ResetInsertPoint(checkrhs, irb);

            succ &= TryExpressionCodeGen(lOp.Right, out var rhs);
            var secondRet = ctx.IsNotNull(rhs, irb);
            ctx.Branch(end, irb);
            ctx.ResetInsertPoint(end, irb);

            var phi = new PHINode(ctx, ctx.GetBoolType(), 2, irb);
            phi.AddMergePoint(secondRet, checkrhs);
            if (lOp.Operator == BinOp.OperatorKind.LAND) {
                phi.AddMergePoint(ctx.False(), start);
            }
            else {
                phi.AddMergePoint(ctx.True(), start);
            }
            ret = phi;
            return succ;
        }
        bool TryStringComparisonCodeGen(BinOp cmpOp, out IntPtr ret) {
            //TODO string compare
            throw new NotImplementedException();
        }
        bool TryStringComparisonCodeGen(IntPtr lhs, IntPtr rhs, out IntPtr ret) {
            throw new NotImplementedException();
        }
        bool TryComparisonCodeGen(BinOp cmpOp, out IntPtr ret) {
            if (cmpOp.Left.ReturnType.IsString() || cmpOp.Right.ReturnType.IsString()) {
                return TryStringComparisonCodeGen(cmpOp, out ret);
            }
            else {
                sbyte op = 0;
                bool orEqual = false;
                switch (cmpOp.Operator) {
                    case BinOp.OperatorKind.EQ:
                        orEqual = true;
                        op = (sbyte) '!';
                        break;
                    case BinOp.OperatorKind.NEQ:
                        orEqual = false;
                        op = (sbyte) '!';
                        break;
                    case BinOp.OperatorKind.LT:
                        orEqual = false;
                        op = (sbyte) '<';
                        break;
                    case BinOp.OperatorKind.LE:
                        orEqual = true;
                        op = (sbyte) '<';
                        break;
                    case BinOp.OperatorKind.GE:
                        orEqual = true;
                        op = (sbyte) '>';
                        break;
                    case BinOp.OperatorKind.GT:
                        orEqual = false;
                        op = (sbyte) '>';
                        break;
                }
                bool succ = true;
                succ &= TryExpressionCodeGen(cmpOp.Left, out var lhs);
                succ &= TryExpressionCodeGen(cmpOp.Right, out var rhs);
                RestoreSavedValue(cmpOp.Left, ref lhs);
                RestoreSavedValue(cmpOp.Right, ref rhs);
                if (succ) {
                    var mscs = Type.MostSpecialCommonSuperType(cmpOp.Left.ReturnType, cmpOp.Right.ReturnType);
                    gen.TryGetType(mscs, out var mscsTy);
                    lhs = ctx.ForceCast(lhs, mscsTy, cmpOp.Left.ReturnType.IsUnsignedNumericType(), irb);
                    rhs = ctx.ForceCast(rhs, mscsTy, cmpOp.Right.ReturnType.IsUnsignedNumericType(), irb);

                    ret = ctx.CompareOp(lhs, rhs, op, orEqual, mscs.IsUnsignedNumericType(), irb);
                    return true;
                }
                else {
                    ret = ctx.False();
                    return false;
                }
            }

        }
        bool TryStringBinopCodeGen(BinOp bo, out IntPtr ret) {
            // string overloads (comparison is already handled)
            IntPtr llFn;
            Vector<IntPtr> llArgs = default;
            bool succ = true;
            succ &= TryExpressionCodeGen(bo.Left, out var lhs) & TryExpressionCodeGen(bo.Right, out var rhs);
            if (!succ) {
                ret = ctx.GetString("", irb);
                return false;
            }
            RestoreSavedValue(bo.Left, ref lhs);
            RestoreSavedValue(bo.Right, ref rhs);
            switch (bo.Operator) {
                case BinOp.OperatorKind.ADD:
                    // leave multiconcat to the optimizer
                    /*if (bo.Left is BinOp lbo && lbo.Operator == BinOp.OperatorKind.ADD && lbo.ReturnType.IsString()) {
                        return TryStringMultiConcat(bo, out ret);
                    }*/
                    llFn = GetOrCreateInternalFunction(ctx, InternalFunction.strconcat_ret);
                    succ &=
                        TryCast(bo.Left.Position, lhs, bo.Left.ReturnType, PrimitiveType.String, out lhs)
                        && TryCast(bo.Right.Position, rhs, bo.Right.ReturnType, PrimitiveType.String, out rhs);
                    if (succ) {
                        llArgs.AddRange(ctx.ExtractValue(lhs, 0, irb),
                                        ctx.ExtractValue(lhs, 1, irb),
                                        ctx.ExtractValue(rhs, 0, irb),
                                        ctx.ExtractValue(rhs, 1, irb));
                    }
                    break;
                case BinOp.OperatorKind.MUL:
                    llFn = GetOrCreateInternalFunction(ctx, InternalFunction.strmul);
                    IExpression factorEx, strEx;
                    if (bo.Right.ReturnType.IsString()) {
                        // ensure, that the factor is the right-hand operand
                        (lhs, rhs) = (rhs, lhs);
                        factorEx = bo.Left;
                        strEx = bo.Right;
                    }
                    else {
                        strEx = bo.Left;
                        factorEx = bo.Right;
                    }
                    succ = succ && TryCast(factorEx.Position, rhs, factorEx.ReturnType, PrimitiveType.UInt, out rhs)
                        && TryCast(strEx.Position, lhs, strEx.ReturnType, PrimitiveType.String, out lhs);
                    if (succ) {
                        var mem = GetMemory(bo);
                        llArgs.AddRange(ctx.ExtractValue(lhs, 0, irb),
                                        ctx.ExtractValue(lhs, 1, irb),
                                        rhs, mem);
                        ctx.GetCall(llFn, llArgs.AsArray(), irb);
                        ret = ctx.Load(mem, irb);
                    }
                    else
                        ret = ctx.GetNullPtr();
                    return succ;

                default:
                    ret = default;
                    return $"The operator {bo.Operator} cannot be applied to strings".Report(bo.Position, false);
            }
            if (succ) {
                ret = ctx.GetCall(llFn, llArgs.AsArray(), irb);
            }
            else
                ret = default;
            return succ;
        }

        IEnumerable<IExpression> LinearizeStringAdd(BinOp bo) {
            // assume bo is string-concat
            IEnumerable<IExpression> ret;
            if (bo.Left is BinOp lbo && lbo.ReturnType.IsString() && lbo.Operator == BinOp.OperatorKind.ADD) {
                ret = LinearizeStringAdd(lbo);
            }
            else {
                ret = new[] { bo.Left };
            }
            if (bo.Right is BinOp rbo && rbo.ReturnType.IsString() && rbo.Operator == BinOp.OperatorKind.ADD) {
                ret = ret.Concat(LinearizeStringAdd(rbo));
            }
            else {
                ret = ret.Append(bo.Right);
            }
            return ret;
        }
        private bool TryStringMultiConcat(BinOp bo, out IntPtr ret) {
            return TryStringMultiConcat(bo, LinearizeStringAdd(bo), out ret);
        }
        private bool TryStringMultiConcat(IExpression expr, IEnumerable<IExpression> stringOperands, out IntPtr ret) {
            bool succ = true;
            var ops = stringOperands.ToArray();
            var mem = ctx.DefineAlloca(fn, ctx.GetArrayType(ctx.GetStringType(), (uint) ops.Length), "strings");

            var zero = ctx.GetInt32(0);
            var one = ctx.GetInt32(1);
            var index = zero;
            foreach (var ex in ops) {
                var strMem = ctx.GetElementPtr(mem, new[] { zero, index }, irb);
                ProvideMemoryForward(PrimitiveType.String, strMem);
                if (TryExpressionCodeGen(ex, out var llOp) && TryCast(ex.Position, llOp, ex.ReturnType, PrimitiveType.String, out llOp)) {
                    if (ReturnMemoryForward(ex)) {
                        ctx.Store(strMem, llOp, irb);
                    }
                }
                else {
                    ReturnMemoryForward(ex);
                    succ = false;
                }
                index = ctx.ArithmeticBinOp(index, one, (sbyte) '+', true, irb);
            }
            if (succ) {
                var llFn = GetOrCreateInternalFunction(ctx, InternalFunction.strmulticoncat);
                var llArgs = Vector<IntPtr>.Reserve(3);
                llArgs.Add(mem);
                llArgs.Add(index);
                var retMem = GetMemory(expr);
                llArgs.Add(retMem);
                ctx.GetCall(llFn, llArgs.AsArray(), irb);
                ret = ctx.Load(retMem, irb);
                return true;
            }
            ret = default;
            return false;
        }
        /// <summary>
        /// Already tested, that objTy is Supertype of ty
        /// </summary>
        /// <param name="obj">A pointer to the object to be tested</param>
        /// <param name="ty">The expected type</param>
        /// <returns></returns>
        public IntPtr IsInst(IntPtr obj, IType objTy, IType ty) {
            gen.TryGetVTable(ty.UnWrapAll(), out var vt, irb);
            TryLoadVTable(obj, objTy.UnWrapAll(), out var objVt);
            objVt = ctx.ForceCast(objVt, ctx.GetPointerType(vt.VTableType), false, irb);
            var isinst = ctx.LoadFieldConstIdx(objVt, new[] { 0u, vt.IsInstIndex }, irb);
            return ctx.GetCall(isinst, new[] { vt.VTablePointer }, irb);
        }
        protected bool TryLoadVTable(IntPtr objPtr, IType tp, out IntPtr vtable) {
            /* if (gen.vtables.TryGetValue(tp, out var vt)) {
                 var zero = ctx.GetInt32(0);
                 var gepIdx = new Vector<IntPtr>(vt.Level + 1);
                 gepIdx.Fill(zero);
                 vtable = ctx.LoadField(objPtr, gepIdx.AsArray(), irb);
                 return true;
             }
             vtable = IntPtr.Zero;
             return false;*/
            if (tp.IsInterface()) {
                vtable = ctx.ExtractValue(objPtr, 1, irb);
                return true;
            }
            else if (TryGepVTable(objPtr, tp, out vtable)) {
                vtable = ctx.Load(vtable, irb);
                return true;
            }
            return false;
        }
        protected bool TryGepVTable(IntPtr objPtr, IType tp, out IntPtr vtableGep) {
            /*if (tp.IsInterface()) {
                vtableGep = ctx.GetElementPtrConstIdx(objPtr, new[] { 0u, 1u }, irb);
                return true;
            }
            else */
            if (gen.TryGetVTable(tp, out var vt, irb)) {
                var zero = ctx.GetInt32(0);
                var gepIdx = new Vector<IntPtr>(vt.Level + 1);
                gepIdx.Fill(zero);
                vtableGep = ctx.GetElementPtr(objPtr, gepIdx.AsArray(), irb);
                return true;
            }
            vtableGep = IntPtr.Zero;
            return false;
        }
        protected bool TryGepInterfaceVTable(IntPtr objPtr, IType tp, IType intf, out IntPtr vtableGep) {
            if (gen.interfaceVTableSlots.TryGetValue((tp, intf), out var slot)) {
                Vector<uint> gepIdx = default;
                gepIdx.Add(0);
                foreach (var sup in gen.GetAllSuperTypes(tp)) {
                    if (sup.ImplementsInterface(intf))
                        gepIdx.Add(0);
                    else
                        break;
                }
                gepIdx.Add(slot);
                vtableGep = ctx.GetElementPtrConstIdx(objPtr, gepIdx.AsArray(), irb);
                return true;
            }
            else {

                vtableGep = IntPtr.Zero;
                return false;
            }
        }

        protected bool TryGetInterfaceVTableForType(Position pos, IType objTy, IType intfTy, ISemantics sem, out IntPtr vt) {
            if (interfaceVTables.TryGetValue((objTy, intfTy), out vt))
                return true;
            var vtVal = Vector<IntPtr>.Reserve((uint) intfTy.Context.InstanceContext.LocalContext.Methods.Count);
            var succ = true;
            var zero = ctx.GetNullPtr();
            foreach (var met in intfTy.Context.InstanceContext.LocalContext.Methods.Values) {
                var src = //GetCompatibleSource(met, objTy.Context.InstanceContext.MethodsByName(met.Signature.Name));
                    sem.BestFittingMethod(pos, objTy.Context.InstanceContext.MethodsByName(met.Signature.Name), met.Arguments.Select(x => x.Type).AsCollection(met.Arguments.Length), met.ReturnType);

                if (src is null) {
                    vt = IntPtr.Zero;
                    succ = $"The type {objTy.Signature} does not implement the provided interface {intfTy.Signature}".Report(pos, false);
                }
                else {

                    var slot = gen.GetVirtualMethodSlot(met, irb);
                    succ &= gen.TryGetMethod(src, out var fn);
                    if (fn == IntPtr.Zero || fn == zero)
                        succ = $"The method {src} which implements the method from the interface {intfTy.Signature} has no body".Report(pos, false);
                    vtVal[slot] = fn;
                }
            }
            if (succ) {
                var vtTy = ctx.GetVTableType(objTy.FullName() + "#interface_" + intfTy.FullName(), vtVal.Select(x => ctx.GetFunctionPtrTypeFromFunction(x)).ToArray());
                vt = ctx.GetVTable(objTy.FullName() + "#interface_" + intfTy.FullName(), vtTy, vtVal.AsArray(), IntPtr.Zero);
            }
            interfaceVTables.Add((objTy, intfTy), vt);
            return succ && vtVal.All(x => x != IntPtr.Zero);
        }
        protected bool TryArgumentCodeGen(IExpression actualArg, IVariable formalArg, out IntPtr ret) {
            bool succ = true;
            if (actualArg is DefaultValueExpression) {
                actualArg = DefaultValueExpression.Get(formalArg.Type.UnWrap());
            }
            if (formalArg.Type.IsByRef() && !(formalArg.Type.IsValueType() && !formalArg.Type.IsPrimitive() && !actualArg.ReturnType.IsValueType()))
                succ &= TryGetMemoryLocation(actualArg, out ret);
            else
                succ &= TryExpressionCodeGen(actualArg, out ret);
            if (succ && actualArg.ReturnType != formalArg.Type.UnWrap()) {
                succ &= gen.TryGetType(formalArg.Type.UnWrap(), out var formTp);
                //succ &= ctx.TryCast(ret, formTp, ref ret, actualArg.ReturnType is PrimitiveType prim && prim.IsUnsignedInteger, irb);
                succ &= TryCast(actualArg.Position, ret, actualArg.ReturnType, formalArg, out ret);
            }
            return succ;
        }
        protected bool TryCallCodeGen(Position pos, IMethod callee, IType parentTy, IntPtr par, IEnumerable<IntPtr> actualArgs, out IntPtr ret, bool isCallVirt, bool isLValue = false) {

            bool succ = true;
            IntPtr fn;
            if (callee.IsInternal())
                fn = GetOrCreateInternalFunction(ctx, callee.Signature.InternalName);
            else
                succ &= gen.TryGetMethod(callee, out fn);
            //var args = Vector<IntPtr>.Reserve((uint)callee.Arguments.Length + (callee.IsStatic() ? 0u : 1u));
            if (!callee.IsStatic() && parentTy != null) {
                // if retType is locally allocated, then use TryGetMemoryLocation instead

                //TODO null check for par

                if (parentTy != (callee.NestedIn as ITypeContext).Type) {
                    succ &= gen.TryGetReferenceType((callee.NestedIn as ITypeContext).Type, out var thisTy);
                    //succ &= ctx.TryCast(par, thisTy, ref par, false, irb);
                    succ &= TryCast(pos, par, parentTy, (callee.NestedIn as ITypeContext).Type.ReceiverType(callee), out ret, false);
                }
                if (isCallVirt) {
                    if (gen.TryGetVirtualMethodSlot(callee, out var slot, irb)) {
                        var vtData = //gen.vtables[parentTy];
                            gen.GetVTable(parentTy.UnWrapAll(), irb);
                        if (TryLoadVTable(par, parentTy.UnWrapAll(), out var vt)) {
                            Assert.assert(vtData.IsVTable);
                            vt = ctx.ForceCast(vt, ctx.GetPointerType(vtData.VTableType), false, irb);
                            var gep = ctx.GetElementPtrConstIdxWithType(vtData.VTableType, vt, new[] { 0u, slot }, irb);
                            fn = ctx.Load(gep, irb);
                            succ &= gen.TryGetFunctionPtrType(callee, out var fnPtrTy) && ctx.TryCast(fn, fnPtrTy, ref fn, false, irb);

                            // load function from functionpointer?
                            //fn = ctx.Load(fn, irb);
                        }
                        else {
                            succ = $"The method {callee.Signature} cannot be called virtually from an object of type {parentTy.Signature}, since it does not support dynamic method-binding".Report(pos, false);
                        }
                    }
                    else {
                        succ = $"The method {callee.Signature} cannot be called virtually, since it is not declared as virtual".Report(pos, false);
                    }
                }
                if (succ && parentTy.IsInterface()) {
                    // The basePtr is at index 0 in an interface

                    par = ctx.ExtractValue(par, 0, irb);
                    /* par = ctx.GetElementPtrConstIdx(par, new[] { 0u, 0u }, irb);
                     par = ctx.Load(par, irb);*/

                }
                //args.Add(par);
                actualArgs = actualArgs.Prepend(par);
            }
            else if (!callee.IsStatic()) {
                ret = IntPtr.Zero;
                return $"The non-static method {callee.Signature} must be called on an object".Report(pos, false);
            }

            ret = ctx.GetCall(fn, actualArgs.ToArray(), irb);
            if (isLValue) {
                if (!callee.ReturnType.IsByRef()) {
                    if (succ &= gen.TryGetType(callee.ReturnType, out var retTy)) {
                        var mem = ctx.DefineAlloca(fn, retTy, "");
                        ctx.Store(mem, ret, irb);
                        ret = mem;
                    }
                }
            }
            else {
                if (callee.ReturnType.IsByRef()) {
                    ret = ctx.Load(ret, irb);
                }
            }
            return succ;
        }
        protected bool TryCallCodeGen(Position pos, IMethod callee, IType parentTy, IntPtr par, Span<IExpression> actualArgs, out IntPtr ret, bool isCallVirt, bool isLValue = false) {
            bool succ = true;
            if (callee.IsInternal())
                return TryInternalCallCodeGen(callee, actualArgs, par, parentTy, out ret);

            succ &= gen.TryGetMethod(callee, out var fn);
            var args = Vector<IntPtr>.Reserve((uint) callee.Arguments.Length + (callee.IsStatic() ? 0u : 1u));
            if (!callee.IsStatic() && parentTy != null) {
                // if retType is locally allocated, then use TryGetMemoryLocation instead

                if (parentTy != (callee.NestedIn as ITypeContext).Type) {
                    succ &= gen.TryGetReferenceType((callee.NestedIn as ITypeContext).Type, out var thisTy);
                    //succ &= ctx.TryCast(par, thisTy, ref par, false, irb);
                    succ &= TryCast(pos, par, parentTy, (callee.NestedIn as ITypeContext).Type.ReceiverType(callee), out ret, false);
                }
                if (isCallVirt) {
                    if (gen.TryGetVirtualMethodSlot(callee, out var slot, irb)) {
                        var vtData = //gen.vtables[parentTy];
                            gen.GetVTable(parentTy, irb);
                        if (TryLoadVTable(par, parentTy, out var vt)) {
                            vt = ctx.ForceCast(vt, ctx.GetPointerType(vtData.VTableType), false, irb);
                            var gep = ctx.GetElementPtrConstIdxWithType(vtData.VTableType, vt, new[] { 0u, slot }, irb);
                            fn = ctx.Load(gep, irb);
                            succ &= gen.TryGetFunctionPtrType(callee, out var fnPtrTy) && ctx.TryCast(fn, fnPtrTy, ref fn, false, irb);

                            // load function from functionpointer?
                            //fn = ctx.Load(fn, irb);
                        }
                        else {
                            succ = $"The method {callee.Signature} cannot be called virtually from an object of type {parentTy.Signature}, since it does not support dynamic method-binding".Report(pos, false);
                        }
                    }
                    else {
                        succ = $"The method {callee.Signature} cannot be called virtually, since it is not declared as virtual".Report(pos, false);
                    }
                }
                if (succ && parentTy.IsInterface()) {
                    // The basePtr is at index 0 in an interface
                    par = ctx.ExtractValue(par, 0, irb);

                    /*par = ctx.GetElementPtrConstIdx(par, new[] { 0u, 0u }, irb);
                    par = ctx.Load(par, irb);*/
                }
                args.Add(par);
            }
            else if (!callee.IsStatic()) {
                ret = IntPtr.Zero;
                return $"The non-static method {callee.Signature} must be called on an object".Report(pos, false);
            }
            int index = 0;
            Span<IExpression> varArgParameters;
            if (callee.Arguments.Any() && callee.Arguments.Last().Type.IsVarArg()) {
                varArgParameters = actualArgs.Slice(callee.Arguments.Length - 1);
                actualArgs = actualArgs.Slice(0, callee.Arguments.Length - 1);
            }
            else
                varArgParameters = default;
            foreach (var arg in actualArgs) {
                /*succ &= TryExpressionCodeGen(arg, out var fnArg);
                if (arg.ReturnType != callee.Arguments[index].Type) {
                    succ &= gen.TryGetVariableType(callee.Arguments[index], out var formTp);
                    succ &= ctx.TryCast(fnArg, formTp, ref fnArg, arg.ReturnType is PrimitiveType prim && prim.IsUnsignedInteger, irb);
                }*/
                succ &= TryArgumentCodeGen(arg, callee.Arguments[index], out var fnArg);
                args.Add(fnArg);
                index++;
            }
            if (varArgParameters.Length > 0) {
                succ &= TryCreateVarArg(varArgParameters, (callee.Arguments.Last().Type as VarArgType).ItemType, out var vaa);
                args.Add(vaa);
            }
            if (args.Length > 0) {
                for (uint i = 0; i < args.Length - 1; ++i) {
                    RestoreSavedValue(actualArgs[(int) i], ref args[i]);
                }
            }
            ret = ctx.GetCall(fn, args.AsArray(), irb);
            if (isLValue) {
                if (!callee.ReturnType.IsByRef()) {
                    if (succ &= gen.TryGetType(callee.ReturnType, out var retTy)) {
                        var mem = ctx.DefineAlloca(fn, retTy, "");
                        ctx.Store(mem, ret, irb);
                        ret = mem;
                    }
                }
            }
            else {
                if (callee.ReturnType.IsByRef()) {
                    ret = ctx.Load(ret, irb);
                }
            }

            return succ;
        }
        private IntPtr CreateStackallocSpan(Vector<IntPtr> elems, IntPtr spanTy, IntPtr itemTy) {
            var array = ctx.DefineAlloca(fn, ctx.GetArrayType(itemTy, elems.Length), "varArg.SingleElems");
            for (uint i = 0; i < elems.Length; ++i) {
                ctx.StoreFieldConstIdx(array, new[] { 0u, i }, elems[i], irb);
            }
            var span = ctx.DefineAlloca(fn, spanTy, "varArg.SingleElements.Span");
            ctx.StoreFieldConstIdx(span, new[] { 0u, 0u }, ctx.GetElementPtrConstIdx(array, new[] { 0u, 0u }, irb), irb);
            ctx.StoreFieldConstIdx(span, new[] { 0u, 1u }, ctx.GetInt32(elems.Length), irb);
            return ctx.Load(span, irb);

        }
        private bool TryCreateVarArg(Span<IExpression> varArgParameters, IType itemTp, out IntPtr ret) {
            if (varArgParameters.Length == 1) {
                if (varArgParameters[0].IsUnpack(out IExpression up) && up.ReturnType.IsVarArg()) {
                    return TryExpressionCodeGen(up, out ret);
                }
            }
            // TODO: create byref varargs
            ret = default;
            if (!gen.TryGetType(itemTp, out var itemTy) | !gen.TryGetType(VarArgType.Get(itemTp), out var varArgTy))
                return false;
            // struct vararg<T>{ span<T>* spans, uint spanc, size_t totalLen, size_t totalOffset};
            var spanTy = gen.GetSpanType(SpanType.Get(itemTp), itemTy);
            bool succ = true;
            var spans = Vector<IntPtr>.Reserve((uint) varArgParameters.Length);
            var one = ctx.GetIntSZ(1);
            var totalLen = ctx.GetIntSZ(0);
            var singleElems = new Vector<IntPtr>();
            var singleElemExs = new Vector<IExpression>();

            void HandleSingleElements() {
                if (singleElems.Any()) {
                    for (uint i = 0; i < singleElems.Length; ++i) {
                        RestoreSavedValue(singleElemExs[i], ref singleElems[i]);
                    }
                    spans.Add(CreateStackallocSpan(singleElems, spanTy, itemTy));
                    singleElems = new Vector<IntPtr>();
                    singleElemExs = new Vector<IExpression>();
                }
            }


            foreach (var elem in varArgParameters) {
                if (elem.IsUnpack(out IExpression _)) {
                    HandleSingleElements();
                    // create vararg from unpack-expressions
                    var range = (elem as UnOp).SubExpression;
                    var rangeTy = range.ReturnType.UnWrapAll();
                    IntPtr span, spanLen;
                    if (rangeTy.IsArraySlice()) {
                        succ &= TryExpressionCodeGen(range, out span);
                        spanLen = ctx.ExtractValue(span, 1, irb);
                    }
                    else if (rangeTy.IsArray()) {
                        succ &= TryRangeExpressionCodeGen(range, out var arr);
                        if (!range.IsNotNullable()) {
                            ThrowIfNull(arr);
                        }
                        succ &= TryGetArrayLength(range.Position, arr, range.ReturnType, out spanLen);
                        succ &= TryGEPFirstElement(range.ReturnType, arr, out var spanValue);
                        span = ctx.GetStructValue(spanTy, new[] { spanValue, spanLen }, irb);
                    }
                    else if (rangeTy.IsVarArg()) {
                        //TODO nested vararg
                        throw new NotImplementedException();
                    }
                    else {
                        "Invalid unpack-expression".Report(elem.Position);
                        continue;
                    }
                    totalLen = ctx.ArithmeticBinOp(totalLen, spanLen, (sbyte) '+', true, irb);
                    spans.Add(span);
                    singleElems = new Vector<IntPtr>();
                    singleElemExs = new Vector<IExpression>();
                }
                else {
                    succ &= TryExpressionCodeGen(elem, out var sElem);
                    singleElems.Add(sElem);
                    singleElemExs.Add(elem);
                    totalLen = ctx.ArithmeticBinOp(totalLen, one, (sbyte) '+', true, irb);
                }
            }
            HandleSingleElements();
            if (!succ)
                return false;
            var spanArray = ctx.DefineAlloca(fn, ctx.GetArrayType(spanTy, spans.Length), "vararg.spans");
            var spanPtr = ctx.GetElementPtrConstIdx(spanArray, new[] { 0u, 0u }, irb);

            for (uint i = 0; i < spans.Length; ++i) {
                var elemPtr = ctx.GetElementPtrConstIdx(spanArray, new[] { 0u, i }, irb);
                ctx.Store(elemPtr, spans[i], irb);
            }
            var varArg = ctx.DefineAlloca(fn, varArgTy, "varArg");
            ctx.StoreFieldConstIdx(varArg, new[] { 0u, 0u }, spanPtr, irb);
            ctx.StoreFieldConstIdx(varArg, new[] { 0u, 1u }, ctx.GetInt32(spans.Length), irb);
            ctx.StoreFieldConstIdx(varArg, new[] { 0u, 2u }, totalLen, irb);
            ctx.StoreFieldConstIdx(varArg, new[] { 0u, 3u }, ctx.GetIntSZ(0), irb);
            ret = ctx.Load(varArg, irb);
            return succ;
        }
        protected bool RestoreSavedValue(IExpression ex, ref IntPtr exVal) {
            if (coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(ex, out var ind)) {
                exVal = ctx.LoadFieldConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, ind }, irb);
                return true;
            }
            return false;
        }

        protected bool TryCallCodeGen(Position pos, IMethod callee, IExpression parent, Span<IExpression> actualArgs, out IntPtr ret, bool isCallVirt, bool isLValue = false) {
            bool succ = true;
            IntPtr par;
            IType parentTy;
            if (parent != null) {
                parentTy = parent.ReturnType;
                succ &= TryRefExpressionCodeGen(parent, out par);

            }
            else {
                parentTy = null;
                par = IntPtr.Zero;
            }

            return succ & TryCallCodeGen(pos, callee, parentTy, par, actualArgs, out ret, isCallVirt, isLValue);
        }
        protected internal virtual bool TryGetArrayLength(Position pos, IntPtr arr, IType arrTp, out IntPtr ret) {

            // %array_T = type { i32, [N x T] }
            /*if (arrTp.IsFixedSizedArray()) {
                ret = ctx.ExtractValue(arr, 0, irb);
            }
            else */
            if (arrTp.IsArray()) {
                var lenGEP = ctx.GetElementPtrConstIdx(arr, new[] { 0u, 0u }, irb);
                ret = ctx.Load(lenGEP, irb);
            }
            else if (arrTp.IsArraySlice()) {
                ret = ctx.ExtractValue(arr, 1, irb);
            }
            else if (arrTp.IsVarArg()) {
                // vararg-length
                //                                                      v
                // vararg<T> = type { span<T>* value, uint valuec, uint length, uint offset }
                ret = ctx.ExtractValue(arr, 2, irb);
            }
            else {
                ret = IntPtr.Zero;
                return "The non-array value has no array-length".Report(pos, false);
            }
            return true;
        }
        protected virtual bool TryVariableAccessCodeGen(VariableAccessExpression vr, out IntPtr ret) {
            bool succ = true;
            if (variables.TryGetValue(vr.Variable, out ret) || gen.staticVariables.TryGetValue(vr.Variable, out ret)) {// local variable
                                                                                                                       // the memory-location is the alloca
                ret = ctx.Load(ret, irb);
                return true;
            }
            else if (coro.LocalVariableIndex != null && coro.LocalVariableIndex.TryGetValue(vr.Variable, out var ind)) {
                ret = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, ind }, irb);
                ret = ctx.Load(ret, irb);
                return true;
            }
            else if (vr.ParentExpression != null && (vr.Variable is ArrayLength || vr.Variable is SpanLength)) {
                if (!TryRangeExpressionCodeGen(vr.ParentExpression, out var arr))
                    return false;

                return TryGetArrayLength(vr.Position, arr, vr.ParentExpression.ReturnType, out ret);
            }
            else if (vr.ParentExpression != null) {
                // instance-field
                var par = vr.Variable.DefinedInType;
                if (par is null) {
                    return $"The instance-field {vr.Variable.Signature} must be declared inside a class".Report(vr.Position, false);
                }
                else if (!vr.ParentExpression.ReturnType.IsSubTypeOf(par)) {
                    succ &= $"The object does not contain an instance-field {vr.Variable.Signature}".Report(vr.Position, false);
                }
                succ &= TryExpressionCodeGen(vr.ParentExpression, out var parent);
                var gepIndices = new List<uint>();
                bool isRef = IsReference(vr.ParentExpression);
                if (isRef) // not indexing an array
                    gepIndices.Add(0);
                var parTmp = vr.ParentExpression.ReturnType.UnWrap();
                while (par != parTmp) {
                    //since retTy is subclass of par, retTy must have an object of type par inside
                    gepIndices.Add(gen.GetSuperPosition(parTmp));
                    parTmp = (parTmp as IHierarchialType).SuperType;
                }

                gepIndices.Add(gen.instanceFields[vr.Variable]);
                if (!isRef) {
                    ret = ctx.ExtractNestedValue(parent, gepIndices.ToArray(), irb);
                }
                else {
                    ret = ctx.GetElementPtrConstIdx(parent, gepIndices.ToArray(), irb);
                    ret = ctx.Load(ret, irb);
                }

                return succ;
            }
            else {
                return "Invalid instance-field access".Report(vr.Position, false);
            }
        }

        protected virtual bool TryExpressionCodeGenInternal(IExpression expr, out IntPtr ret) {
            //bool succ = true;
            switch (expr) {
                case null: {
                    ret = IntPtr.Zero;
                    return false;
                }
                case CallExpression call: {
                    return TryCallCodeGen(call.Position, call.Callee, call.ParentExpression, call.Arguments, out ret, call.IsCallVirt);
                }
                case ILiteral lit: {
                    return TryLiteralCodeGen(lit, out ret);
                }
                case VariableAccessExpression vr: {
                    return TryVariableAccessCodeGen(vr, out ret);
                }
                case BinOp bo: {
                    return TryBinOpCodeGen(bo, out ret);
                }
                case UnOp uo: {
                    return TryUnOpCodeGen(uo, out ret);
                }
                case TypecastExpression tce: {

                    return TryRangeExpressionCodeGen(tce.SubExpression, out ret)
                        && TryCast(tce.Position, ret, tce.SubExpression.ReturnType, tce.ReturnType, out ret, tce.SubExpression.ReturnType.IsArray() ? false : default(bool?), tce.ThrowOnError);
                }
                case ThisExpression _: {
                    ret = ctx.GetArgument(fn, 0);

                    if (coro.LocalVariableIndex != null) {
                        ret = ctx.LoadFieldConstIdx(ret, new[] { 0u, coro.ThisIndex }, irb);
                    }
                    // already verified, that fn is an instance method
                    return true;
                }
                case BaseExpression be: {
                    ret = ctx.GetArgument(fn, 0);

                    if (coro.LocalVariableIndex != null) {
                        ret = ctx.LoadFieldConstIdx(ret, new[] { 0u, coro.ThisIndex }, irb);
                    }

                    return gen.TryGetReferenceType(be.ReturnType, out var baseTp) && ctx.TryCast(ret, baseTp, ref ret, false, irb);
                }
                case IndexerExpression arrInd: {
                    return TryArrayIndexerCodeGen(arrInd, out ret);
                }
                case ArrayInitializerExpression arrInit: {
                    return TryArrayInitializerCodeGen(arrInit, out ret);
                }
                case DefaultValueExpression dflt: {
                    if (dflt.ReturnType.IsVoid() || dflt.ReturnType.IsTop()) {
                        ret = ctx.GetNullPtr();
                        if (dflt.ReturnType.IsVoid())
                            return "The void-type has no default-value".Report(dflt.Position, false);
                        else
                            return "The type of the default-expression could not be inferred. Try to typecast it to the expected type.".Report(dflt.Position, false);
                    }
                    if (gen.TryGetType(dflt.ReturnType, out var ty)) {
                        //DOLATER call the default-constructor for value-types
                        ret = ctx.GetAllZeroValue(ty);
                        return true;
                    }
                    else {
                        ret = ctx.GetNullPtr();
                        return false;
                    }

                }
                case ConditionalExpression cex: {
                    return TryConditionalExpressionCodeGen(cex, out ret);
                }
                case RangedIndexerExpression rinde: {
                    return TryRangeBasedIndexerExpressionCodeGen(rinde, out ret);
                }
                case IndexerSetOverload iset: {
                    return TryIndexerSetOverloadCodeGen(iset, out ret);
                }
                //TODO expr
            }
            throw new NotImplementedException();
        }

        private bool TryIndexerSetOverloadCodeGen(IndexerSetOverload iset, out IntPtr ret) {
            return TryExpressionCodeGen(iset.OperatorCall, out ret);
        }

        private bool TryRangeBasedIndexerExpressionCodeGen(RangedIndexerExpression rinde, out IntPtr ret) {
            if (rinde.Parent.ReturnType.IsArray())
                return TryRangeBasedArrayIndexerExpressionCodeGen(rinde, out ret);
            else if (rinde.Parent.ReturnType.IsVarArg())
                return TryRangeBasedVarArgIndexerExpressionCodeGen(rinde, out ret);
            else {
                ret = ctx.GetNullPtr();
                return $"Values of type {rinde.Parent.ReturnType.Signature} cannot be indexed by range".Report(rinde.Position, false);
            }
        }
        private bool TryRangeBasedVarArgIndexerExpressionCodeGen(RangedIndexerExpression rinde, out IntPtr ret) {
            bool succ = true;
            if (!TryExpressionCodeGen(rinde.Parent, out var vaa)) {
                ret = ctx.GetNullPtr();
                return false;
            }
            succ &= gen.TryGetType(rinde.ReturnType, out var retTy);
            succ &= TryGetArrayLength(rinde.Position, vaa, rinde.Parent.ReturnType, out var len);
            succ &= TryExpressionCodeGen(rinde.Offset, out var offs);
            IntPtr count;
            if (rinde.IsOffsetToEnd)
                count = len;
            else
                succ &= TryExpressionCodeGen(rinde.Count, out count);

            var dfltVaa = ctx.GetConstantStruct(retTy, new[] { ctx.GetNullPtr(), ctx.GetInt32(0), ctx.GetIntSZ(0), ctx.GetIntSZ(0) });
            var spans = ctx.ExtractValue(vaa, 0, irb);
            var spanc = ctx.ExtractValue(vaa, 1, irb);
            var nwOffs = ctx.ArithmeticBinOp(offs, ctx.ExtractValue(vaa, 3, irb), (sbyte) '+', true, irb);

            var lenMinusOffs = ctx.ArithmeticBinOp(len, offs, (sbyte) '-', true, irb);
            var length = ctx.GetMin(count, lenMinusOffs, true, irb);
            var lenSmallerOffs = ctx.CompareOp(len, offs, (sbyte) '<', false, true, irb);

            var retVaa = ctx.GetStructValue(retTy, new[] { spans, spanc, length, nwOffs }, irb);
            ret = ctx.ConditionalSelect(dfltVaa, retVaa, lenSmallerOffs, irb);
            return succ;
        }
        private bool TryRangeBasedArrayIndexerExpressionCodeGen(RangedIndexerExpression rinde, out IntPtr ret) {
            bool succ = true;
            if (!TryRangeExpressionCodeGen(rinde.Parent, out var par)) {
                ret = ctx.GetNullPtr();
                return false;
            }
            succ &= gen.TryGetType(rinde.ReturnType, out var retTy);

            succ &= TryGetArrayLength(rinde.Position, par, rinde.Parent.ReturnType, out var len);
            succ &= TryExpressionCodeGen(rinde.Offset, out var offs);
            IntPtr count;
            if (rinde.IsOffsetToEnd) {
                count = len;
            }
            else
                succ &= TryExpressionCodeGen(rinde.Count, out count);

            var dfltSpan = ctx.GetConstantStruct(retTy, new[] { ctx.GetNullPtr(), ctx.GetInt32(0) });

            succ &= TryGEPNthElement(rinde.Parent.ReturnType, par, offs, out var pointer);

            // lenMinusOffset will underflow, when len < offs
            var lenMinusOffs = ctx.ArithmeticBinOp(len, offs, (sbyte) '-', true, irb);
            var length = ctx.GetMin(count, lenMinusOffs, true, irb);

            var lenSmallerOffs = ctx.CompareOp(len, offs, (sbyte) '<', false, true, irb);

            var retSpan = ctx.GetStructValue(retTy, new[] { pointer, length }, irb);

            ret = ctx.ConditionalSelect(dfltSpan, retSpan, lenSmallerOffs, irb);
            return succ;
        }

        private bool TryConditionalExpressionCodeGen(ConditionalExpression cex, out IntPtr ret) {
            bool succ = true;
            succ &= TryExpressionCodeGen(cex.Condition, out var cond);
            if (!cex.Condition.ReturnType.IsPrimitive(PrimitiveName.Bool)) {
                if (succ)
                    succ &= TryCast(cex.Condition.Position, cond, cex.ReturnType, PrimitiveType.Bool, out cond);
            }
            succ &= gen.TryGetType(cex.ReturnType, out var retTy);
            IntPtr
                bTrue = new BasicBlock(ctx, "caseTrue", fn),
                bFalse = new BasicBlock(ctx, "caseFalse", fn),
                bMerge = new BasicBlock(ctx, "mergeCases", fn);
            ctx.ConditionalBranch(bTrue, bFalse, cond, irb);
            ctx.ResetInsertPoint(bTrue, irb);
            succ &= TryExpressionCodeGen(cex.ThenExpression, out var resThen) && TryCast(cex.ThenExpression.Position, resThen, cex.ThenExpression.ReturnType, cex.ReturnType, out resThen);
            // current bb may have changed
            bTrue = ctx.GetCurrentBasicBlock(irb);
            ctx.Branch(bMerge, irb);
            ctx.ResetInsertPoint(bFalse, irb);
            succ &= TryExpressionCodeGen(cex.ElseExpression, out var resElse) && TryCast(cex.ElseExpression.Position, resElse, cex.ElseExpression.ReturnType, cex.ReturnType, out resElse);
            bFalse = ctx.GetCurrentBasicBlock(irb);
            ctx.Branch(bMerge, irb);
            ctx.ResetInsertPoint(bMerge, irb);
            var phi = new PHINode(ctx, retTy, 2, irb);
            phi.AddMergePoint(resThen, bTrue);
            phi.AddMergePoint(resElse, bFalse);
            ret = phi;
            return succ;
        }

        public bool TryExpressionCodeGen(IExpression expr, out IntPtr ret) {
            var toret = TryExpressionCodeGenInternal(expr, out ret);
            if (coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(expr, out var slot)) {
                ctx.StoreFieldConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, slot }, ret, irb);
            }
            return toret;
        }

        private bool TryArrayInitializerCodeGen(ArrayInitializerExpression arrInit, out IntPtr ret) {
            bool succ = true;
            IntPtr len = ctx.GetInt32((uint) arrInit.Elements.Count(x => !x.IsUnpack()));
            var rangeCache = new Dictionary<IExpression, IntPtr>();
            foreach (var arr in arrInit.Elements.Where(x => x.IsUnpack()).Select(x => (x as UnOp).SubExpression)) {
                IntPtr arrLen = ctx.GetInt32(0);
                succ &= TryRangeExpressionCodeGen(arr, out var llArr)
                     && TryGetArrayLength(arr.Position, llArr, arr.ReturnType, out arrLen);
                len = ctx.ArithmeticBinOp(len, arrLen, (sbyte) '+', true, irb);
                rangeCache[arr] = llArr;
            }
            if (!succ) {
                ret = default;
                return false;
            }
            var itemTp = (arrInit.ReturnType as IWrapperType).ItemType;
            if (!gen.TryGetType(itemTp, out var itemTy)) {
                ret = default;
                return false;
            }
            IntPtr mem;
            if (arrInit.ReturnType.IsFixedSizedArray()) {
                mem = GetMemory(arrInit);
            }
            else {

                var byteLen = ctx.ArithmeticBinOp(len, ctx.GetI32SizeOf(itemTy), (sbyte) '*', true, irb);
                byteLen = ctx.ArithmeticBinOp(byteLen, ctx.GetI32SizeOf(gen.GetArrayStructType(itemTy, 0)), (sbyte) '+', true, irb);
                var malloc = ctx.DeclareMallocFunction(InternalFunction.gc_new.ToString(), new[] { ctx.GetSizeTType() }, true);
                mem = ctx.GetCall(malloc, new[] { byteLen }, irb);
                if (!gen.TryGetType(arrInit.ReturnType, out var retTy)) {
                    ret = mem;
                    return false;
                }
                mem = ctx.ForceCast(mem, retTy, false, irb);
            }

            var zero = ctx.GetInt32(0);
            var one = ctx.GetInt32(1);

            ctx.StoreField(mem, new[] { zero, zero }, len, irb);
            #region initialize array
            IntPtr index = zero;
            foreach (var elem in arrInit.Elements) {
                //TODO valueType-copy-ctor
                if (!elem.IsUnpack()) {
                    if (TryExpressionCodeGen(elem, out var llElem)) {
                        ctx.StoreField(mem, new[] { zero, one, index }, ctx.ForceCast(llElem, itemTy, itemTp.IsUnsignedNumericType(), irb), irb);
                    }
                    index = ctx.ArithmeticBinOp(index, ctx.GetInt32(1), (sbyte) '+', true, irb);
                }
                else {

                    var elemArrTy = (elem as UnOp).SubExpression.ReturnType;

                    if (rangeCache.TryGetValue((elem as UnOp).SubExpression, out var llRange) && TryGetArrayLength(elem.Position, llRange, elemArrTy, out var elemLen)) {
                        if (elemArrTy.IsVarArg()) {
                            succ &= TryCopyVarArg(arrInit.ReturnType, mem, index, elemArrTy as VarArgType, llRange, zero, elemLen, true);
                        }
                        else {
                            succ &= TryCopyRange(arrInit.ReturnType, mem, index, elemArrTy, llRange, zero, elemLen, true);
                        }
                        index = ctx.ArithmeticBinOp(index, elemLen, (sbyte) '+', true, irb);
                    }
                }

            }
            #endregion

            if (arrInit.ReturnType.IsFixedSizedArray()) {
                ret = ctx.Load(mem, irb);
            }
            else {
                ret = mem;
            }
            return succ;
        }

        private bool TryUnOpCodeGen(UnOp uo, out IntPtr ret) {
            //bool succ = true;
            if (uo.SubExpression.ReturnType.IsPrimitive()) {
                if (!uo.SubExpression.ReturnType.IsString()) {
                    if (uo.Operator.IsIncDec()) {
                        return TryIncDecCodeGen(uo, out ret);
                    }
                    else {
                        if (!TryExpressionCodeGen(uo.SubExpression, out var par)) {
                            ret = IntPtr.Zero;
                            return false;
                        }
                        switch (uo.Operator) {
                            case UnOp.OperatorKind.NEG:
                                ret = ctx.Negate(par, (sbyte) '-', uo.SubExpression.ReturnType.IsUnsignedNumericType(), irb);
                                return true;
                            case UnOp.OperatorKind.NOT:
                                ret = ctx.Negate(par, (sbyte) '~', uo.SubExpression.ReturnType.IsUnsignedNumericType(), irb);
                                return true;
                            case UnOp.OperatorKind.LNOT:
                                ret = ctx.Negate(par, (sbyte) '!', uo.SubExpression.ReturnType.IsUnsignedNumericType(), irb);
                                return true;
                            case UnOp.OperatorKind.BOOLCAST:
                                ret = ctx.IsNotNull(par, irb);
                                return true;
                        }

                    }
                }
                else {
                    //TODO string unop-overloads
                }
            }
            else if (uo.SubExpression.ReturnType.IsAwaitable() && uo.Operator == UnOp.OperatorKind.AWAIT) {
                if (!TryExpressionCodeGen(uo.SubExpression, out var par)) {
                    ret = IntPtr.Zero;
                    return false;
                }
                return TryAwaitCodeGen(uo, par, out ret);
            }
            else {
                //TODO UnOp-overloads
            }
            throw new NotImplementedException();
        }

        private bool TryAwaitCodeGen(UnOp uo, IntPtr par, out IntPtr ret) {
            bool isValueAwait = !uo.ReturnType.IsVoid();
            var t = par;
            var awaiter = ctx.LoadFieldConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, 1u }, irb);// this-task
            var awaitMet = GetOrCreateInternalFunction(ctx, InternalFunction.taskAwaiterEnqueue);
            var enqueued = ctx.GetCall(awaitMet, new[] { t, awaiter }, irb);

            var bDoSuspend = new BasicBlock(ctx, "doSuspend", fn);
            var bResumePoint = coro.StateTargets[coro.CurrentState + 1];
            if (isValueAwait) {
                var saveGEP = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, 2u }, irb);
                ctx.Store(saveGEP, par, irb);
            }
            ctx.ConditionalBranch(bDoSuspend, bResumePoint, enqueued, irb);

            // suspend
            ctx.ResetInsertPoint(bDoSuspend, irb);
            coro.SetCurrentState(coro.CurrentState + 1);
            var stateGEP = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.StateIndex }, irb);
            ctx.Store(stateGEP, ctx.GetInt32((uint) coro.CurrentState), irb);
            ctx.ReturnValue(ctx.False(), irb);

            // resume
            ctx.ResetInsertPoint(bResumePoint, irb);
            if (isValueAwait) {
                var saveGEP = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, 2u }, irb);
                par = ctx.Load(saveGEP, irb);
                var getTaskValue = GetOrCreateInternalFunction(ctx, InternalFunction.getTaskValue);
                ret = gen.GetValueFromVoidPtr(ctx.GetCall(getTaskValue, new[] { par }, irb), uo.ReturnType, irb);
                ctx.StoreFieldConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.OtherSaveIndex[uo] }, ret, irb);
            }
            else {
                ret = ctx.GetNullPtr();
            }
            return true;
        }

        private bool TryIncDecCodeGen(UnOp uo, out IntPtr ret) {
            if (!TryGetMemoryLocation(uo.SubExpression, out var par)) {
                ret = IntPtr.Zero;
                return false;
            }
            bool isPre = uo.Operator == UnOp.OperatorKind.INCR_PRE || uo.Operator == UnOp.OperatorKind.DECR_PRE;
            bool isInc = uo.Operator == UnOp.OperatorKind.INCR_PRE || uo.Operator == UnOp.OperatorKind.INCR_POST;
            var old = ctx.Load(par, irb);
            if (isInc) {
                var nw = ctx.ArithmeticBinOp(old, ctx.GetInt8(1), (sbyte) '+', uo.SubExpression.ReturnType is PrimitiveType prim && prim.IsUnsignedInteger, irb);
                ctx.Store(par, nw, irb);
                ret = isPre ? nw : old;
            }
            else {
                var nw = ctx.ArithmeticBinOp(old, ctx.GetInt8(1), (sbyte) '-', uo.SubExpression.ReturnType is PrimitiveType prim && prim.IsUnsignedInteger, irb);
                ctx.Store(par, nw, irb);
                ret = isPre ? nw : old;
            }
            return true;
        }

        protected void TryNumericArithmeticOrBitwiseBinOpCodeGen(IntPtr lhs, IntPtr rhs, BinOp.OperatorKind boOperator, bool isUnsigned, out IntPtr ret) {
            if (boOperator.IsBitwise()) {
                // and, or, xor
                sbyte op = 0;
                switch (boOperator) {
                    case BinOp.OperatorKind.AND:
                        op = (sbyte) '&';
                        break;
                    case BinOp.OperatorKind.OR:
                        op = (sbyte) '|';
                        break;
                    case BinOp.OperatorKind.XOR:
                        op = (sbyte) '^';
                        break;
                }
                ret = ctx.BitwiseBinop(lhs, rhs, op, isUnsigned, irb);
            }
            else if (boOperator.IsShift()) {
                //bool isUnsigned = bo.Left.ReturnType.IsUnsignedNumericType() || boOperator == BinOp.OperatorKind.URSHIFT;
                bool left = boOperator == BinOp.OperatorKind.LSHIFT;
                ret = ctx.ShiftOp(lhs, rhs, left, isUnsigned, irb);
            }
            else {
                // +, -, *, /, %
                sbyte op;
                switch (boOperator) {
                    case BinOp.OperatorKind.ADD:
                        op = (sbyte) '+';
                        break;
                    case BinOp.OperatorKind.SUB:
                        op = (sbyte) '-';
                        break;
                    case BinOp.OperatorKind.MUL:
                        op = (sbyte) '*';
                        break;
                    case BinOp.OperatorKind.DIV:
                        op = (sbyte) '/';
                        break;
                    case BinOp.OperatorKind.REM:
                        op = (sbyte) '%';
                        break;
                    default:
                        throw new NotImplementedException();
                }
                ret = ctx.ArithmeticBinOp(lhs, rhs, op, isUnsigned, irb);
            }
        }
        private bool TryBinOpCodeGen(BinOp bo, out IntPtr ret) {
            bool succ = true;
            if (bo.Operator.IsAssignment()) {
                return TryAssignmentCodeGen(bo, out ret);
            }
            else if (bo.Operator.IsBooleanOperator()) {
                return TryLogicalChainCodeGen(bo, out ret);
            }
            else if (bo.Operator.IsComparison()) {
                return TryComparisonCodeGen(bo, out ret);
            }
            else if (bo.ReturnType.IsNumericType()) {
                succ &= TryExpressionCodeGen(bo.Left, out var lhs);
                succ &= TryExpressionCodeGen(bo.Right, out var rhs);
                RestoreSavedValue(bo.Left, ref lhs);
                RestoreSavedValue(bo.Right, ref rhs);
                if (!succ) {
                    ret = IntPtr.Zero;
                    return false;
                }
                TryNumericArithmeticOrBitwiseBinOpCodeGen(lhs, rhs, bo.Operator, (bo.ReturnType as PrimitiveType).IsUnsignedInteger, out ret);
                return succ;
            }
            else if (bo.ReturnType.IsPrimitive(PrimitiveName.String)) {
                return TryStringBinopCodeGen(bo, out ret);
            }
            else {
                //TODO operator overload
                //TODO bitwise operators on booleans
            }
            throw new NotImplementedException();
        }

        private bool TryArrayIndexerCodeGen(IndexerExpression arrInd, out IntPtr ret) {
            if (arrInd.Indices.Length > 1) {
                if (arrInd.Indices.Length == 2) {
                    return TryArrayIndexerSetCodeGen(arrInd, out ret);
                }
                // kann nicht 0 sein, sonst wäre der Konstruktor gescheitert
                "An array cannot be indexed with multiple indices".Report(arrInd.Position, false);
            }
            /*if (!TryExpressionCodeGen(arrInd.Indices[0], out var index)) {
                ret = IntPtr.Zero;
                return false;
            }
            if (arrInd.ParentExpression.ReturnType.IsArray()) {
                if (!TryRefExpressionCodeGen(arrInd.ParentExpression, out var array)) {
                    ret = IntPtr.Zero;
                    return false;
                }
                ret = ctx.LoadField(array, new[] { ctx.GetInt32(0), ctx.GetInt32(1), index }, irb);
                return true;
            }
            else if (arrInd.ParentExpression.ReturnType.IsArraySlice()) {
                if (!TryExpressionCodeGen(arrInd.ParentExpression, out var span)) {
                    ret = IntPtr.Zero;
                    return false;
                }
                var arr = ctx.ExtractValue(span, 0, irb);
                ret = ctx.LoadField(arr, new[] { index }, irb);
                return true;
            }
            else {
                //TODO Span indexer
                ret = IntPtr.Zero;
                return $"The {arrInd.ParentExpression.GetType().Name} cannot be indexed".Report(arrInd.Position, false);
            }*/
            if (TryGetIndexerExpressionMemoryLocation(arrInd, out var mem)) {
                ret = ctx.Load(mem, irb);
                return true;
            }
            ret = ctx.GetNullPtr();
            return false;
        }
        private bool TryArrayIndexerSetCodeGen(IndexerExpression arrInd, out IntPtr nwElem) {
            if (!TryGetIndexerExpressionMemoryLocation(arrInd, out var arrElemPtr) | !TryExpressionCodeGen(arrInd.Indices[1], out nwElem)) {
                return false;
            }
            ctx.Store(arrElemPtr, nwElem, irb);
            return true;
        }
        private void ThrowIfGreaterEqual(IntPtr index, IntPtr length, bool is64bit) {
            if (!DoBoundsChecks)
                return;
            /*var cond = ctx.CompareOp(index, length, (sbyte)'<', false, true, irb);
            BasicBlock
                bThrow = new BasicBlock(ctx, "indexOutOfBounds", fn),
                bNotThrow = new BasicBlock(ctx, "indexInBounds", fn);

            ctx.ConditionalBranch(bNotThrow, bThrow, cond, irb);
            ctx.ResetInsertPoint(bThrow, irb);

            var throwFn = GetOrCreateInternalFunction(ctx, is64bit ? InternalFunction.throwOutOfBounds64 : InternalFunction.throwOutOfBounds);
            ctx.GetCall(throwFn, new[] { index, length }, irb);
            ctx.Branch(bNotThrow, irb);

            ctx.ResetInsertPoint(bNotThrow, irb);*/
            var throwFn = GetOrCreateInternalFunction(ctx, is64bit ? InternalFunction.throwIfOutOfBounds64 : InternalFunction.throwIfOutOfBounds);
            ctx.GetCall(throwFn, new[] { index, length }, irb);
        }
        private void ThrowOutOfBounds(IntPtr index, IntPtr length, bool is64Bit = true) {
            if (!DoBoundsChecks)
                return;
            var throwFn = GetOrCreateInternalFunction(ctx, is64Bit ? InternalFunction.throwOutOfBounds64 : InternalFunction.throwOutOfBounds);
            ctx.GetCall(throwFn, new[] { index, length }, irb);
            ctx.Unreachable(irb);
        }
        private void ThrowIfNullOrGreaterEqual(IntPtr index, IntPtr lengthPtr, bool is64Bit) {
            if (!DoBoundsChecks)
                return;
            ThrowIfNull(lengthPtr);
            ThrowIfGreaterEqual(index, ctx.Load(lengthPtr, irb), is64Bit);

            //var throwFn = GetOrCreateInternalFunction(ctx, is64Bit ? InternalFunction.throwIfNullOrOutOfBounds64 : InternalFunction.throwIfNullOrOutOfBounds);
            //ctx.GetCall(throwFn, new[] { index, lengthPtr }, irb);
        }
        private void ThrowIfNull(IntPtr ptr) {
            if (!DoNullChecks)
                return;
            /*var cond = ctx.IsNotNull(ptr, irb);
            BasicBlock
                bThrow = new BasicBlock(ctx, "nullPointer", fn),
                bNotThrow = new BasicBlock(ctx, "nonNullPointer", fn);
            ctx.ConditionalBranch(bNotThrow, bThrow, cond, irb);
            ctx.ResetInsertPoint(bThrow, irb);
            var throwFn = GetOrCreateInternalFunction(ctx, InternalFunction.throwNullDereference);
            ctx.GetCall(throwFn, new[] { ctx.GetNullPtr(), ctx.GetInt32(0) }, irb);
            ctx.Branch(bNotThrow, irb);

            ctx.ResetInsertPoint(bNotThrow, irb);*/
            var throwFn = GetOrCreateInternalFunction(ctx, InternalFunction.throwIfNull);
            ctx.GetCall(throwFn, new[] { ptr, ctx.GetNullPtr(), ctx.GetInt32(0) }, irb);
        }
        private void ThrowException(string message) {
            var throwFn = GetOrCreateInternalFunction(ctx, InternalFunction.throwException);
            var msg = ctx.GetString(message, irb);
            var msgVal = ctx.ExtractValue(msg, 0, irb);
            var msgLen = ctx.ExtractValue(msg, 1, irb);
            ctx.GetCall(throwFn, new[] { msgVal, msgLen }, irb);
            ctx.Unreachable(irb);
        }
    }
}
