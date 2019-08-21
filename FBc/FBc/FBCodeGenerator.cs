/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Compiler;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using LLVMCodeGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static NativeManagedContext;

namespace FBc {
    class FBLLVMCodeGenerator : LLVMCodeGenerator.LLVMCodeGenerator {
        private readonly bool emitLLVM;
        private readonly bool native;
        private readonly bool forceLV;
        private readonly bool disableBoundsChecks;
        private readonly bool disableNullChecks;
        const string runtimeName = "BasicBitcodeJIT.lib";

        private readonly LazyDictionary<IType, VirtualMethodTable> iteratorVTables, iterableVTables;



        public FBLLVMCodeGenerator(string outputFilename, FBModule fBModule, OptLevel lvl = OptLevel.O1, bool emitLLVM = false, bool native = false, bool forceLV = false, bool disableBoundsChecks = false, bool disableNullChecks = false, bool isLibrary = false)
            : base(outputFilename, lvl, isLibrary: isLibrary) {
            this.emitLLVM = emitLLVM;
            this.native = native;
            this.forceLV = forceLV;
            this.disableBoundsChecks = disableBoundsChecks;
            this.disableNullChecks = disableNullChecks;
            iteratorVTables = new LazyDictionary<IType, VirtualMethodTable>(ty => {
                TryGetType(ty, out var llTy);
                IntPtr tryGetNextTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr(), ctx.GetPointerType(llTy) });
                IntPtr vtableTy = ctx.GetUnnamedStruct(new[] { tryGetNextTy });

                var vtableVal = new Vector<IntPtr> { ctx.GetNullPtr() };
                var virtMetTys = new Vector<IntPtr> { tryGetNextTy };
                return new VirtualMethodTable(vtableTy, IntPtr.Zero, vtableVal, virtMetTys, 1, 0, 0);
            });
            iterableVTables = new LazyDictionary<IType, VirtualMethodTable>(ty => {
                TryGetType(ty, out var llTy);
                IntPtr getIteratorTy = ctx.GetFunctionPtrType(GetGlobalInterfaceType(), new[] { ctx.GetVoidPtr() });
                IntPtr vtableTy = ctx.GetUnnamedStruct(new[] { getIteratorTy });

                var vtableVal = new Vector<IntPtr> { ctx.GetNullPtr() };
                var virtMetTys = new Vector<IntPtr> { getIteratorTy };
                return new VirtualMethodTable(vtableTy, IntPtr.Zero, vtableVal, virtMetTys, 1, 0, 0);
            });
        }
        protected override InstructionGenerator CreateInstructionGenerator(LLVMCodeGenerator.LLVMCodeGenerator gen, NativeManagedContext.ManagedContext ctx, IntPtr function, FunctionType metTy, IMethod met) {
            return new FBInstructionGenerator(gen, ctx, function, metTy, met: met) { ForceLoopVectorization = forceLV, DoBoundsChecks = !disableBoundsChecks, DoNullChecks = !disableNullChecks };
        }
        protected override InstructionGenerator CreateCoroInstructionGenerator(LLVMCodeGenerator.LLVMCodeGenerator gen, ManagedContext ctx, IntPtr function, IMethod method, IntPtr irb = default) {
            function = SetupCoro(method, function, irb, FBModule.StaticSemantics, out var numSuspendPoints, out var coroTp, out var kind, out var localIdx, out var thisInd, out var stateInd, out var otherIdx);
            return new FBInstructionGenerator(gen, ctx, function, coroTp, numSuspendPoints, kind, localIdx, thisInd, stateInd, otherIdx, 0) { ForceLoopVectorization = forceLV, DoBoundsChecks = !disableBoundsChecks, DoNullChecks = !disableNullChecks };
        }

        internal void SetFieldSlot(IVariable vr, uint slot) {
            instanceFields[vr] = slot;
        }
        internal bool TryGetFieldSlot(IVariable vr, out uint slot) {
            return instanceFields.TryGetValue(vr, out slot);
        }
        public override bool TryGetVirtualMethodSlot(IMethod met, out uint slot, IntPtr irb) {
            if (!base.TryGetVirtualMethodSlot(met, out slot, irb)) {
                if (met.NestedIn is ITypeContext tcx && (tcx.Type is IteratorType || tcx.Type is IterableType)) {
                    slot = 0;
                    return true;
                }
                return false;
            }
            return true;
        }
        public override bool TryGetVTable(IType ty, out VirtualMethodTable vt, IntPtr irb) {
            if (!base.TryGetVTable(ty, out vt, irb)) {
                if (ty is IteratorType it) {
                    vt = iteratorVTables[it.ItemType];
                    return true;
                }
                else if (ty is IterableType iter) {
                    vt = iterableVTables[iter.ItemType];
                    return true;
                }
                return false;
            }
            return true;
        }

        internal bool TryGetQueueIndex(IType actor, out uint qInd) {
            var ret = actorQueueIndex.TryGetValue(actor, out qInd);
            return ret;
        }
        public override void FinalizeCodeGen() {
            if (ErrorCollector.HasErrors)
                return;
            if (ctx.VerifyModule()) {
                if (optLvl != 0)
                    ctx.Optimize((byte) optLvl, 8);
                if (!IsLibrary)
                    ctx.LinkTimeOptimization();

                if (ctx.VerifyModule()) {
                    if (emitLLVM) {
                        ctx.DumpModule(OutputFilename);
                    }
                    else if (native) {
                        if (PathLocator.TryLocateFile(runtimeName, out var bbjit)) {
                            try {
                                string bcFileName = Path.GetTempFileName() + ".bc";
                                ctx.Save(bcFileName);
                                if (!Clang.Compile(new[] { bcFileName, bbjit }, OutputFilename, new[] { Path.GetFileNameWithoutExtension(runtimeName) })) {
                                    "Failed to compile the bitcode to native code".Report();
                                }
                                File.Delete(bcFileName);
                            }
                            catch (IOException e) {
                                e.Message.Report();
                            }
                        }
                        else {
                            $"Cannot find the runtime {runtimeName}. It needs to be linked into the executable. Otherwise it will run.".Report();
                        }
                    }
                    else {
                        ctx.Save(OutputFilename);
                    }
                    return;
                }
            }
            ("Invalid LLVM IR -- see dump in " + OutputFilename + ".ll").Report();
            ctx.DumpModule(OutputFilename + ".ll");
        }
        protected override void CollectCoroSaveSlots(IStatement stmt, ref Vector<IntPtr> llFlds, ref bool hasError, IDictionary<IVariable, uint> localIndices, IDictionary<IExpression, uint> otherIndices) {
            base.CollectCoroSaveSlots(stmt, ref llFlds, ref hasError, localIndices, otherIndices);
            if (stmt is ForeachLoop fel && fel.Range is ContiguousRangeExpression cre) {
                var itemTy = (cre.ReturnType as AggregateType).ItemType;
                hasError |= TryGetType(itemTy, out var llItemTy);
                // llFlds.Last == llRangeTy
                llFlds.PopBack();
                // reserve space for 'from' and 'to'
                llFlds.Add(llItemTy);
                llFlds.Add(llItemTy);
            }
        }
    }
    class FBInstructionGenerator : InstructionGenerator {
        new readonly FBLLVMCodeGenerator gen;
        IntPtr actualCaptureType;
        public FBInstructionGenerator(LLVMCodeGenerator.LLVMCodeGenerator _gen, ManagedContext _ctx, IntPtr function, FunctionType metTy, IntPtr irBuilder = default, IMethod met = null)
            : base(_gen, _ctx, function, metTy, irBuilder, met) {
            gen = (FBLLVMCodeGenerator) _gen;

        }

        public FBInstructionGenerator(LLVMCodeGenerator.LLVMCodeGenerator _gen, ManagedContext _ctx, IntPtr function, FunctionType _methodTp, uint numSuspendPoints, CoroutineInfo.Kind coroType, IDictionary<IVariable, uint> localGEP, uint thisGEP, uint stateGEP, IDictionary<IExpression, uint> otherGEP, uint mutArgsInd, IntPtr irBuilder = default)
            : base(_gen, _ctx, function, _methodTp, numSuspendPoints, coroType, localGEP, thisGEP, stateGEP, otherGEP, mutArgsInd, irBuilder) {
            gen = (FBLLVMCodeGenerator) _gen;
        }

        public FBInstructionGenerator(LLVMCodeGenerator.LLVMCodeGenerator _gen, ManagedContext ctx, IntPtr function, in CoroutineFrameInfo frameInfo, IntPtr irBuilder)
            : base(_gen, ctx, function, frameInfo, irBuilder) {
            gen = (FBLLVMCodeGenerator) _gen;
        }

        internal bool ForceLoopVectorization {
            get; set;
        }

        bool TryGetLambdaObjectType(FunctionType fnTy, IReadOnlyCollection<ICaptureExpression> allCaptures, out IntPtr ret, out IntPtr llFnTy) {
            bool succ = true;
            /*managedTy = new ClassType(fnTy.Position, ":LambdaType", Visibility.Internal) {
                Context = new SimpleTypeContext(fnTy.Context.Module)
            };
            managedTy.Context.Type = managedTy;*/
            var body = Vector<IntPtr>.Reserve((uint) allCaptures.Count + 1);
            uint index = 0;
            succ &= gen.TryGetFunctionPtrType(fnTy, PrimitiveType.Handle, out llFnTy);
            body.Add(llFnTy);
            index++;
            foreach (var cap in allCaptures) {
                //DOLATER decide whether to capture that object or not

                succ &= gen.TryGetVariableType(cap.Variable, out var ty);
                body.Add(ty);
                gen.SetFieldSlot(cap.Variable, index++);
                //managedTy.Context.InstanceContext.DefineVariable(cap.Variable);

            }


            if (succ)
                ret = ctx.GetUnnamedStruct(body.AsArray());
            else
                ret = ctx.GetVoidType();
            return succ;
        }
        bool TryGetTaskContextType(IType retTy, IReadOnlyCollection<ICaptureExpression> allCaptures, out IntPtr ret, out IntPtr llFnTy) {
            bool succ = true;
            var body = Vector<IntPtr>.Reserve((uint) allCaptures.Count);
            if ((retTy as IWrapperType).ItemType.IsVoid()) {
                llFnTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr() });
            }
            else {
                //succ &= gen.TryGetType(retTy, out var llRetTy);
                llFnTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr(), ctx.GetPointerType(ctx.GetVoidPtr()) });
            }

            uint index = 0;
            foreach (var cap in allCaptures) {
                //DOLATER decide whether to capture that object or not

                succ &= gen.TryGetVariableType(cap.Variable, out var ty);
                body.Add(ty);
                gen.SetFieldSlot(cap.Variable, index++);
            }

            if (succ)
                ret = ctx.GetUnnamedStruct(body.AsArray());
            else
                ret = ctx.GetVoidType();
            return succ;
        }
        bool TryGetConcurrentForLoopBodyContextType(ConcurrentForLoop cfl, IVariable[] args, IReadOnlyCollection<ICaptureExpression> allCaptures, out IntPtr ret, out IntPtr retMutable, out bool hasMutableState, out IntPtr llFnTy, out CoroutineFrameInfo frameInfo) {
            frameInfo = new CoroutineFrameInfo {
                localIdx = new Dictionary<IVariable, uint>(),
                otherIdx = new Dictionary<IExpression, uint>(),
                stateInd = uint.MaxValue,
                thisInd = uint.MaxValue
            };
            IType elemTy = PrimitiveType.Void;
            var hasError = false;
            var llFlds = new Vector<IntPtr> {
                ctx.GetIntType(),// state
                ctx.GetVoidPtr(),// this-task
                ctx.GetVoidPtr() // awaited task
            };
            uint index;
            frameInfo.numSuspendPoints = gen.CountSuspendPoints(cfl.Body.Body.Instruction, frameInfo.localIdx, frameInfo.otherIdx, ref elemTy, ref hasError, ref llFlds);
            if (frameInfo.numSuspendPoints > 0) {
                frameInfo.stateInd = 0;
                index = llFlds.Length;
            }
            else
                index = llFlds.Length - 3;

            //var body = Vector<IntPtr>.Reserve((uint)allCaptures.Count);

            var argIdx = new Dictionary<IVariable, uint>();
            foreach (var arg in args) {
                hasError |= !gen.TryGetVariableType(arg, out var ty);
                llFlds.Add(ty);
                argIdx[arg] = index;
                frameInfo.localIdx[arg] = index;
                gen.SetFieldSlot(arg, index++);
            }
            var immuFlds = Vector<IntPtr>.Reserve((uint) allCaptures.Count);
            index = 0;
            foreach (var cap in allCaptures) {
                //DOLATER decide whether to capture that object or not

                hasError |= !gen.TryGetVariableType(cap.Variable, out var ty);
                immuFlds.Add(ty);
                if (!argIdx.ContainsKey(cap.Variable.Original))
                    gen.SetFieldSlot(cap.Variable, index++);
                if (cap.Variable.IsThisCapture) {
                    frameInfo.thisInd = index;
                }
            }
            hasMutableState = frameInfo.numSuspendPoints > 0;
            if (!hasError) {
                ret = ctx.GetStruct(":concurrentForLoopBody.frameTy");
                ctx.CompleteStruct(ret, immuFlds.AsArray());
                retMutable = ctx.GetStruct(":concurrentForLoopBody.mutableFrameTy");
                ctx.CompleteStruct(retMutable, llFlds.AsArray());
                if (hasMutableState)
                    frameInfo.mutArgsInd = 1;
            }
            else {
                ret = retMutable = ctx.GetVoidType();
            }

            var longTy = ctx.GetLongType();
            llFnTy = hasMutableState
                ? ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetPointerType(ret), ctx.GetPointerType(retMutable), ctx.GetPointerType(longTy), longTy })
                : ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetPointerType(ret), longTy, longTy });
            return !hasError;
        }
        bool TryGetLambdaObject(LambdaExpression lambda, FunctionType lambdaFnTy, IReadOnlyCollection<ICaptureExpression> captures, IntPtr capTy, IntPtr llFnTy, out IntPtr cap) {
            bool succ = true;

            #region Declare and implement anon function
            var lambdaFn = ctx.DeclareFunctionOfType(":lambda", llFnTy, false);
            var bodyGen = new FBInstructionGenerator(gen, ctx, lambdaFn, lambdaFnTy, new IRBuilder(ctx)) {
                actualCaptureType = capTy
            };
            var lambdaEntry = new BasicBlock(ctx, "entry", lambdaFn);
            ctx.ResetInsertPoint(lambdaEntry, bodyGen.irb);
            succ &= bodyGen.TryInstructionCodeGen(lambda.Body.Instruction);

            #endregion

            var voidPtr = ctx.GetVoidPtr();
            var ui = ctx.GetIntType();
            var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
            var sz = ctx.GetSizeOf(capTy);
            succ &= ctx.TryCast(sz, ui, ref sz, true, irb);
            var mem = ctx.GetCall(malloc, new[] { sz }, irb);
            cap = IntPtr.Zero;
            if (!ctx.TryCast(mem, ctx.GetPointerType(capTy), ref cap, false, irb))
                return false;
            uint index = 0;
            var fnGEP = ctx.GetElementPtrConstIdx(cap, new[] { 0u, index++ }, irb);
            ctx.Store(fnGEP, lambdaFn, irb);

            foreach (var acc in captures) {
                if (TryExpressionCodeGen(acc.Original, out var fld)) {
                    var capGEP = ctx.GetElementPtrConstIdx(cap, new[] { 0u, index }, irb);
                    ctx.Store(capGEP, fld, irb);

                }
                else
                    succ = false;
                index++;
            }
            return succ;
        }
        bool TryGetConcurrentForLoopContext(ConcurrentForLoop cfl, IntPtr capTy, IntPtr llFnTy, IntPtr mutStateTy, bool hasMutState, bool unsignedIndices, out IntPtr cap, out IntPtr mutState, out IntPtr lambdaFn, in CoroutineFrameInfo frameInfo) {
            bool succ = true;
            #region Declare and implement anon function
            //TODO consider coroutines !!!
            lambdaFn = ctx.DeclareFunctionOfType(":concurrentForLoopBody", llFnTy, false);

            FBInstructionGenerator bodyGen;
            var lambdaEntry = new BasicBlock(ctx, "entry", lambdaFn);
            var loop = new BasicBlock(ctx, "loop", lambdaFn);
            var loopEnd = new BasicBlock(ctx, "loopEnd", lambdaFn);
            var bodyGen_irb = new IRBuilder(ctx);
            ctx.ResetInsertPoint(lambdaEntry, bodyGen_irb);
            if (frameInfo.numSuspendPoints > 0) {
                bodyGen = new FBInstructionGenerator(gen, ctx, lambdaFn, (FunctionType) cfl.Body.ReturnType, frameInfo.numSuspendPoints, CoroutineInfo.Kind.Async, frameInfo.localIdx, frameInfo.thisInd, frameInfo.stateInd, frameInfo.otherIdx, frameInfo.mutArgsInd, bodyGen_irb) {
                    actualCaptureType = capTy
                };
            }
            else
                bodyGen = new FBInstructionGenerator(gen, ctx, lambdaFn, (FunctionType) cfl.Body.ReturnType, bodyGen_irb) {
                    actualCaptureType = capTy
                };



            succ &= bodyGen.TryInstructionCodeGen(cfl.Arguments);
            // inner for-loop from start to start+blcksz over body
            var loopVariable = cfl.Body.Arguments[0];
            IntPtr llCounter, count, start;
            if (hasMutState) {
                llCounter = ctx.GetArgument(lambdaFn, 2);
                start = ctx.Load(llCounter, bodyGen.irb);
                var startMem = bodyGen.GetVariable(loopVariable);
                // start is only available before the first iteration (is a pointer rather than a value)
                ctx.Store(startMem, start, bodyGen.irb);
                count = ctx.GetArgument(lambdaFn, 3);
            }
            else {
                llCounter = bodyGen.GetVariable(loopVariable);
                start = ctx.GetArgument(lambdaFn, 1);
                count = ctx.GetArgument(lambdaFn, 2);

                ctx.Store(llCounter, start, bodyGen.irb);
            }
            var initCond = ctx.CompareOp(count, ctx.GetInt64(0), (sbyte) '>', false, unsignedIndices, bodyGen.irb);
            ctx.ConditionalBranch(loop, loopEnd, initCond, bodyGen.irb);
            ctx.ResetInsertPoint(loop, bodyGen.irb);

            succ &= bodyGen.TryInstructionCodeGen(cfl.Body.Body.Instruction);

            if (!ctx.CurrentBlockIsTerminated(bodyGen.irb)) {
                var i = ctx.Load(llCounter, bodyGen.irb);
                ctx.Store(llCounter, i = ctx.ArithmeticBinOp(i, ctx.GetInt64(1), (sbyte) '+', unsignedIndices, bodyGen.irb), bodyGen.irb);

                var end = ctx.ArithmeticBinOp(hasMutState ? ctx.Load(bodyGen.GetVariable(loopVariable), bodyGen.irb) : start, count, (sbyte) '+', unsignedIndices, bodyGen.irb);
                var cond = ctx.CompareOp(i, end, (sbyte) '<', false, unsignedIndices, bodyGen.irb);
                ctx.ConditionalBranch(loop, loopEnd, cond, bodyGen.irb);
            }
            ctx.ResetInsertPoint(loopEnd, bodyGen.irb);
            if (!ctx.CurrentBlockIsTerminated(bodyGen.irb)) {
                ctx.ReturnValue(ctx.True(), bodyGen.irb);
            }
            if (cfl.EnableVectorization) {
                if (!ctx.ForceVectorizationForCurrentLoop(loop, lambdaFn)) {
                    succ &= "The concurrent for-loop could not be vectorized".Report(cfl.Position, false);
                }
            }

            #endregion

            var voidPtr = ctx.GetVoidPtr();
            var ui = ctx.GetIntType();
            var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
            var sz = ctx.GetSizeOf(capTy);
            succ &= ctx.TryCast(sz, ui, ref sz, true, irb);
            var mem = ctx.GetCall(malloc, new[] { sz }, irb);
            cap = IntPtr.Zero;
            if (!ctx.TryCast(mem, ctx.GetPointerType(capTy), ref cap, false, irb)) {
                mutState = hasMutState ? ctx.GetNullPtr() : default;
                return false;
            }
            uint index = 0;

            foreach (var acc in cfl.Body.CaptureAccesses) {
                if (!cfl.Body.Arguments.Contains(acc.Variable.Original)) {
                    if (TryExpressionCodeGen(acc.Original, out var fld)) {

                        var capGEP = ctx.GetElementPtrConstIdx(cap, new[] { 0u, index }, irb);
                        ctx.Store(capGEP, fld, irb);
                    }
                    else
                        succ = false;
                }
                else {
                    succ &= gen.TryGetVariableType(acc.Variable.Original, out var argTy);
                    ctx.StoreFieldConstIdx(cap, new[] { 0u, index }, ctx.GetAllZeroValue(argTy), irb);
                }
                index++;
            }
            mutState = default;
            if (hasMutState) {
                var mutSz = ctx.GetI32SizeOf(mutStateTy);
                var mutMem = ctx.GetCall(malloc, new[] { mutSz }, irb);
                succ &= ctx.TryCast(mutMem, ctx.GetPointerType(mutStateTy), ref mutState, false, irb);
                // zero-initialized

            }
            return succ;
        }
        bool TryGetTaskContext(IExpression task, FunctionType taskFnTy, IReadOnlyCollection<ICaptureExpression> captures, IntPtr capTy, IntPtr llFnTy, out IntPtr cap, out IntPtr lambdaFn) {
            bool succ = true;

            #region Declare and implement anon function
            //TODO consider coroutines
            lambdaFn = ctx.DeclareFunctionOfType(":async", llFnTy, false);
            var bodyGen = new FBInstructionGenerator(gen, ctx, lambdaFn, taskFnTy, new IRBuilder(ctx)) {
                actualCaptureType = capTy
            };
            var lambdaEntry = new BasicBlock(ctx, "entry", lambdaFn);
            ctx.ResetInsertPoint(lambdaEntry, bodyGen.irb);
            succ &= bodyGen.TryExpressionCodeGen(task, out var ret);
            if (!ctx.CurrentBlockIsTerminated(bodyGen.irb)) {
                if (!task.ReturnType.IsVoid()) {
                    var retVal = gen.GetVoidPtrFromValue(ret, task.ReturnType, irb);
                    ctx.Store(ctx.GetArgument(lambdaFn, 1), retVal, bodyGen.irb);
                    //ctx.ReturnValue(retVal, bodyGen.irb);
                }
                ctx.ReturnValue(ctx.True(), bodyGen.irb);
            }
            #endregion

            var voidPtr = ctx.GetVoidPtr();
            var ui = ctx.GetIntType();
            var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
            var sz = ctx.GetSizeOf(capTy);
            succ &= ctx.TryCast(sz, ui, ref sz, true, irb);
            var mem = ctx.GetCall(malloc, new[] { sz }, irb);
            cap = IntPtr.Zero;
            if (!ctx.TryCast(mem, ctx.GetPointerType(capTy), ref cap, false, irb))
                return false;
            uint index = 0;

            foreach (var acc in captures) {
                if (TryExpressionCodeGen(acc.Original, out var fld)) {
                    var capGEP = ctx.GetElementPtrConstIdx(cap, new[] { 0u, index }, irb);
                    ctx.Store(capGEP, fld, irb);

                }
                else
                    succ = false;
                index++;
            }
            return succ;
        }

        bool TryLambdaCodeGen(LambdaExpression lambda, out IntPtr ret) {
            bool succ = true;
            var captures = //FBModule.StaticSemantics.GetCaptures(lambda, out var captureThis, out var captureBase);
                lambda.CaptureAccesses;
            succ &= FBModule.StaticSemantics.IsFunctional(lambda.ReturnType, out var lambdaFnTy);
            succ &= TryGetLambdaObjectType(lambdaFnTy, captures, out var capTy, out var llFnTy);
            succ &= TryGetLambdaObject(lambda, lambdaFnTy, captures, capTy, llFnTy, out var cap);

            var retTy = gen.GetGlobalInterfaceType();
            ret = ctx.DefineAlloca(fn, retTy, "lambdaTmp");
            var basePtrGEP = ctx.GetElementPtrConstIdx(ret, new[] { 0u, 0u }, irb);
            var vtablePtrGEP = ctx.GetElementPtrConstIdx(ret, new[] { 0u, 1u }, irb);
            ctx.Store(basePtrGEP, cap, irb);
            ctx.Store(vtablePtrGEP, cap, irb);
            ret = ctx.Load(ret, irb);
            return succ;
        }
        bool TryRunAsyncCodeGen(RunAsyncExpression rae, out IntPtr ret) {
            bool succ = true;
            var captures = rae.CaptureAccesses;

            succ &= TryGetTaskContextType(rae.ReturnType, captures, out var capTy, out var llFnTy);
            var args = rae.ReturnType.IsVoid() ? Array.Empty<IType>() : new[] { rae.ReturnType.AsByRef() };
            var fnTy = new FunctionType(rae.Position, rae.ReturnType.Context.Module, ":async", PrimitiveType.Bool, args, Visibility.Private);
            succ &= TryGetTaskContext(rae.Task, fnTy, captures, capTy, llFnTy, out var cap, out var lambdaFn);

            succ &= gen.TryGetType(rae.ReturnType, out var retTy);
            var asyncRetTy = (rae.ReturnType as IWrapperType).ItemType;
            var runAsyncFn = asyncRetTy.IsVoid() ? GetOrCreateInternalFunction(ctx, InternalFunction.runAsync) : GetOrCreateInternalFunction(ctx, InternalFunction.runAsyncT);
            ret = default;
            succ &= ctx.TryCast(ctx.GetCall(runAsyncFn, new[] { lambdaFn, cap }, irb), retTy, ref ret, false, irb);
            return succ;
        }
        bool TryNewObjCodeGen(NewObjectExpression newObj, out IntPtr ret) {
            bool succ = true;
            if (!newObj.ReturnType.IsValueType()) {

                var voidPtr = ctx.GetVoidPtr();
                var ui = ctx.GetIntType();
                var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);
                succ &= gen.TryGetType(newObj.ReturnType.AsValueType(), out var objTy);
                var sz = ctx.GetI32SizeOf(objTy);
                ret = ctx.GetCall(malloc, new[] { sz }, irb);
                objTy = ctx.GetPointerType(objTy);
                if (!TryCast(newObj.Position, ret, PrimitiveType.Handle, newObj.ReturnType, out ret, false))
                    return false;

                if (gen.TryGetVTable(newObj.ReturnType, out var vt, irb)) {
                    succ &= TryGepVTable(ret, newObj.ReturnType, out var vtPtr);
                    var vtVal = ctx.ForceCast(vt.VTablePointer, ctx.GetVoidPtr(), false, irb);
                    ctx.Store(vtPtr, vtVal, irb);
                }
                //else
                //    Console.WriteLine();
                var intfs = newObj.ReturnType is ClassType ctp ? ctp.AllImplementingInterfaces() : newObj.ReturnType.ImplementingInterfaces;
                foreach (var intf in intfs) {
                    succ &= TryGepInterfaceVTable(ret, newObj.ReturnType, intf, out var vtableGep);
                    succ &= TryGetInterfaceVTableForType(newObj.Position, newObj.ReturnType, intf, FBModule.StaticSemantics, out var vtable);
                    ctx.Store(vtableGep, ctx.ForceCast(vtable, ctx.GetVoidPtr(), false, irb), irb);
                }
                if (newObj.ReturnType.IsActor()) {
                    succ &= gen.TryGetQueueIndex(newObj.ReturnType, out var qInd);
                    /*var initQueue = GetOrCreateInternalFunction(ctx, InternalFunction.initializeActorQueue);
                    var qGEP = ctx.GetElementPtrConstIdx(ret, new[] { 0u, qInd }, irb);
                    ctx.GetCall(initQueue, new[] { qGEP }, irb);*/
                    var gcnewActorContext = GetOrCreateInternalFunction(ctx, InternalFunction.gcnewActorContext);
                    var actorContext = ctx.GetCall(gcnewActorContext, Array.Empty<IntPtr>(), irb);
                    ctx.StoreFieldConstIdx(ret, new[] { 0u, qInd }, actorContext, irb);
                }
                return succ & TryCallCodeGen(newObj.Position, newObj.Constructor, newObj.ReturnType, ret, newObj.Arguments, out _, false);
            }
            else {
                succ &= gen.TryGetType(newObj.ReturnType, out var objTy);
                var mem = ctx.DefineZeroinitializedAlloca(fn, objTy, "", irb, true);
                succ &= TryCallCodeGen(newObj.Position, newObj.Constructor, newObj.ReturnType.AsReferenceType(), mem, newObj.Arguments, out _, false);
                ret = ctx.Load(mem, irb);
                ctx.EndLifeTime(mem, irb);
                return succ;
            }
        }
        bool TryNewArrCodeGen(NewArrayExpression newArr, out IntPtr ret) {
            bool succ = true;
            var voidPtr = ctx.GetVoidPtr();
            var ui = ctx.GetIntType();
            var malloc = GetOrCreateInternalFunction(ctx, InternalFunction.gc_new);

            //succ &= gen.TryGetType(newArr.ReturnType, out var retTy);
            succ &= gen.TryGetVariableType((newArr.ReturnType as IWrapperType).ItemType, false, out var itemTy);

            succ &= TryExpressionCodeGen(newArr.Length, out var count);
            count = ctx.ForceCast(count, ui, true, irb);
            var byteCount = ctx.ArithmeticBinOp(count, ctx.GetI32SizeOf(itemTy), (sbyte) '*', true, irb);
            //var arrTy = ctx.GetArrayType(itemTy, 0);
            var arrTy = gen.GetArrayStructType(itemTy, 0);
            var sz = ctx.ArithmeticBinOp(byteCount, ctx.GetI32SizeOf(arrTy), (sbyte) '+', true, irb);

            var mem = ctx.GetCall(malloc, new[] { sz }, irb);
            ret = ctx.ForceCast(mem, ctx.GetPointerType(arrTy), false, irb);
            var szGEP = ctx.GetElementPtrConstIdx(ret, new[] { 0u, 0u }, irb);
            ctx.Store(szGEP, count, irb);
            return succ;
        }
        protected override bool TryGetMemoryLocation(IExpression exp, out IntPtr ret) {
            switch (exp) {
                case DeclarationExpression de: {
                    var defaultValue = de.DefaultValue;
                    var vr = de.Variable;
                    bool succ = gen.TryGetVariableType(vr, out var vrTp);
                    succ &= TryExpressionCodeGen(defaultValue, out var dflt);
                    if (defaultValue.ReturnType != de.ReturnType)
                        succ &= TryCast(defaultValue.Position, dflt, defaultValue.ReturnType, de.ReturnType, out dflt);

                    if (coro.LocalVariableIndex != null) {
                        var mem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.LocalVariableIndex[vr] }, irb);

                        ctx.Store(mem, dflt, irb);
                        ret = mem;
                    }
                    else {
                        variables[vr] = ret = ctx.DefineInitializedAlloca(fn, vrTp, dflt, vr.Signature.Name, irb, false);
                    }

                    return succ;
                }
            }
            return base.TryGetMemoryLocation(exp, out ret);
        }

        protected override bool TryExpressionCodeGenInternal(IExpression expr, out IntPtr ret) {
            //bool succ = true;
            switch (expr) {
                case NewObjectExpression newObj: {
                    return TryNewObjCodeGen(newObj, out ret);
                }
                case NewArrayExpression newArr: {
                    return TryNewArrCodeGen(newArr, out ret);
                }
                case LambdaExpression lambda: {
                    return TryLambdaCodeGen(lambda, out ret);
                }
                case CaptureAccessExpression cae: {
                    if (!gen.TryGetFieldSlot(cae.Variable, out var slot)) {
                        ret = IntPtr.Zero;
                        return $"The value {cae.Original} cannot be accessed in this lambda-context".Report(cae.Position, false);
                    }
                    var capture = ctx.ForceCast(ctx.GetArgument(fn, CoroutineInfo.ImmutableArgsIndex), ctx.GetPointerType(actualCaptureType), false, irb);
                    var capGEP = ctx.GetElementPtrConstIdx(capture, new[] { 0u, slot }, irb);
                    ret = ctx.Load(capGEP, irb);
                    return true;
                }
                case RunAsyncExpression rae: {
                    return TryRunAsyncCodeGen(rae, out ret);
                }
                case ConcurrentForLoop cfl: {
                    return TryConcurrentForLoopCodeGen(cfl, out ret);
                }
                case CompletedTaskExpression cte: {
                    return TryCompletedTaskCodeGen(cte, out ret);
                }
                case ReduceExpression re: {
                    return TryReduceExpressionCodeGen(re, out ret);
                }
                case DeclarationExpression de: {
                    return TryDeclarationExpressionCodeGen(de, out ret);
                }
            }
            return base.TryExpressionCodeGenInternal(expr, out ret);
        }

        private bool TryDeclarationExpressionCodeGen(DeclarationExpression de, out IntPtr ret) {
            var defaultValue = de.DefaultValue;
            var vr = de.Variable;
            bool succ = gen.TryGetVariableType(vr, out var vrTp);
            succ &= TryExpressionCodeGen(defaultValue, out var dflt);
            if (defaultValue.ReturnType != de.ReturnType)
                succ &= TryCast(defaultValue.Position, dflt, defaultValue.ReturnType, de.ReturnType, out dflt);

            if (coro.LocalVariableIndex != null) {
                var mem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.LocalVariableIndex[vr] }, irb);

                ctx.Store(mem, dflt, irb);
            }
            else {
                variables[vr] = ctx.DefineInitializedAlloca(fn, vrTp, dflt, vr.Signature.Name, irb, false);
            }
            ret = dflt;
            return succ;
        }

        private bool TryReduceExpressionCodeGen(ReduceExpression re, out IntPtr ret) {
            if (re.IsConcurrent) {
                return TryConcurrentReduceCodeGen(re, out ret);
            }
            bool succ = true;
            var retVarDecl = new Declaration(re.Position, re.ReturnType, Variable.Specifier.LocalVariable, new[] { "reductionResult" }, re.Seed);
            succ &= TryInstructionCodeGen(retVarDecl);
            var retVar = retVarDecl.Variables[0];

            var decl = new Declaration(re.Position, re.ItemType, Variable.Specifier.LocalVariable, new[] { "%i" });
            var iVar = decl.Variables[0];
            var iVarAccess = new VariableAccessExpression(re.Position, re.ItemType, iVar);
            var retVarAccess = new VariableAccessExpression(re.Position, re.ReturnType, retVar);
            var rhs = re.HasOperator
                ? FBSemantics.Instance.CreateBinOp(re.Position, re.ReturnType, retVarAccess, re.ReductionOperator, iVarAccess)
                : re.ReductionFunction.IsStatic() ? new CallExpression(re.Position, re.ReturnType, re.ReductionFunction, null, new[] { retVarAccess, iVarAccess }) :
                new CallExpression(re.Position, re.ReturnType, re.ReductionFunction, retVarAccess, new[] { iVarAccess });
            var body = new ExpressionStmt(re.Position,
                new BinOp(re.Position,
                re.ReturnType,
                new VariableAccessExpression(re.Position, re.ReturnType, retVar),
                BinOp.OperatorKind.ASSIGN_NEW,
                rhs
            ));
            var fe = new ForeachLoop(re.Position, decl, re.DataSource, body, Context.Immutable);
            succ = succ && TryInstructionCodeGen(fe);
            if (succ) {
                return TryExpressionCodeGen(new VariableAccessExpression(re.Position, re.ReturnType, retVar), out ret);
            }
            else {
                ret = ctx.GetNullPtr();
                return false;
            }
        }

        private bool TryConcurrentReduceCodeGen(ReduceExpression re, out IntPtr ret) {
            bool succ = true;
            IntPtr reducer;
            var retTy = (re.ReturnType as AwaitableType).ItemType;
            if (re.HasOperator)
                reducer = GetOrCreateReducer(re.ReductionOperator, retTy);
            else
                gen.TryGetMethod(re.ReductionFunction, out reducer);

            var voidPtrReducer = CreateVoidPtrReducer(reducer, re.ItemType, retTy);
            succ &= TryExpressionCodeGen(re.Seed, out var seed);
            var body = CreateReductionBody(reducer, re.ItemType, retTy, seed);
            var reduceAsyncFn = GetOrCreateInternalFunction(ctx, InternalFunction.reduceAsync);
            succ &= TryExpressionCodeGen(re.DataSource, out var dataSource);
            succ &= TryGEPFirstElement(re.DataSource.ReturnType, dataSource, out var array);
            succ &= TryGetArrayLength(re.DataSource.Position, dataSource, re.DataSource.ReturnType, out var arrLen);

            var voidPtrSeed = gen.GetVoidPtrFromValue(seed, retTy, irb);

            var task = ctx.GetCall(reduceAsyncFn, new[] { body, voidPtrReducer, array, arrLen, ctx.GetInt64(4), voidPtrSeed }, irb);
            ret = task;
            return succ;
        }

        Dictionary<(BinOp.OperatorKind, IType), IntPtr> reducerCache
            = new Dictionary<(BinOp.OperatorKind, IType), IntPtr>();
        IntPtr GetOrCreateReducer(BinOp.OperatorKind op, IType ty) {
            if (reducerCache.TryGetValue((op, ty), out var ret)) {
                return ret;
            }
            gen.TryGetType(ty, out var llty);
            ret = ctx.DeclareFunction($":{op}Reducer", llty, new[] { llty, llty }, new[] { "lhs", "rhs" }, false);
            ctx.AddFunctionAttributes(ret, new[] { "alwaysinline", "readnone" });
            IntPtr currentBlock = ctx.GetCurrentBasicBlock(irb),
                reducerEntry = new BasicBlock(ctx, "entry", ret);
            ctx.ResetInsertPoint(reducerEntry, irb);

            var lhs = ctx.GetArgument(ret, 0);
            var rhs = ctx.GetArgument(ret, 1);

            TryNumericArithmeticOrBitwiseBinOpCodeGen(lhs, rhs, op, ty.IsUnsignedNumericType(), out var retVal);
            ctx.ReturnValue(retVal, irb);

            ctx.ResetInsertPoint(currentBlock, irb);
            return ret;
        }
        IntPtr CreateVoidPtrReducer(IntPtr reducer, IType itemTy, IType reductionTy) {
            var voidPtr = ctx.GetVoidPtr();
            var ret = ctx.DeclareFunction(":Reducer", voidPtr, new[] { voidPtr, voidPtr }, new[] { "lhs", "rhs" }, false);
            ctx.AddFunctionAttributes(ret, new[] { "argmemonly" });
            IntPtr currentBlock = ctx.GetCurrentBasicBlock(irb),
                reducerEntry = new BasicBlock(ctx, "entry", ret);
            ctx.ResetInsertPoint(reducerEntry, irb);

            var lhs = gen.GetValueFromVoidPtr(ctx.GetArgument(ret, 0), reductionTy, irb);
            var rhs = gen.GetValueFromVoidPtr(ctx.GetArgument(ret, 1), itemTy, irb);

            var retVal = gen.GetVoidPtrFromValue(ctx.GetCall(reducer, new[] { lhs, rhs }, irb), reductionTy, irb);
            ctx.ReturnValue(retVal, irb);

            ctx.ResetInsertPoint(currentBlock, irb);
            return ret;
        }
        IntPtr CreateReductionBody(IntPtr reducer, IType itemType, IType reductionType, IntPtr seed) {
            gen.TryGetType(itemType, out var itemTy);
            gen.TryGetType(reductionType, out var reductionTy);
            var voidPtr = ctx.GetVoidPtr();
            var longTy = ctx.GetLongType();
            var ret = ctx.DeclareFunction(":ReductionBody", voidPtr, new[] { voidPtr, longTy, longTy }, new[] { "array", "start", "count" }, false);
            ctx.AddFunctionAttributes(ret, new[] { "argmemonly" });
            ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
            IntPtr currentBlock = ctx.GetCurrentBasicBlock(irb),
                reductionEntry = new BasicBlock(ctx, "entry", ret),
                reductionLoop = new BasicBlock(ctx, "loop", ret),
                reductionLoopEnd = new BasicBlock(ctx, "loopEnd", ret);
            ctx.ResetInsertPoint(reductionEntry, irb);

            var array = ctx.ForceCast(ctx.GetArgument(ret, 0), ctx.GetPointerType(itemTy), false, irb);
            var start = ctx.GetArgument(ret, 1);
            var count = ctx.GetArgument(ret, 2);

            //seed = gen.GetValueFromVoidPtr(seed, reductionType, irb);
            var retVar = ctx.DefineInitializedAlloca(ret, reductionTy, seed, "retVar", irb, false);
            var iVar = ctx.DefineZeroinitializedAlloca(ret, ctx.GetLongType(), "i", irb, false);
            var firstCond = ctx.CompareOp(count, ctx.GetInt64(0), (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(reductionLoop, reductionLoopEnd, firstCond, irb);
            ctx.ResetInsertPoint(reductionLoop, irb);

            var lhs = ctx.Load(retVar, irb);
            var rhs = ctx.LoadField(array, new[] { ctx.Load(iVar, irb) }, irb);
            ctx.Store(retVar, ctx.GetCall(reducer, new[] { lhs, rhs }, irb), irb);
            ctx.Store(iVar, ctx.ArithmeticBinOp(ctx.GetInt64(1), ctx.Load(iVar, irb), (sbyte) '+', true, irb), irb);

            var cond = ctx.CompareOp(ctx.Load(iVar, irb), count, (sbyte) '<', false, true, irb);
            ctx.ConditionalBranch(reductionLoop, reductionLoopEnd, cond, irb);
            ctx.ResetInsertPoint(reductionLoopEnd, irb);
            var retVal = gen.GetVoidPtrFromValue(ctx.Load(retVar, irb), reductionType, irb);
            ctx.ReturnValue(retVal, irb);

            ctx.ResetInsertPoint(currentBlock, irb);
            return ret;
        }

        private bool TryCompletedTaskCodeGen(CompletedTaskExpression cte, out IntPtr ret) {
            if (cte.Underlying is null || cte.Underlying.ReturnType.IsVoid()) {
                var completedTaskCtor = GetOrCreateInternalFunction(ctx, InternalFunction.getCompletedTask);
                ret = ctx.GetCall(completedTaskCtor, Array.Empty<IntPtr>(), irb);
                return true;
            }
            else {
                if (!TryExpressionCodeGen(cte.Underlying, out var toret)) {
                    ret = ctx.GetNullPtr();
                    return false;
                }
                var toretVoidPtr = gen.GetVoidPtrFromValue(toret, cte.Underlying.ReturnType, irb);
                var completedTaskCtor = GetOrCreateInternalFunction(ctx, InternalFunction.getCompletedTaskT);
                ret = ctx.GetCall(completedTaskCtor, new[] { toretVoidPtr }, irb);
                return true;
            }
        }

        private bool TryConcurrentForLoopCodeGen(ConcurrentForLoop cfl, out IntPtr ret) {
            if (cfl.Range is ContiguousRangeExpression cre) {
                return TryContiguousRangeBasedConcurrentForLoopCodeGen(cfl, cre, out ret);
            }
            throw new NotImplementedException();
        }

        private bool TryContiguousRangeBasedConcurrentForLoopCodeGen(ConcurrentForLoop cfl, ContiguousRangeExpression cre, out IntPtr ret) {
            // concurrentFor(uint i: start to end){...}

            // lambda-body -> bool(void*args,u64 start_i, u64 blcksz)
            // runAsync -> void*(lambda-body, void* args, u64 start, u64 count, u64 blcksz)
            bool succ = true;
            var indexTy = (cre.ReturnType.UnWrapAll() as IWrapperType).ItemType;
            succ &= gen.TryGetType(indexTy, out var llIndexTy);
            bool isUnsignedIndex = indexTy.IsUnsignedNumericType();
            IntPtr lambdaFn = default, cap = default, mutState = default;

            succ &= TryGetConcurrentForLoopBodyContextType(cfl, cfl.Body.Arguments, cfl.Body.CaptureAccesses, out var capTy, out var mutStateTy, out var hasMutState, out var llFnTy, out var frameInfo)
                 && TryGetConcurrentForLoopContext(cfl, capTy, llFnTy, mutStateTy, hasMutState, isUnsignedIndex, out cap, out mutState, out lambdaFn, frameInfo);



            succ &= TryExpressionCodeGen(cre.From, out var _from);
            succ &= TryExpressionCodeGen(cre.To, out var _to);

            if (!succ) {
                ret = ctx.GetNullPtr();
                return false;
            }
            RestoreSavedValue(cre.From, ref _from);
            RestoreSavedValue(cre.To, ref _to);

            succ &= ctx.TryCast(_from, llIndexTy, ref _from, isUnsignedIndex, irb) && ctx.TryCast(_to, llIndexTy, ref _to, isUnsignedIndex, irb);

            var count = ctx.ArithmeticBinOp(_to, ctx.GetMin(_from, _to, isUnsignedIndex, irb), (sbyte) '-', isUnsignedIndex, irb);

            succ &= gen.TryGetType(PrimitiveType.Void.AsAwaitable(), out var retTy);

            ret = default;
            IntPtr retTask;
            if (hasMutState) {
                var runLoopAsyncFn = GetOrCreateInternalFunction(ctx, InternalFunction.runLoopCoroutineAsyncOffs);
                var retGEP = ctx.GetElementPtrConstIdx(mutState, new[] { 0u, 1u }, irb);

                var offs = ctx.PtrDiff(retGEP, mutState, irb);
                retTask = ctx.GetCall(runLoopAsyncFn, new[] { lambdaFn, cap, mutState, ctx.GetI32SizeOf(mutStateTy), _from, count, ctx.GetInt64(4), offs }, irb);

            }
            else {
                var runLoopAsyncFn = GetOrCreateInternalFunction(ctx, InternalFunction.runLoopAsync);
                retTask = ctx.GetCall(runLoopAsyncFn, new[] { lambdaFn, cap, _from, count, ctx.GetInt64(4) }, irb);
            }

            succ &= ctx.TryCast(retTask, retTy, ref ret, false, irb);
            return succ;
        }

        private bool ContiguousRangeBasedForLoopCodesGen(ForeachLoop loopStmt) {

            bool succ = true;
            IntPtr[] loopVrs;
            IType[] loopVrTps;
            VariableAccessExpression[] loopVariables;
            if (loopStmt.TryGetDeclaration(out var decl)) {
                succ &= TryInstructionCodeGen(decl);
                loopVrs = (decl as Declaration).Variables.Select(x => GetVariable(x)).ToArray();
                loopVariables = (decl as Declaration).Variables.Select(x => new VariableAccessExpression(decl.Position, x.Type, x)).ToArray();
                loopVrTps = (decl as Declaration).Variables.Select(x => x.Type).ToArray();
            }
            else if (loopStmt.TryGetLoopVariables(out var vrs)) {
                loopVrs = new IntPtr[vrs.Length];
                loopVrTps = new IType[vrs.Length];
                loopVariables = new VariableAccessExpression[vrs.Length];
                for (int i = 0; i < vrs.Length; ++i) {
                    succ &= TryGetMemoryLocation(vrs[i], out var mem);
                    loopVrs[i] = mem;
                    loopVrTps[i] = vrs[i].ReturnType;
                    if (vrs[i] is VariableAccessExpression vra)
                        loopVariables[i] = vra;
                }
            }
            else {
                return false;
            }

            var cre = loopStmt.Range as ContiguousRangeExpression;

            uint fromSlot = coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(loopStmt.Range, out var fs) ? fs : 0;
            uint toSlot = fromSlot + 1;

            //TODO operator++ and operator< overloads
            var itemTp = (cre.ReturnType.UnWrap() as AggregateType).ItemType;
            if (!itemTp.IsNumericType())
                throw new NotImplementedException("Ranges for not-numeric types are currently not implemented");
            succ &= gen.TryGetType(itemTp, out var elemTy);

            BasicBlock
                bLoop = new BasicBlock(ctx, "loop", fn),
                bEndLoop = new BasicBlock(ctx, "loopEnd", fn),
                bCond = new BasicBlock(ctx, "loopCond", fn);

            succ &= TryExpressionCodeGen(cre.From, out var _from) && ctx.TryCast(_from, elemTy, ref _from, itemTp.IsUnsignedNumericType(), irb);
            succ &= TryExpressionCodeGen(cre.To, out var _to) && ctx.TryCast(_to, elemTy, ref _to, itemTp.IsUnsignedNumericType(), irb);


            // initialization
            var context = coro.OtherSaveIndex != null ? ctx.GetArgument(fn, coro.MutableArgsIndex) : ctx.DefineAlloca(fn, ctx.GetArrayType(elemTy, 2), "range");
            ctx.StoreFieldConstIdx(context, new[] { 0u, fromSlot }, _from, irb);
            ctx.StoreFieldConstIdx(context, new[] { 0u, toSlot }, _to, irb);
            //ctx.Store(loopVrs[0], _from, irb);

            // first condition-check
            var initCond = ctx.CompareOp(_from, _to, (sbyte) '<', false, itemTp.IsUnsignedNumericType(), irb);
            ctx.ConditionalBranch(bLoop, bEndLoop, initCond, irb);

            // loop body
            ctx.ResetInsertPoint(bLoop, irb);

            _from = ctx.LoadFieldConstIdx(context, new[] { 0u, fromSlot }, irb);
            StoreExpressionValue(loopVariables[0], loopVrs[0], _from);

            succ &= TryInstructionCodeGen(loopStmt.Body);

            if (!ctx.CurrentBlockIsTerminated(irb)) {
                ctx.Branch(bCond, irb);
            }
            // loop-condition
            ctx.ResetInsertPoint(bCond, irb);
            _from = ctx.LoadFieldConstIdx(context, new[] { 0u, fromSlot }, irb);
            var nwFrom = ctx.ArithmeticBinOp(_from, ctx.GetInt32(1), (sbyte) '+', itemTp.IsUnsignedNumericType(), irb);
            _to = ctx.LoadFieldConstIdx(context, new[] { 0u, toSlot }, irb);

            ctx.StoreFieldConstIdx(context, new[] { 0u, fromSlot }, nwFrom, irb);
            var cond = ctx.CompareOp(nwFrom, _to, (sbyte) '<', false, itemTp.IsUnsignedNumericType(), irb);
            ctx.ConditionalBranch(bLoop, bEndLoop, cond, irb);
            // loop end
            ctx.ResetInsertPoint(bEndLoop, irb);
            if (succ && loopStmt.EnableVectorization) {
                if (!ctx.ForceVectorizationForCurrentLoop(bLoop, fn)) {
                    succ = "The for-loop could not be vectorized".Report(loopStmt.Position, false);
                }
            }
            return succ;
        }
        public override bool TryInstructionCodeGen(IStatement stmt) {
            switch (stmt) {
                case ForeachLoop fel when fel.Range is ContiguousRangeExpression: {
                    return ContiguousRangeBasedForLoopCodesGen(fel);
                }
                case DeconstructDeclaration ddecl when ddecl.DeconstructionRange.ReturnType.IsArray() || ddecl.DeconstructionRange.ReturnType.IsArraySlice(): {
                    return DeconstructionDeclarationCodeGen(ddecl);
                }
            }
            return base.TryInstructionCodeGen(stmt);
        }

        private bool DeconstructionDeclarationCodeGen(DeconstructDeclaration ddecl) {
            bool succ = true;
            succ &= gen.TryGetType(ddecl.Type, out var varTy);
            succ &= TryRangeExpressionCodeGen(ddecl.DeconstructionRange, out var range);
            if (!succ)
                return false;
            succ &= TryGetArrayLength(ddecl.DeconstructionRange.Position, range, ddecl.DeconstructionRange.ReturnType, out var len);
            succ &= TryGEPFirstElement(ddecl.DeconstructionRange.ReturnType, range, out var arr);

            if (!succ)
                return false;

            bool isCoro = coro.LocalVariableIndex != null;

            for (uint i = 0; i < ddecl.Variables.Length; ++i) {
                var vr = ddecl.Variables[i];
                IntPtr mem;
                if (isCoro) {
                    mem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.LocalVariableIndex[vr] }, irb);
                    //ctx.Store(mem, ctx.GetAllZeroValue(varTy), irb);
                }
                else
                    variables[vr] = mem = ctx.DefineAlloca(fn, varTy, vr.Signature.Name);

                BasicBlock
                    storeElem = new BasicBlock(ctx, "storeElem", fn),
                    storeZero = new BasicBlock(ctx, "storeZero", fn),
                    storeMerge = new BasicBlock(ctx, "storeMerge", fn);

                var iLTlen = ctx.CompareOp(ctx.GetIntSZ(i), len, (sbyte) '<', false, true, irb);
                ctx.ConditionalBranch(storeElem, storeZero, iLTlen, irb);
                ctx.ResetInsertPoint(storeElem, irb);
                var elem = ctx.LoadFieldConstIdx(arr, new uint[] { i }, irb);
                //DOLATER cpy-ctor for $ types
                ctx.Store(mem, elem, irb);
                ctx.Branch(storeMerge, irb);
                ctx.ResetInsertPoint(storeZero, irb);
                ctx.Store(mem, ctx.GetAllZeroValue(varTy), irb);
                ctx.Branch(storeMerge, irb);
                ctx.ResetInsertPoint(storeMerge, irb);
            }
            return succ;
        }

        protected override bool FinalizeMethodBody() {
            if (base.FinalizeMethodBody()) {
                if (ForceLoopVectorization) {
                    ctx.ForceVectorizationForAllLoops(fn);
                }
                return true;
            }
            return false;
        }
    }
}
