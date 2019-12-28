/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Compiler;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using System;
using System.Collections.Generic;
using System.Text;

using Module = CompilerInfrastructure.Module;
using Type = CompilerInfrastructure.Structure.Types.Type;
using CompilerInfrastructure.Utils;
using System.Linq;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Expressions;
using static NativeManagedContext;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Semantics;
using static LLVMCodeGenerator.InstructionGenerator;

namespace LLVMCodeGenerator {
    public partial class LLVMCodeGenerator : AbstractCodeGenerator, IDisposable {
        public enum OptLevel : byte {
            None,
            O1,
            O2,
            O3
        }

        internal readonly Dictionary<IType, IntPtr> types = new Dictionary<IType, IntPtr>();
        internal readonly Dictionary<IType, IntPtr> structTypes = new Dictionary<IType, IntPtr>();
        internal readonly Dictionary<IMethod, IntPtr> methods = new Dictionary<IMethod, IntPtr>();
        //internal readonly Dictionary<IType, IntPtr> isInstanceOf = new Dictionary<IType, IntPtr>();
        internal readonly Dictionary<IType, ulong> typeId = new Dictionary<IType, ulong>();

        ICollection<IMethod> allMethods = new List<IMethod>();
        internal readonly HashSet<Type.Signature> incomplete = new HashSet<Type.Signature>();
        internal readonly MultiMap<IType, IMethod> virtualMethods = new MultiMap<IType, IMethod>();
        protected internal readonly Dictionary<IMethod, uint> virtualMethodSlot = new Dictionary<IMethod, uint>();
        protected internal readonly Dictionary<IVariable, uint> instanceFields = new Dictionary<IVariable, uint>();
        protected internal readonly Dictionary<IVariable, IntPtr> staticVariables = new Dictionary<IVariable, IntPtr>();
        protected internal readonly Dictionary<(IType, IType), uint> interfaceVTableSlots = new Dictionary<(IType, IType), uint>();
        protected internal readonly Dictionary<IType, VirtualMethodTable> vtables = new Dictionary<IType, VirtualMethodTable>();
        readonly Dictionary<IType, uint> superIndex = new Dictionary<IType, uint>();
        protected internal readonly Dictionary<IType, uint> actorQueueIndex = new Dictionary<IType, uint>();
        IntPtr interfaceType = IntPtr.Zero;
        protected internal readonly OptLevel optLvl;
        protected internal readonly byte maxOptIterations;
        protected internal readonly ISemantics semantics;

        protected internal ManagedContext ctx;
        readonly BuiltinHashMap bhm;

        public string OutputFilename {
            get;
        }
        public bool IsLibrary {
            get;
        }

        public LLVMCodeGenerator(string outputFilename, OptLevel lvl = OptLevel.O1, byte _maxOptIterations = 4, bool isLibrary = false, ISemantics sem = null)
            : base(TemplateBehavior.None, TemplateBehavior.None, false, false, new DefaultNameMangler()) {
            if (string.IsNullOrWhiteSpace(outputFilename))
                throw new ArgumentException("The output-filename must not be empty", nameof(outputFilename));

            OutputFilename = outputFilename;
            optLvl = lvl;
            maxOptIterations = _maxOptIterations;
            IsLibrary = isLibrary;
            semantics = sem ?? new BasicSemantics();
            bhm = new BuiltinHashMap(this);
        }
        internal protected static IEnumerable<IType> GetAllSuperTypes(IType tp) {
            if (tp is null)
                yield break;
            if (tp is IHierarchialType htp) {
                while (htp.SuperType != null) {
                    yield return htp.SuperType;
                    htp = htp.SuperType;
                }
            }
        }
        public virtual bool TryGetVirtualMethodSlot(IMethod met, out uint slot, IntPtr irb) {
            if (virtualMethodSlot.TryGetValue(met, out slot))
                return true;
            if (met.Signature.BaseMethodTemplate != null) {
                var bb = ctx.GetCurrentBasicBlock(irb);
                ImplementMethod(met);
                ctx.ResetInsertPoint(bb, irb);
                return virtualMethodSlot.TryGetValue(met, out slot);
            }
            else if (met.NestedIn is ITypeContext tcx && tcx.Type != null && tcx.Type.Signature.BaseGenericType != null) {
                var bb = ctx.GetCurrentBasicBlock(irb);
                ImplementType(tcx.Type);
                ctx.ResetInsertPoint(bb, irb);
                return virtualMethodSlot.TryGetValue(met, out slot);
            }
            return false;
        }
        protected internal virtual uint GetVirtualMethodSlot(IMethod met, IntPtr irb) {
            if (!TryGetVirtualMethodSlot(met, out var slot, irb))
                throw new KeyNotFoundException();
            return slot;
        }
        protected internal virtual void SetVirtualMethodSlot(IMethod met, uint slot) {
            virtualMethodSlot[met] = slot;
        }
        public virtual bool TryGetVTable(IType ty, out VirtualMethodTable vt, IntPtr irb) {
            if (vtables.TryGetValue(ty, out vt))
                return true;

            var bb = ctx.GetCurrentBasicBlock(irb);
            ImplementType(ty);
            ctx.ResetInsertPoint(bb, irb);
            return vtables.TryGetValue(ty, out vt);

        }
        protected internal virtual VirtualMethodTable GetVTable(IType ty, IntPtr irb) {
            if (!TryGetVTable(ty, out var vt, irb))
                throw new KeyNotFoundException();
            return vt;
        }
        protected internal virtual void SetVTable(IType ty, VirtualMethodTable vt) {
            vtables[ty] = vt;
        }
        public override void InitCodeGen(Module mod) {
            ctx = new ManagedContext(mod.ModuleName, mod.Filename);
        }
        public override void FinalizeCodeGen() {
            if (ctx.VerifyModule()) {
                if (optLvl != OptLevel.None)
                    ctx.Optimize((byte) optLvl, maxOptIterations);
                if (!IsLibrary)
                    ctx.LinkTimeOptimization();
                if (ctx.VerifyModule()) {
                    ctx.Save(OutputFilename);
                    return;
                }
                //ctx.DumpModule(OutputFilename + ".ll");
            }

            $"Invalid LLVM IR -- see dump in {OutputFilename}.ll".Report();
            ctx.DumpModule(OutputFilename + ".ll");

        }
        protected override bool DoCodeGen(Module mod) {
            var ret = base.DoCodeGen(mod);
            IType[] actualTypes;
            do {
                actualTypes = types.Keys.ToArray();
                foreach (var tp in actualTypes.OfType<ClassType>()) {
                    ret &= ImplementType(tp);
                }
            } while (actualTypes.Length < types.Count);
            ICollection<IMethod> mets;
            do {
                mets = allMethods;
                allMethods = new List<IMethod>();
                foreach (var met in mets.ToArray()) {
                    ret &= ImplementMethod(met);
                }
            } while (allMethods.Any());
            return ret;
        }

        protected override bool DeclareFieldImpl(IVariable fld) {
            return true;
        }
        protected override bool DeclareMethodImpl(IMethod met) {
            if (met.IsInternal())
                return true;
            return TryGetMethod(met, out _, default);
        }

        protected override bool DeclareMethodTemplateImpl(IMethodTemplate<IMethod> met) {
            return true;
        }

        protected override bool DeclareTypeImpl(IType ty) {
            return TryGetType(ty, out _);
        }
        protected override bool ImplementTypeImpl(IType ty) {
            if (ty.IsImport())
                return true;
            if (ty.IsBuiltin())
                return ImplementBuiltinType(ty);
            bool succ = true;
            if (virtualMethods.TryGetValue(ty, out ISet<IMethod> virtMets) || ty.CanBeInherited()) {
                if (virtMets is null) {
                    virtMets = Set.Empty<IMethod>();
                }
                Vector<IntPtr> vtableVal = default;
                Vector<IntPtr> vtableTys = default;
                VirtualMethodTable superVTable = default;
                uint level = 1;
                {

                    if (ty is IHierarchialType htp && htp.SuperType != null) {
                        succ &= ImplementType(htp.SuperType);
                        vtableVal = new Vector<IntPtr>((superVTable = GetVTable(htp.SuperType, IntPtr.Zero)).VirtualMethods);
                        vtableTys = new Vector<IntPtr>(superVTable.VirtualMethodTypes);
                        level = 1 + superVTable.Level;
                    }
                }
                foreach (var met in virtMets) {
                    if (met.IsOverride()) {
                        if (met.OverrideTarget is null)
                            succ = $"The overriding method {met.Signature} must specify a method to override".Report(met.Position, false);
                        else if (met.OverrideTarget.NestedIn is ITypeContext tcx) {
                            if (!ty.IsSubTypeOf(tcx.Type)) {
                                succ = $"The method {met.Signature} cannot override the method {met.OverrideTarget}, since it is not defined in a superclass".Report(met.Position, false);
                            }
                            else if (ty == tcx.Type) {
                                succ = $"The method {met.Signature} cannot override the method {met.OverrideTarget} from the same class".Report(met.Position, false);
                            }
                        }
                        else {
                            succ = $"The method {met.Signature} cannot override a global method".Report(met.Position, false);
                        }
                        if (!(met.OverrideTarget is IMethod ovMet)) {
                            succ = $"The method {met.Signature} cannot override {met.OverrideTarget}, since it is only a method-template".Report(met.Position, false);
                        }
                        else {
                            succ &= TryGetMethod(met, out var llMet, met.Position);
                            uint slot = GetVirtualMethodSlot(ovMet, IntPtr.Zero);
                            vtableVal[slot] = llMet;
                            succ &= TryGetFunctionPtrType(met, out vtableTys[slot]);
                            //virtualMethodSlot[met] = slot;
                            SetVirtualMethodSlot(met, slot);
                        }
                    }
                    else {
                        succ &= TryGetMethod(met, out var llMet, met.Position);
                        //virtualMethodSlot[met] = vtableVal.Length;
                        SetVirtualMethodSlot(met, vtableVal.Length);
                        vtableVal.Add(llMet);
                        succ &= TryGetFunctionPtrType(met, out var fnPtrTy);
                        vtableTys.Add(fnPtrTy);
                    }
                }
                //var names = GetAllSuperTypes(ty).Select(x => x.FullName()).Prepend(ty.FullName());
                IntPtr superVT;
                {
                    // superVT is available here, since ImplementTypeimpl is called from this method recursively
                    superVT = ty is IHierarchialType htp && htp.SuperType != null ? GetVTable(htp.SuperType, IntPtr.Zero).VTablePointer : IntPtr.Zero;
                }

                //typeID:
                uint typeididx;
                uint isinstidx;
                if (superVTable.IsVTable) {
                    typeididx = superVTable.TypeIdIndex;
                    isinstidx = superVTable.IsInstIndex;
                }
                else {
                    typeididx = vtableVal.Length;
                    vtableTys.Add(ctx.GetLongType());
                    isinstidx = vtableVal.Length + 1;
                    vtableTys.Add(ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr() }));
                }
                vtableVal[typeididx] = ctx.GetInt64(GetTypeId(ty));
                vtableVal[isinstidx] = DeclareIsInstanceOf(ty);

                IntPtr vtableTy = ctx.GetVTableType(ty.FullName(), vtableTys.AsArray());
                if (!ty.IsInterface()) {

                    var vtablePtr = //new VTable(ctx, ty.FullName(), vtableTy, vtableVal.AsArray());
                        ctx.GetVTable(ty.FullName(), vtableTy, vtableVal.AsArray(), superVT);
                    //vtables[ty] = new VirtualMethodTable(vtableTy, vtablePtr, vtableVal, vtableTys, level);
                    SetVTable(ty, new VirtualMethodTable(vtableTy, vtablePtr, vtableVal, vtableTys, level, typeididx, isinstidx));
                }
                else {
                    //vtables[ty] = new VirtualMethodTable(vtableTy, IntPtr.Zero, vtableVal, vtableTys, level);
                    SetVTable(ty, new VirtualMethodTable(vtableTy, ctx.GetNullPtr(), vtableVal, vtableTys, level, typeididx, isinstidx));
                }
                CreateIsInstanceOf(ty, vtableVal[isinstidx]);
            }
            succ &= ImplementMethods(ty.Context);
            return succ & base.ImplementTypeImpl(ty);
        }

        private bool ImplementBuiltinType(IType tp) {
            if (tp.Signature.Name == "::HashMap") {
                var keyTy = (IType)tp.Signature.GenericActualArguments.First();
                var valTy = (IType)tp.Signature.GenericActualArguments.ElementAt(1);
                return bhm.ImplementBuiltinHashMap(tp, keyTy, valTy);
            }
            // already reported, that this is an invalid builtin type
            return false;
        }



        protected override bool DeclareTypeTemplateImpl(ITypeTemplate<IType> ty) {
            return true;
        }
        protected virtual InstructionGenerator CreateInstructionGenerator(LLVMCodeGenerator gen, ManagedContext ctx, IntPtr function, FunctionType methodTp, IMethod met) {
            return new InstructionGenerator(gen, ctx, function, methodTp);
        }
        protected virtual void CollectCoroSaveSlots(IStatement stmt, ref Vector<IntPtr> llFlds, ref bool hasError, IDictionary<IVariable, uint> localIndices, IDictionary<IExpression, uint> otherIndices) {
            if (stmt is Declaration decl) {
                foreach (var vr in decl.Variables) {
                    localIndices[vr] = llFlds.Length;
                    hasError |= !TryGetVariableType(vr, out var vrTp);
                    llFlds.Add(vrTp);
                }
            }
            else if (stmt is ForeachLoop fel) {
                otherIndices[fel.Range] = llFlds.Length;
                hasError |= !TryGetType(fel.Range.ReturnType, out var rangeTy);
                llFlds.Add(rangeTy);
                if (fel.Range.ReturnType.IsArray() || fel.Range.ReturnType.IsArraySlice())
                    // reserve space for loop-variable
                    llFlds.Add(ctx.GetSizeTType());
                if (fel.Range.ReturnType.IsVarArg()) {
                    // outer loop variable
                    llFlds.Add(ctx.GetIntType());
                    // inner loop variable
                    llFlds.Add(ctx.GetSizeTType());
                }
            }
        }
        protected virtual void CollectCoroSaveSlots(IExpression expr, Vector<IntPtr> llFlds, ref bool hasError, IDictionary<IVariable, uint> localIndices, IDictionary<IExpression, uint> otherIndices) {

        }

        public uint CountSuspendPoints(IStatement stmt, IDictionary<IVariable, uint> localIndices, IDictionary<IExpression, uint> otherIndices, ref IType iteratorElementTy, ref bool hasError, ref Vector<IntPtr> llFlds) {
            if (stmt is YieldStatement yst) {
                var oldElemTy = iteratorElementTy;
                iteratorElementTy = Type.MostSpecialCommonSuperType(iteratorElementTy, yst.HasReturnValue ? yst.ReturnValue.ReturnType : PrimitiveType.Void);
                if (!hasError && iteratorElementTy.IsError()) {
                    hasError = true;
                    $"The yield-type {(yst.HasReturnValue ? yst.ReturnValue.ReturnType : PrimitiveType.Void)} is inconsistent with the expected type {oldElemTy}".Report(yst.Position);
                }
                return 1;
            }
            else {
                CollectCoroSaveSlots(stmt, ref llFlds, ref hasError, localIndices, otherIndices);
                uint ret = 0;
                foreach (var ex in stmt.GetExpressionsRecursively()) {
                    if (ex is UnOp uo && uo.Operator == UnOp.OperatorKind.AWAIT) {
                        var oldElemTy = iteratorElementTy;
                        iteratorElementTy = Type.MostSpecialCommonSuperType(iteratorElementTy, ex.ReturnType);
                        if (!hasError && iteratorElementTy.IsError()) {
                            hasError = true;
                            $"The await-type {ex.ReturnType} is inconsistent with the expected type {oldElemTy}".Report(ex.Position);
                        }
                        ret++;
                        if (!uo.ReturnType.IsVoid()) {
                            hasError |= !TryGetType(uo.ReturnType, out var awaitTy);
                            otherIndices[uo] = llFlds.Length;
                            llFlds.Add(awaitTy);
                        }

                    }
                    CollectCoroSaveSlots(ex, llFlds, ref hasError, localIndices, otherIndices);
                }
                foreach (var st in stmt.GetStatements()) {
                    ret += CountSuspendPoints(st, localIndices, otherIndices, ref iteratorElementTy, ref hasError, ref llFlds);
                }
                return ret;
            }
        }

        public virtual IntPtr SetupCoro(IMethod coroutine, IntPtr function, IntPtr irb, ISemantics sem, out uint numSuspendPoints, out FunctionType coroTp, out CoroutineInfo.Kind kind, out IDictionary<IVariable, uint> localIdx, out uint thisInd, out uint stateInd, out IDictionary<IExpression, uint> otherIdx, IType retElemTy = null) {

            var localIndices = localIdx = new Dictionary<IVariable, uint>();
            var otherIndices = otherIdx = new Dictionary<IExpression, uint>();
            thisInd = coroutine.IsStatic() ? uint.MaxValue : 1;
            stateInd = 0;
            var iteratorElementTy = retElemTy is null || retElemTy.IsError() ? Type.Top : retElemTy;


            if (iteratorElementTy.IsError())
                kind = default;
            else if (coroutine.IsAsync())
                kind = CoroutineInfo.Kind.Async;
            else if (sem.IsIterable(coroutine.ReturnType, iteratorElementTy))
                kind = CoroutineInfo.Kind.Iterable;
            else if (sem.IsIterator(coroutine.ReturnType, iteratorElementTy))
                kind = CoroutineInfo.Kind.Iterator;
            else
                kind = $"The coroutine {coroutine} has an invalid return-type. It must be an iterator or iterable over {iteratorElementTy} or an asynchronous task".Report(coroutine.Position, default(CoroutineInfo.Kind));

            var llFlds = new Vector<IntPtr> {
                ctx.GetIntType()
            };
            if (kind == CoroutineInfo.Kind.Async) {
                // include the this-task in the frame
                llFlds.Add(ctx.GetVoidPtr());
                // save the awaited task to retrieve the result
                llFlds.Add(ctx.GetVoidPtr());
            }
            var iterableFlds = Vector<IntPtr>.Reserve((coroutine.IsStatic() ? 0u : 1u) + (uint) coroutine.Arguments.Length);
            if (!coroutine.IsStatic()) {
                if (!TryGetType((coroutine.NestedIn as ITypeContext).Type, out var thisTy)) {
                    thisTy = $"The non-static method {coroutine} must be located inside of a class".Report(coroutine.Position, ctx.GetVoidPtr());
                }
                thisInd = llFlds.Length;
                llFlds.Add(thisTy);
                iterableFlds.Add(thisTy);
            }
            foreach (var arg in coroutine.Arguments) {
                if (arg.Type.IsByRef()) {
                    "A coroutine must not have a byRef-parameter".Report(arg.Position);
                }
                TryGetVariableType(arg, out var argTp);
                localIndices[arg] = llFlds.Length;
                llFlds.Add(argTp);
                iterableFlds.Add(argTp);
            }


            bool hasError = false;
            uint countSuspendPoints(IStatement stmt) {
                /* if (stmt is YieldStatement yst) {
                     var oldElemTy = iteratorElementTy;
                     iteratorElementTy = Type.MostSpecialCommonSuperType(iteratorElementTy, yst.HasReturnValue ? yst.ReturnValue.ReturnType : PrimitiveType.Void);
                     if (!hasError && iteratorElementTy.IsError()) {
                         hasError = true;
                         $"The yield-type {(yst.HasReturnValue ? yst.ReturnValue.ReturnType : PrimitiveType.Void)} is inconsistent with the expected type {oldElemTy}".Report(yst.Position);
                     }
                     return 1;
                 }
                 else {
                     CollectCoroSaveSlots(stmt, ref llFlds, ref hasError, localIndices, otherIndices);
                     uint ret = 0;
                     foreach (var ex in stmt.GetExpressionsRecursively()) {
                         if (ex is UnOp uo && uo.Operator == UnOp.OperatorKind.AWAIT) {
                             var oldElemTy = iteratorElementTy;
                             iteratorElementTy = Type.MostSpecialCommonSuperType(iteratorElementTy, ex.ReturnType);
                             if (!hasError && iteratorElementTy.IsError()) {
                                 hasError = true;
                                 $"The await-type {ex.ReturnType} is inconsistent with the expected type {oldElemTy}".Report(ex.Position);
                             }
                             ret++;
                         }
                         CollectCoroSaveSlots(ex, llFlds, ref hasError, localIndices, otherIndices);
                     }
                     foreach (var st in stmt.GetStatements()) {
                         ret += countSuspendPoints(st);
                     }
                     return ret;
                 }*/
                return CountSuspendPoints(stmt, localIndices, otherIndices, ref iteratorElementTy, ref hasError, ref llFlds);
            }
            // assume coroutine is not abstract
            numSuspendPoints = countSuspendPoints(coroutine.Body.Instruction);

            //var zero = ctx.GetInt32(0);


            var frameTy = ctx.GetStruct(coroutine.FullName() + ":coroutineFrameTy");
            ctx.CompleteStruct(frameTy, llFlds.AsArray());
            var iterableTy = ctx.GetStruct(coroutine.FullName() + ":iterableTy");
            ctx.CompleteStruct(iterableTy, iterableFlds.AsArray());
            var malloc = GetOrCreateInternalFunction(ctx, "gc_new");

            IntPtr getIteratorFn, tryGetNextFn;
            if (kind != CoroutineInfo.Kind.Async) {

                #region Iterable-method
                if (kind == CoroutineInfo.Kind.Iterable) {

                    {
                        //already in entry BB
                        var sz = ctx.GetI32SizeOf(iterableTy);
                        var iterableBase_raw = ctx.GetCall(malloc, new[] { sz }, irb);
                        var iterableBase = ctx.ForceCast(iterableBase_raw, ctx.GetPointerType(iterableTy), false, irb);
                        uint index = 0;
                        if (!coroutine.IsStatic()) {
                            var thisGEP = ctx.GetElementPtrConstIdx(iterableBase, new[] { 0u, index }, irb);
                            ctx.Store(thisGEP, ctx.GetArgument(function, index), irb);

                            index++;
                        }
                        foreach (var arg in coroutine.Arguments) {
                            var argGEP = ctx.GetElementPtrConstIdx(iterableBase, new[] { 0u, index }, irb);
                            ctx.Store(argGEP, ctx.GetArgument(function, index), irb);
                            index++;
                        }

                        //DOLATER get interface of right type
                        getIteratorFn = ctx.DeclareFunction(coroutine.FullName() + ":iterable.getIterator", GetInterfaceType(Type.Top), new[] { ctx.GetVoidPtr() }, new[] { "iterableBase_raw" }, false);

                        var iterableVTableTy = ctx.GetVTableType(coroutine.FullName() + ":iterable.vtableTy", new[] { ctx.GetFunctionPtrTypeFromFunction(getIteratorFn) });
                        var iterableVTable = ctx.GetVTable(coroutine.FullName() + ":iterable.vtable", iterableVTableTy, new[] { getIteratorFn }, default);

                        var retAlloca = ctx.DefineAlloca(function, GetInterfaceType(Type.Top), "ret");
                        var baseGEP = ctx.GetElementPtrConstIdx(retAlloca, new[] { 0u, 0u }, irb);
                        ctx.Store(baseGEP, iterableBase_raw, irb);

                        var vtableGEP = ctx.GetElementPtrConstIdx(retAlloca, new[] { 0u, 1u }, irb);
                        var iterableVTable_raw = ctx.ForceCast(iterableVTable, ctx.GetVoidPtr(), false, irb);
                        ctx.Store(vtableGEP, iterableVTable_raw, irb);

                        var ret = ctx.Load(retAlloca, irb);
                        ctx.ReturnValue(ret, irb);
                    }

                }
                else {
                    getIteratorFn = function;
                }
                #endregion
                #region getIterator-method
                {
                    if (kind == CoroutineInfo.Kind.Iterable) {
                        var entry = new BasicBlock(ctx, "entry", getIteratorFn);
                        ctx.ResetInsertPoint(entry, irb);
                    }// else already in entry

                    var sz = ctx.GetI32SizeOf(frameTy);
                    var mem = ctx.GetCall(malloc, new[] { sz }, irb);
                    var fram = ctx.ForceCast(mem, ctx.GetPointerType(frameTy), false, irb);
                    var iterableBase_raw = ctx.GetArgument(getIteratorFn, 0);
                    var iterableBase = ctx.ForceCast(iterableBase_raw, ctx.GetPointerType(iterableTy), false, irb);
                    uint frameIndex = 0;
                    {
                        // start with state 0
                        var stateGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                        ctx.Store(stateGEP, ctx.GetInt32(0), irb);
                        frameIndex++;
                    }
                    uint index = 0;
                    if (!coroutine.IsStatic()) {
                        var framThisGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                        IntPtr thisPtr;
                        if (kind == CoroutineInfo.Kind.Iterable) {
                            var thisGEP = ctx.GetElementPtrConstIdx(iterableBase, new[] { 0u, index }, irb);
                            thisPtr = ctx.Load(thisGEP, irb);
                        }
                        else
                            thisPtr = ctx.GetArgument(function, 0);
                        ctx.Store(framThisGEP, thisPtr, irb);
                        index++;
                        frameIndex++;
                    }
                    foreach (var arg in coroutine.Arguments) {
                        var framArgGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                        IntPtr argVal;
                        if (kind == CoroutineInfo.Kind.Iterable) {
                            var argGEP = ctx.GetElementPtrConstIdx(iterableBase, new[] { 0u, index }, irb);
                            argVal = ctx.Load(argGEP, irb);
                        }
                        else
                            argVal = ctx.GetArgument(function, index);
                        ctx.Store(framArgGEP, argVal, irb);

                        index++;
                        frameIndex++;
                    }
                    if (hasError || !TryGetType(iteratorElementTy.AsByRef(), out IntPtr retTy)) {
                        retTy = ctx.GetVoidPtr();
                    }
                    coroTp = new FunctionType(coroutine.Position, (coroutine as IDeclaredMethod).Context.Module, coroutine.FullName() + ":iterator.tryGetNext", PrimitiveType.Bool, new[] { iteratorElementTy.AsByRef() }, Visibility.Internal);
                    //DOLATER get interface of right type
                    tryGetNextFn = ctx.DeclareFunction(coroutine.FullName() + ":iterator.tryGetNext", ctx.GetBoolType(), new[] { ctx.GetPointerType(frameTy), retTy }, new[] { "coro_frame", "ret" }, false);

                    var iteratorVTableTy = ctx.GetVTableType(coroutine.FullName() + ":iterator.vtableTy", new[] { ctx.GetFunctionPtrTypeFromFunction(tryGetNextFn) });
                    var iteratorVTable = ctx.GetVTable(coroutine.FullName() + ":iterator.vtable", iteratorVTableTy, new[] { tryGetNextFn }, default);

                    var retAlloca = ctx.DefineAlloca(getIteratorFn, GetInterfaceType(Type.Top), "ret");
                    var baseGEP = ctx.GetElementPtrConstIdx(retAlloca, new[] { 0u, 0u }, irb);
                    ctx.Store(baseGEP, mem, irb);

                    var vtableGEP = ctx.GetElementPtrConstIdx(retAlloca, new[] { 0u, 1u }, irb);
                    var iteratorVTable_raw = ctx.ForceCast(iteratorVTable, ctx.GetVoidPtr(), false, irb);
                    ctx.Store(vtableGEP, iteratorVTable_raw, irb);

                    var ret = ctx.Load(retAlloca, irb);
                    ctx.ReturnValue(ret, irb);
                }
                #endregion

                var coroEntry = new BasicBlock(ctx, "entry", tryGetNextFn);
                ctx.ResetInsertPoint(coroEntry, irb);
                return tryGetNextFn;
            }
            else {
                var retTy = (coroutine.ReturnType as IWrapperType).ItemType;
                var framePtrTy = ctx.GetPointerType(frameTy);
                var args = retTy.IsVoid() ? new[] { framePtrTy } : new[] { framePtrTy, ctx.GetPointerType(ctx.GetVoidPtr()) };
                var argNames = retTy.IsVoid() ? new[] { "coro_frame_raw" } : new[] { "coro_frame_raw", "ret" };
                var coro = ctx.DeclareFunction(":coro:" + coroutine.FullName(), ctx.GetBoolType(), args, argNames, false);
                var managedArgs = retTy.IsVoid() ? Array.Empty<IType>() : new[] { PrimitiveType.Handle.AsByRef() };
                coroTp = new FunctionType(coroutine.Position, (coroutine as IDeclaredMethod).Context.Module, ":coro:" + coroutine.FullName(), PrimitiveType.Bool, managedArgs, Visibility.Internal);


                #region initialize frame
                var sz = ctx.GetI32SizeOf(frameTy);
                var mem = ctx.GetCall(malloc, new[] { sz }, irb);
                var fram = ctx.ForceCast(mem, ctx.GetPointerType(frameTy), false, irb);
                uint frameIndex = 0;
                {
                    // start with state 0
                    var stateGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                    ctx.Store(stateGEP, ctx.GetInt32(0), irb);
                    frameIndex++;

                    //var taskGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                    frameIndex++;

                    var awaitTaskGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                    ctx.Store(awaitTaskGEP, ctx.GetNullPtr(), irb);
                    frameIndex++;
                }
                uint index = 0;
                if (!coroutine.IsStatic()) {
                    var framThisGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                    IntPtr thisPtr;

                    thisPtr = ctx.GetArgument(function, 0);
                    ctx.Store(framThisGEP, thisPtr, irb);
                    index++;
                    frameIndex++;
                }
                foreach (var arg in coroutine.Arguments) {
                    var framArgGEP = ctx.GetElementPtrConstIdx(fram, new[] { 0u, frameIndex }, irb);
                    IntPtr argVal;

                    argVal = ctx.GetArgument(function, index);
                    ctx.Store(framArgGEP, argVal, irb);

                    index++;
                    frameIndex++;
                }
                #endregion

                if (!(coroutine.NestedIn is ITypeContext tcx) || tcx.Type is null || !tcx.Type.IsActor()) {
                    #region run first iteration synchronously
                    IntPtr retTaskCtor = retTy.IsVoid()
                        ? GetOrCreateInternalFunction(ctx, InternalFunction.runImmediatelyTask)
                        : GetOrCreateInternalFunction(ctx, InternalFunction.runImmediatelyTaskT);
                    var retTask = ctx.GetElementPtrConstIdx(fram, new[] { 0u, 1u }, irb);
                    ctx.GetCall(retTaskCtor, new[] { coro, fram, retTask }, irb);
                    ctx.ReturnValue(ctx.Load(retTask, irb), irb);
                    #endregion
                }
                else {
                    #region run everything asynchronously
                    IntPtr retTaskCtor = retTy.IsVoid()
                        ? GetOrCreateInternalFunction(ctx, InternalFunction.runActorTaskAsync)
                        : GetOrCreateInternalFunction(ctx, InternalFunction.runActorTaskTAsync);
                    var retTaskPtr = ctx.GetElementPtrConstIdx(fram, new[] { 0u, 1u }, irb);
                    var actor = ctx.GetArgument(function, 0);
                    var queueIndex = actorQueueIndex[(coroutine.NestedIn as ITypeContext).Type];
                    var queue = ctx.LoadFieldConstIdx(actor, new[] { 0u, queueIndex }, irb);

                    ctx.GetCall(retTaskCtor, new[] { coro, fram, queue, retTaskPtr }, irb);
                    ctx.ReturnValue(ctx.Load(retTaskPtr, irb), irb);
                    #endregion
                }
                var entry = new BasicBlock(ctx, "entry", coro);
                ctx.ResetInsertPoint(entry, irb);
                return coro;
            }
        }
        protected virtual InstructionGenerator CreateCoroInstructionGenerator(LLVMCodeGenerator gen, ManagedContext ctx, IntPtr function, IMethod method, IntPtr irb = default) {
            function = SetupCoro(method, function, irb, new BasicSemantics(), out var numSuspendPoints, out var coroTp, out var kind, out var localIdx, out var thisInd, out var stateInd, out var otherIdx);
            return new InstructionGenerator(gen, ctx, function, coroTp, numSuspendPoints, kind, localIdx, thisInd, stateInd, otherIdx, 0);
        }
        protected override bool ImplementMethodImpl(IMethod met) {
            //TODO method-implementation
            if (met.IsInternal() || met.IsExternal() || met.IsImport())
                return true;
            if (!TryGetMethod(met, out var fn, met.Position))
                return false;

            if (met.Body.Instruction != null) {

                bool succ = true;
                var entry = new BasicBlock(ctx.Instance, "entry", fn);
                ctx.ResetInsertPoint(entry, IntPtr.Zero);
                uint index = met.IsStatic() ? 0u : 1u;
                InstructionGenerator bodyGen;
                //TODO coroutine: create right instructioncodegen and dont init args(this is done in setupCoro)
                if (met.IsCoroutine()) {
                    bodyGen = CreateCoroInstructionGenerator(this, ctx, fn, met, IntPtr.Zero);
                }
                else {

                    bodyGen = //new InstructionGenerator(this, ctx, fn);
                                 CreateInstructionGenerator(this, ctx, fn, FunctionType.FromMethod(met), met);
                    foreach (var arg in met.Arguments) {
                        if (!arg.Type.IsByRef()) {
                            if (succ &= TryGetVariableType(arg, out var argTp)) {
                                var argAlloca = ctx.DefineInitializedAlloca(fn, argTp, ctx.GetArgument(fn, index), arg.Signature.Name + "Local", IntPtr.Zero, false);
                                //variables[arg] = argAlloca;
                                bodyGen.AddVariable(arg, argAlloca);
                            }
                        }
                        else
                            //variables[arg] = ctx.GetArgument(fn, index);
                            bodyGen.AddVariable(arg, ctx.GetArgument(fn, index));
                        ++index;
                    }
                }
                if (met.Signature.Name == "main" && met.IsStatic() && met.NestedIn is Module) {
                    // initializer EH
                    var initEH = GetOrCreateInternalFunction(ctx, InternalFunction.initExceptionHandling);
                    ctx.GetCall(initEH, Array.Empty<IntPtr>(), IntPtr.Zero);
                    // initialize GC
                    var gcinit = GetOrCreateInternalFunction(ctx, InternalFunction.gc_init);
                    ctx.GetCall(gcinit, Array.Empty<IntPtr>(), IntPtr.Zero);
                    // var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
                    // ctx.GetCall(malloc, new[] { ctx.GetInt32(0) }, IntPtr.Zero);
                }
                succ &= //TryInstructionCodeGen(met.Body);
                    bodyGen.TryInstructionCodeGen(met.Body.Instruction)
                    & bodyGen.FinalizeMethodBody();
                return succ;
            }
            else if (!met.IsAbstract()) {
                return $"The non-abstract method {met.Signature} must provide a body".Report(met.Position, false);
            }
            return true;
        }

        protected override bool ImplementMethodTemplateImpl(IMethodTemplate<IMethod> met) {
            return true;
        }
        public bool TryGetFunctionPtrType(IMethod met, out IntPtr ret) {
            if (!met.IsAbstract() && TryGetMethod(met, out var fn, met.Position)) {
                ret = ctx.GetFunctionPtrTypeFromFunction(fn);
                return true;
            }
            else {
                bool succ = true;

                succ &= TryGetVariableType(met.ReturnType, false, out var retTy);
                var argTys = Vector<IntPtr>.Reserve((uint) met.Arguments.Length + (met.IsStatic() ? 0u : 1u));
                if (!met.IsStatic()) {
                    if (met.NestedIn is ITypeContext tcx && tcx.Type != null) {
                        IntPtr thisTy;
                        if (tcx.Type.IsInterface()) {
                            thisTy = ctx.GetVoidPtr();
                        }
                        else {

                            succ &= TryGetReferenceType(tcx.Type, out thisTy);
                        }
                        argTys.Add(thisTy);
                    }
                    else {
                        ret = IntPtr.Zero;
                        return $"The non-static method {met.Signature} must be declared inside a type".Report(met.Position, false);
                    }
                }
                foreach (var x in met.Arguments) {
                    succ &= TryGetVariableType(x, out var argTy);
                    argTys.Add(argTy);
                }
                ret = succ ? ctx.GetFunctionPtrType(retTy, argTys.AsArray()) : IntPtr.Zero;
                return succ;
            }
        }
        public bool TryGetFunctionPtrType(FunctionType fnTp, IType isInstance, out IntPtr ret) {
            bool succ = true;

            succ &= TryGetVariableType(fnTp.ReturnType, false, out var retTy);
            var argTys = Vector<IntPtr>.Reserve((uint) fnTp.ArgumentTypes.Count + (isInstance is null ? 0u : 1u));
            if (isInstance != null) {

                IntPtr thisTy;
                if (isInstance.IsInterface()) {
                    thisTy = ctx.GetVoidPtr();
                }
                else {

                    succ &= TryGetReferenceType(isInstance, out thisTy);
                }
                argTys.Add(thisTy);

            }
            foreach (var x in fnTp.ArgumentTypes) {
                succ &= TryGetVariableType(x, false, out var argTy);
                argTys.Add(argTy);
            }
            ret = succ ? ctx.GetFunctionPtrType(retTy, argTys.AsArray()) : IntPtr.Zero;
            return succ;
        }
        #region LazyCreation: Method
        private bool TryGetArgcArgvMainMethod(IMethod met, out IntPtr ret) {
            var entry = ctx.DeclareFunction("main", ctx.GetIntType(), new[] { ctx.GetIntType(), ctx.GetPointerType(ctx.GetPointerType(ctx.GetByteType())) }, new[] { "argc", "argv" }, true);

            bool succ = TryGetType(met.ReturnType, out var retTy);
            succ &= TryGetType(met.Arguments[0].Type, out var args);
            ret = ctx.DeclareFunction("_main_", retTy, new[] { args }, new[] { met.Arguments[0].Signature.Name }, met.Visibility == Visibility.Public);
            methods.Add(met, ret);
            // add code to marshal the arguments and call the real main-method
            var entryBlock = new BasicBlock(ctx.Instance, "entry", entry);
            ctx.ResetInsertPoint(entryBlock.Instance, IntPtr.Zero);
            var retVal = ctx.MarshalMainMethodCMDLineArgs(ret, entry, IntPtr.Zero);
            if (met.ReturnType.IsPrimitive(PrimitiveName.Void))
                ctx.ReturnValue(ctx.GetInt32(0), IntPtr.Zero);
            else
                ctx.ReturnValue(retVal, IntPtr.Zero);
            return succ;
        }
        private bool TryGetProgramOptionsMainMethod(IMethod met, out IntPtr ret) {
            //TODO include program options
            throw new NotImplementedException();
        }

        internal bool TryGetMainMethod(IMethod met, out IntPtr ret) {
            if (met.Arguments.Any()) {
                if (met.Arguments.Length == 1
                    && !met.Arguments[0].IsLocallyAllocated()
                    && met.Arguments[0].Type.IsArray()
                    && (met.Arguments[0].Type as IWrapperType).ItemType.IsPrimitive(PrimitiveName.String)) {
                    return TryGetArgcArgvMainMethod(met, out ret);
                }
                else {
                    return TryGetProgramOptionsMainMethod(met, out ret);
                }
            }

            if (met is BasicMethod bm)
                bm.Visibility = Visibility.Public;
            return TryGetMethodInternal(met, out ret, default);
        }



        public bool TryGetMethod(IMethod met, out IntPtr ret, Position pos) {
            if (methods.TryGetValue(met, out ret)) {
                return true;
            }
            if (met.Signature.Name == "main" && met.IsStatic() && met.NestedIn is Module) {
                return TryGetMainMethod(met, out ret);
            }
            return TryGetMethodInternal(met, out ret, pos);
        }
        bool TryGetMethodInternal(IMethod met, out IntPtr ret, Position pos) {
            if (met.IsAbstract()) {
                // when declaring a bodyless function in llvm, it must be imported from a library,
                // so don't declare abstract methods

                ret = ctx.GetNullPtr();
                var parent = (met.NestedIn as ITypeContext)?.Type;
                if (parent != null)
                    virtualMethods.Add(parent, met);
                return true;
            }
            if (met.IsBuiltin())
                return GetOrCreateBuiltinFunction(pos, met, out ret);
            if (met.IsInternal()) {
                ret = GetOrCreateInternalFunction(ctx, met);
                return true;
            }
            bool succ = TryGetType(met.ReturnType, out var retTy);
            var argTys = met.Arguments.Select(x => {
                /*succ &= TryGetType(x.Type, out var argTy);
                if (x.IsLocallyAllocated() || x.Type.IsPrimitive() || x.Type.IsArray() || x.Type.IsFixedSizedArray() || x.Type.IsArraySlice() || x.Type.IsVarArg())
                    return argTy;
                else
                    return ctx.GetPointerType(argTy);*/
                succ &= TryGetVariableType(x, out var argTy);
                return argTy;
            });
            if (!met.IsStatic()) {
                var parent = (met.NestedIn as ITypeContext)?.Type;
                if (parent != null) {
                    succ &= TryGetType(parent.AsReferenceType(), out var parTy);
                    argTys = argTys.Prepend(parTy);
                    if (met.IsVirtual())
                        virtualMethods.Add(parent, met);
                }
                else {
                    succ &= $"The non-static method {met.Signature} must be located inside a class".Report(met.Position, false);
                }
            }
            ret = ctx.DeclareFunction(mangler.MangleFunctionName(met), retTy, argTys.ToArray(), met.Arguments.Select(x => x.Signature.Name).ToArray(), met.Visibility == Visibility.Public);

            for (int i = 0, j = met.IsStatic() ? 0 : 1; i < met.Arguments.Length; ++i, ++j) {
                var arg = met.Arguments[i];
                if (arg.Type.IsByRef())
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "nonnull" }, (uint) j);
                else if (arg.Type.IsNotNullable() && !arg.Type.IsValueType() && !arg.Type.IsArraySlice())
                    ctx.AddParamAttributes(ret, new[] { "nonnull" }, (uint) j);

                if (arg.Type.IsByConstRef() || (!arg.Type.IsValueType() && arg.Type.IsConstant()))
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, (uint) j);
                if (arg.Type.IsUnique())
                    ctx.AddParamAttributes(ret, new[] { "noalias" }, (uint) j);
            }
            if (met.ReturnType.IsNotNullable() && !met.ReturnType.IsValueType() && !met.ReturnType.IsArraySlice() || met.ReturnType.IsByRef())
                ctx.AddReturnNotNullAttribute(ret);
            if (met.ReturnType.IsUnique())
                ctx.AddReturnNoAliasAttribute(ret);
            methods.Add(met, ret);
            allMethods.Add(met);
            return succ;
        }

        private bool GetOrCreateBuiltinFunction(Position pos, IMethod met, out IntPtr ret) {
            if (met.NestedIn is ITypeContext tcx && tcx.Type.Signature.BaseGenericType != null && tcx.Type.Signature.BaseGenericType.Signature.Name == "::HashMap") {
                return bhm.TryGetMethod(pos, met, out ret);
            }
            throw new NotImplementedException();
        }
        #endregion
        #region LazyCreation: Type

        public IntPtr GetGlobalInterfaceType() {
            if (interfaceType == IntPtr.Zero) {
                var ret = ctx.GetStruct("interface");
                var voidPtr = ctx.GetVoidPtr();
                // %interface = type { i8*, i8* }
                // %interface[0] = basePtr
                // %interface[1] = vtablePtr
                ctx.CompleteStruct(ret, new[] { voidPtr, voidPtr });
                interfaceType = ret;
            }
            return interfaceType;
        }
        public uint GetSuperPosition(IType tp) {
            if (superIndex.TryGetValue(tp, out var ret))
                return ret;
            return 0;
        }
        public static bool IsReferenceType(IType tp) {
            // arrays are per default reference-types and spans are structs; interfaces are structs
            return !tp.IsValueType() && !tp.IsArraySlice() && !tp.IsInterface();
        }
        public bool TryGetVariableType(IVariable vr, out IntPtr tp) {
            return TryGetVariableType(vr.Type, vr.IsLocallyAllocated(), out tp);
        }
        public bool TryGetVariableType(IType tp, bool isLocallyAllocated, out IntPtr ret) {
            /*if (!TryGetType(tp, out ret))
                return false;
            if (!(isLocallyAllocated || tp.IsPrimitive() || tp.IsArray() || tp.IsFixedSizedArray() || tp.IsArraySlice() || tp.IsInterface())) {
                ret = ctx.GetPointerType(ret);
            }
            return true;*/
            return TryGetType(isLocallyAllocated ? tp.AsValueType() : tp, out ret);
        }
        public bool TryGetReferenceType(IType src, out IntPtr tp) {
            if (!TryGetType(src, out tp))
                return false;
            // for fixed-sized arrays, get pointer (in contrast to TryGetVariableType)
            if (src.IsFixedSizedArray()) {
                tp = ctx.GetPointerType(tp);
            }
            return true;
        }
        internal IntPtr GetArrayType(ArrayType tp) {
            TryGetVariableType(tp.ItemType, false, out var lltp);
            //var arr = ctx.GetArrayType(lltp, 0);
            var ret = //ctx.GetPointerType(ctx.GetUnnamedStruct(new[] { ctx.GetIntType(), arr }));
                ctx.GetPointerType(GetArrayStructType(lltp, 0));
            types[tp] = ret;
            return ret;
        }
        internal IntPtr GetFSArrayType(FixedSizedArrayType tp) {
            TryGetType(tp.ItemType, out var lltp);
            if (!tp.ArrayLength.TryConvertToUInt(out var len)) {
                "The length of a fixed-sized array must be a 32Bit compiletime-constant unsigned integer".Report(tp.Position);
            }
            var ret = //ctx.GetUnnamedStruct(new[] { ctx.GetIntType(), ctx.GetArrayType(lltp, len) });
                GetArrayStructType(lltp, len);
            types[tp] = ret;
            return ret;
        }
        public IntPtr GetArrayStructType(IntPtr itemTy, uint len) {
            return ctx.GetUnnamedStruct(new[] { ctx.GetSizeTType(), ctx.GetArrayType(itemTy, len) });
        }
        internal IntPtr GetSpanType(SpanType tp) {
            TryGetVariableType(tp.ItemType, false, out var lltp);
            return GetSpanType(tp, lltp);
        }
        internal IntPtr GetSpanType(SpanType tp, IntPtr itemTy) {
            var ret = ctx.GetUnnamedStruct(new[] { ctx.GetPointerType(itemTy), ctx.GetSizeTType() });
            types[tp] = ret;
            return ret;
        }
        internal IntPtr GetNativePointerType(IType tp) {
            TryGetType(tp, out var lltp);
            var ret = ctx.GetPointerType(lltp);
            return ret;
        }
        internal IntPtr GetPrimitiveType(PrimitiveType tp) {
            IntPtr ret;
            switch (tp.PrimitiveName) {
                case PrimitiveName.Bool:
                    ret = ctx.GetBoolType();
                    break;
                case PrimitiveName.Byte:
                case PrimitiveName.Char:
                    ret = ctx.GetByteType();
                    break;
                case PrimitiveName.Short:
                case PrimitiveName.UShort:
                    ret = ctx.GetShortType();
                    break;
                case PrimitiveName.Int:
                case PrimitiveName.UInt:
                    ret = ctx.GetIntType();
                    break;
                case PrimitiveName.SizeT: {
                    ret = Environment.Is64BitOperatingSystem ? ctx.GetLongType() : ctx.GetIntType();
                    break;
                }
                case PrimitiveName.Long:
                case PrimitiveName.ULong:
                    ret = ctx.GetLongType();
                    break;
                case PrimitiveName.BigLong:
                case PrimitiveName.UBigLong:
                    ret = ctx.GetBiglongType();
                    break;
                case PrimitiveName.Float:
                    ret = ctx.GetFloatType();
                    break;
                case PrimitiveName.Double:
                    ret = ctx.GetDoubleType();
                    break;
                case PrimitiveName.String:
                    ret = ctx.GetStringType();
                    break;
                case PrimitiveName.Void:
                    ret = ctx.GetVoidType();
                    break;
                default:
                    ret = ctx.GetVoidPtr();
                    break;
            }
            types[tp] = ret;
            return ret;
        }
        internal IntPtr GetInterfaceType(IType tp) {
            // interfaces are independent from the actual type, since they have no fields and only abstract methods.
            // Hence, ignore tp
            // TODO interfaces implementing other interfaces
            var ret = GetGlobalInterfaceType();
            types[tp] = ret;
            return ret;
        }
        internal IntPtr GetVarArgType(VarArgType tp, IntPtr itemTy) {
            // vararg:
            // struct vararg<T>{ span<T>* spans, uint spanc, size_t totalLen, size_t totalOffset};
            var intTy = ctx.GetIntType();
            var sizeTy = ctx.GetSizeTType();
            var spanTy = GetSpanType(tp.ItemType.AsVarArg(), itemTy);
            var ret = ctx.GetStruct($"var_arg<{tp.ItemType.FullName()}>");
            ctx.CompleteStruct(ret, new[] {
                ctx.GetPointerType(spanTy),// spans
                intTy,                     // spanc
                sizeTy,                     // totalLength
                sizeTy                      // totalOffset
            });
            types[tp] = ret;
            return ret;
        }

        bool? TryGetSpecialType(IType tp, out IntPtr ret) {

            if (tp.IsFixedSizedArray()) {

                ret = GetFSArrayType(tp as FixedSizedArrayType);
                return true;
            }
            if (tp.IsArray()) {
                ret = GetArrayType(tp as ArrayType);
                return true;
            }
            else if (tp.IsAwaitable()) {
                // awaitable
                if ((tp as IWrapperType).ItemType.IsVoid())
                    ret = ctx.GetPointerType(ctx.GetOpaqueType(":Task"));
                else
                    ret = ctx.GetPointerType(ctx.GetOpaqueType($":Task<{(tp as IWrapperType).ItemType.FullName()}>"));
                types[tp] = ret;
                return true;
            }
            else if (tp.IsArraySlice()) {
                if (tp is SpanType spanTp)
                    ret = GetSpanType(spanTp);
                else if (tp is ByRefType brt)
                    ret = GetNativePointerType(brt.UnderlyingType);
                else
                    ret = GetNativePointerType(tp);
                return true;
            }
            else if (tp.IsPrimitive()) {
                ret = GetPrimitiveType(tp as PrimitiveType);
                return true;
            }
            else if (tp.IsInterface()) {
                ret = GetInterfaceType(tp);
                // recognize all methods as virtual (abstract)
                return DeclareMethods(tp.Context);
            }
            else if (tp.IsNativePointer()) {
                ret = GetNativePointerType(tp);
                return true;
            }
            else if (tp.IsVarArg()) {

                if (!TryGetType((tp as IWrapperType).ItemType, out var itemTy)) {
                    ret = default;
                    return false;
                }
                ret = GetVarArgType((tp as VarArgType), itemTy);
                return true;
            }
            else if (tp.IsBuiltin()) {
                return TryGetBuiltinType(tp, out ret);
            }
            ret = IntPtr.Zero;
            return null;
        }

        private bool TryGetBuiltinType(IType tp, out IntPtr ret) {
            if (tp.Signature.BaseGenericType != null && tp.Signature.BaseGenericType.Signature.Name == "::HashMap") {
                var keyTy = (IType)tp.Signature.GenericActualArguments.First();
                var valTy = (IType)tp.Signature.GenericActualArguments.ElementAt(1);
                return bhm.TryGetBuiltinHashMap(tp, keyTy, valTy, out ret);
            }
            ret = ctx.GetVoidPtr();
            return $"Invalid builtin type: {tp.Signature}".Report(false);
        }



        public bool TryGetType(IType tp, out IntPtr ret) {
            if (tp is null) {
                ret = default;
                return false;
            }
            if (types.TryGetValue(tp, out ret))
                return true;
            if (tp.IsByRef()) {
                if (!TryGetType((tp as ModifierType).UnderlyingType, out var underlying))
                    return false;
                ret = ctx.GetPointerType(underlying);
                types[tp] = ret;
                return true;
            }
            if ((tp.IsUnique() || tp.IsTransition() || tp.IsImmutable() || tp.IsConstant() || tp.IsTag()) && tp is RefConstrainedType rct) {
                return TryGetType(rct.UnderlyingType, out ret);
            }
            if (tp is ReferenceValueType rvt) {
                if (rvt.IsValueType()) {
                    if (TryGetType(rvt.UnderlyingType, out _) && structTypes.TryGetValue(rvt.UnderlyingType, out var valueTy)) {
                        types[rvt] = ret = valueTy;
                        return true;
                    }
                    return $"There is no corresponding value-type for {rvt.UnderlyingType}".Report(tp.Position, false);
                }
                else {
                    ret = default;
                    "Upgrading value-types to reference-types is currently not supported".Report(tp.Position);
                    return false;
                }
            }
            if (tp is NotNullableType nnt) {
                return TryGetType(nnt.UnderlyingType, out ret);
            }
            var isSpecialType = TryGetSpecialType(tp, out ret);
            if (isSpecialType != null)
                return isSpecialType.Value;


            bool succ = true;
            var structTy = ctx.GetStruct(mangler.MangleTypeName(tp));
            structTypes.Add(tp, structTy);
            if (tp.IsValueType()) {
                types.Add(tp, ret = structTy);
            }
            else {
                types.Add(tp, ret = ctx.GetPointerType(structTy));
            }
            incomplete.Add(tp.Signature);

            IType superTp = tp is IHierarchialType htp ? htp.SuperType : null;

            uint index = 0;
            if (tp.CanBeInherited() && superTp == null) {
                // vtable gets position 0
                index = 1;
            }
            else {// since there is no vtable (or the supertype has already one), store the underlying superObject at position 0
                superIndex[tp] = 0;
                if (!tp.CanBeInherited() && superTp == null)
                    index = 0;
                else
                    index = 1;
            }
            IEnumerable<IntPtr> flds = Enumerable.Empty<IntPtr>();

            if (tp.IsActor()) {
                if (superTp == null) {
                    flds = //flds.Concat(new[] { ctx.GetVoidPtr(), ctx.GetBoolType() });
                        flds.Append(ctx.GetVoidPtr());
                    actorQueueIndex[tp] = index;
                    index++;
                }
                else {
                    // assert superTp is Actor too
                    actorQueueIndex[tp] = actorQueueIndex[superTp];
                }
            }

            // interface-vtables

            var intfs = tp is ClassType ctp ? ctp.AllImplementingInterfaces() : tp.ImplementingInterfaces;
            uint newIntfCount;
            if (superTp != null) {
                newIntfCount = 0;
                foreach (var intf in intfs) {
                    if (superTp.ImplementsInterface(intf)) {
                        interfaceVTableSlots[(tp, intf)] = interfaceVTableSlots[(superTp, intf)];
                    }
                    else {
                        newIntfCount++;
                        interfaceVTableSlots[(tp, intf)] = index++;
                    }
                }
                //index += newIntfCount = (uint)tp.ImplementingInterfaces.Count(x => !superTp.ImplementsInterface(x));
            }
            else {
                newIntfCount = index;
                foreach (var intf in intfs) {
                    interfaceVTableSlots[(tp, intf)] = index++;
                }
                newIntfCount = index - newIntfCount;
                //index += newIntfCount = (uint)tp.ImplementingInterfaces.Count;
            }
            //index = superTp != null ? 1u : 0u;
            flds = flds.Concat(tp.Context.InstanceContext.LocalContext.Variables.Values.Select(x => {
                if (incomplete.Contains(x.Type.Signature) && x.IsLocallyAllocated()) {
                    succ = $"The variable {x.Signature} cannot be declared in {tp.Context.Name}, since it would result in endless-recursion".Report(x.Position, false);
                }
                succ &= TryGetVariableType(x, out var fldTp);
                instanceFields[x] = index++;
                return fldTp;
            }));
            if (tp.ImplementingInterfaces.Any()) {
                // for each new interface reserve a pointer to the vtable
                flds = Enumerable.Repeat(ctx.GetVoidPtr(), (int) newIntfCount).Concat(flds);
            }
            if (superTp != null) {
                succ &= TryGetType(superTp, out _) & structTypes.TryGetValue(superTp, out var superTy);
                // prepend the super-object
                flds = flds.Prepend(superTy);
            }
            else if (tp.CanBeInherited()) {
                // prepend the new vtablePtr
                flds = flds.Prepend(ctx.GetVoidPtr());
            }

            ctx.CompleteStruct(structTy, flds.ToArray());
            incomplete.Remove(tp.Signature);
            succ &= DeclareMethods(tp.Context);
            return succ;
        }


        public void Dispose() => ((IDisposable) ctx).Dispose();
        #endregion


        public static bool CanLosslesslyBitCastToVoidPtr(IType tp, AddressWidth wd = AddressWidth.Unspecified) {
            if (wd == AddressWidth.Unspecified)
                wd = Address.CurrentOSAddressWidth;
            if (tp is PrimitiveType prim) {
                switch (prim.PrimitiveName) {
                    case PrimitiveName.Null:
                    case PrimitiveName.Handle:
                    case PrimitiveName.Bool:
                    case PrimitiveName.Byte:
                    case PrimitiveName.Char:
                    case PrimitiveName.Short:
                    case PrimitiveName.UShort:
                    case PrimitiveName.Int:
                    case PrimitiveName.UInt:
                    case PrimitiveName.Float:
                    case PrimitiveName.SizeT:
                        return true;
                    case PrimitiveName.Long:
                    case PrimitiveName.ULong:
                    case PrimitiveName.Double:
                        return wd == AddressWidth._64;
                    default:
                        return false;
                }
            }
            else {
                return !tp.IsValueType() && !tp.IsInterface() && !tp.IsArraySlice();
            }
        }
        public IntPtr GetVoidPtrFromValue(IntPtr val, IType valTy, IntPtr irb) {
            if (CanLosslesslyBitCastToVoidPtr(valTy)) {
                return ctx.ForceCast(val, ctx.GetVoidPtr(), false, irb);
            }
            else {
                var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
                var ty = ctx.GetTypeFromValue(val);
                var sz = ctx.GetI32SizeOf(ty);
                var ret = ctx.GetCall(malloc, new[] { sz }, irb);
                var bcRet = ctx.ForceCast(ret, ctx.GetPointerType(ty), false, irb);
                ctx.Store(bcRet, val, irb);
                return ret;
            }
        }
        public IntPtr GetValueFromVoidPtr(IntPtr ptr, IType valTy, IntPtr irb) {
            if (!TryGetType(valTy, out var llValTy))
                throw new InvalidProgramException("Fatal Error");
            if (CanLosslesslyBitCastToVoidPtr(valTy)) {
                return ctx.ForceCast(ptr, llValTy, false, irb);
            }
            else {
                var bcMem = ctx.ForceCast(ptr, ctx.GetPointerType(llValTy), false, irb);
                return ctx.Load(bcMem, irb);
            }
        }

        IntPtr DeclareIsInstanceOf(IType ty) {
            var ret = ctx.DeclareFunction(ty.FullName() + ".isInstanceOf", ctx.GetBoolType(), new[] { ctx.GetVoidPtr() }, new[] { "other" }, true);
            ctx.AddFunctionAttributes(ret, new[] { "readonly", "nounwind", "argmemonly" });
            ctx.AddParamAttributes(ret, new[] { "nocapture", "nonnull" }, 0);
            return ret;
        }
        void CreateIsInstanceOf(IType ty, IntPtr isInst) {
            BasicBlock
                entry = new BasicBlock(ctx, "entry", isInst),
                hashEntry = new BasicBlock(ctx, "hashEntry", isInst),
                returnTrue = new BasicBlock(ctx, "returnTrue", isInst),
                returnFalse = new BasicBlock(ctx, "returnFalse", isInst);
            using (var irb = new IRBuilder(ctx)) {
                ctx.ResetInsertPoint(entry, irb);
                var other = ctx.GetArgument(isInst, 0);
                TryGetVTable(ty, out var thisVT, irb);
                //TODO interfaces
                var exact = ctx.CompareOp(thisVT.VTablePointer, other, (sbyte) '!', true, false, irb);
                ctx.ConditionalBranch(returnTrue, hashEntry, exact, irb);
                ctx.ResetInsertPoint(hashEntry, irb);

                //var thisHash = thisVT.VirtualMethods[thisVT.TypeIdIndex];
                // since this and other are in the same inheritance-hierarchy, the typeid-index is the same
                var otherHash = ctx.LoadFieldConstIdx(ctx.ForceCast(other, ctx.GetPointerType(thisVT.VTableType), false, irb), new[] { 0u, thisVT.TypeIdIndex }, irb);
                Vector<IntPtr>
                    caseConds = default,
                    caseLabels = default;
                var collisionSolver = new MultiMap<ulong, IType>();
                for (var type = ty; type != null; type = (type as IHierarchialType)?.SuperType) {
                    collisionSolver.Add(GetTypeId(type), type);
                    foreach (var intf in type.ImplementingInterfaces) {
                        collisionSolver.Add(GetTypeId(intf), intf);
                    }
                }


                foreach (var tid in collisionSolver.Keys) {
                    caseConds.Add(ctx.GetInt64(tid));
                    var bb = new BasicBlock(ctx, "case" + tid, isInst);
                    caseLabels.Add(bb);
                    ctx.ResetInsertPoint(bb, irb);

                    foreach (var type in collisionSolver[tid]) {
                        TryGetVTable(type, out var typeVT, irb);
                        var cmp = ctx.CompareOp(other, typeVT.VTablePointer, (sbyte) '!', true, false, irb);
                        bb = new BasicBlock(ctx, "not_" + type.FullName(), isInst);
                        ctx.ConditionalBranch(returnTrue, bb, cmp, irb);
                        ctx.ResetInsertPoint(bb, irb);
                    }
                    ctx.Branch(returnFalse, irb);
                }
                ctx.ResetInsertPoint(hashEntry, irb);
                ctx.IntegerSwitch(otherHash, returnFalse, caseLabels.AsArray(), caseConds.AsArray(), irb);

                ctx.ResetInsertPoint(returnTrue, irb);
                ctx.ReturnValue(ctx.True(), irb);
                ctx.ResetInsertPoint(returnFalse, irb);
                ctx.ReturnValue(ctx.False(), irb);
            }
        }

        public ulong GetTypeId(IType tp) {
            if (tp.IsVoid())
                return 0;
            if (typeId.TryGetValue(tp, out var ret))
                return ret;

            //ulong moduleHash = ((ulong) tp.Context.Module.ID.GetHashCode()) << 32;
            //ulong typenameHash = (ulong) tp.FullName().GetHashCode();
            var moduleHash = tp.Context.Module.ModuleName.UniqueHash();
            var typenameHash = tp.FullName().UniqueHash();
            ret = moduleHash | typenameHash;
            typeId[tp] = ret;
            return ret;
        }
    }
}
