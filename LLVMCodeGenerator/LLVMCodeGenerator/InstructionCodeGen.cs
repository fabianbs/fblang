/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NativeManagedContext;
using System.Collections.ObjectModel;
using CompilerInfrastructure.Semantics;

namespace LLVMCodeGenerator {
    public partial class InstructionGenerator {
        protected readonly struct RedirectTarget : IEquatable<RedirectTarget> {
            public readonly BasicBlock Start;
            public readonly BasicBlock End;

            public RedirectTarget(BasicBlock start, BasicBlock end) {
                (Start, End) = (start, end);
            }
            public RedirectTarget((BasicBlock, BasicBlock) startEnd) {
                (Start, End) = startEnd;
            }
            public void Deconstruct(out BasicBlock start, out BasicBlock end) {
                (start, end) = (Start, End);
            }

            public override bool Equals(object obj) => obj is RedirectTarget && Equals((RedirectTarget) obj);
            public bool Equals(RedirectTarget other)
                => EqualityComparer<BasicBlock>.Default.Equals(Start, other.Start) &&
                   EqualityComparer<BasicBlock>.Default.Equals(End, other.End);
            public override int GetHashCode() => System.HashCode.Combine(Start, End);

            public static bool operator ==(RedirectTarget target1, RedirectTarget target2) => target1.Equals(target2);
            public static bool operator !=(RedirectTarget target1, RedirectTarget target2) => !(target1 == target2);
            public static implicit operator RedirectTarget((BasicBlock, BasicBlock) startEnd) {
                return new RedirectTarget(startEnd);
            }
        }

        public readonly struct CoroutineInfo {
            public enum Kind {
                Iterable,
                Iterator,
                Async
            }
            public readonly uint MutableArgsIndex;
            public const uint ImmutableArgsIndex = 0;
            public readonly bool IsGeneratorCoroutine;
            public readonly bool IsAsynchronousCoroutine;
            public readonly IntPtr FallThroughTarget;
            public readonly IReadOnlyList<IntPtr> StateTargets;
            public readonly uint ThisIndex;
            public readonly uint StateIndex;
            public readonly IReadOnlyDictionary<IVariable, uint> LocalVariableIndex;
            public readonly IReadOnlyDictionary<IExpression, uint> OtherSaveIndex;

            readonly int[] currentState;

            public int CurrentState {
                get => currentState != null ? currentState[0] : 0;
                set {
                    if (value < 0 || value >= StateTargets.Count)
                        throw new ArgumentOutOfRangeException();
                    if (currentState != null)
                        currentState[0] = value;
                    throw new InvalidOperationException();
                }
            }
            public void SetCurrentState(int value) {
                if (currentState != null) {
                    currentState[0] = value;
                }
            }
            public CoroutineInfo(Kind kind, IntPtr dfltBB, IReadOnlyList<IntPtr> stateBBs, uint thisGEP, uint stateGEP, IReadOnlyDictionary<IVariable, uint> variableGEP, IReadOnlyDictionary<IExpression, uint> otherGEP, uint mutArgsInd) {
                IsGeneratorCoroutine = kind != Kind.Async;
                IsAsynchronousCoroutine = kind == Kind.Async;
                FallThroughTarget = dfltBB;
                StateTargets = stateBBs;
                currentState = new[] { 0 };
                ThisIndex = thisGEP;
                StateIndex = stateGEP;
                LocalVariableIndex = variableGEP;
                OtherSaveIndex = otherGEP;
                MutableArgsIndex = mutArgsInd;
            }
        }
        protected readonly ManagedContext ctx;
        protected readonly LLVMCodeGenerator gen;
        protected readonly Dictionary<IVariable, IntPtr> variables = new Dictionary<IVariable, IntPtr>();
        protected readonly Vector <Vector<IntPtr>> locallyDefinedVariables = new Vector<Vector<IntPtr>>();
        protected readonly Dictionary<IStatement, RedirectTarget> breakableStmts = new Dictionary<IStatement, RedirectTarget>();
        protected readonly IntPtr fn;
        protected readonly IntPtr irb;
        protected readonly FunctionType methodTp;
        protected readonly IMethod method;
        protected readonly IntPtr entryBlock;
        protected readonly CoroutineInfo coro;


        public InstructionGenerator(LLVMCodeGenerator _gen, ManagedContext _ctx, IntPtr function, FunctionType _methodTp, IntPtr irBuilder = default, IMethod _met = null) {
            gen = _gen;
            ctx = _ctx;
            fn = function;
            irb = irBuilder;
            methodTp = _methodTp;
            method = _met;
            entryBlock = ctx.GetCurrentBasicBlock(irb);
            locallyDefinedVariables.PushBack(new Vector<IntPtr>());
        }
        public InstructionGenerator(LLVMCodeGenerator _gen, ManagedContext _ctx, IntPtr function, FunctionType _methodTp, uint numSuspendPoints, CoroutineInfo.Kind coroType, IDictionary<IVariable, uint> localGEP, uint thisGEP, uint stateGEP, IDictionary<IExpression, uint> otherGEP, uint mutArgsInd, IntPtr irBuilder = default)
            : this(_gen, _ctx, function, _methodTp, irBuilder) {

            var stateBBs = new IntPtr[numSuspendPoints + 1];
            for (uint i = 0; i <= numSuspendPoints; ++i) {
                stateBBs[i] = new BasicBlock(_ctx, "resumePoint" + i, function);
            }
            coro = new CoroutineInfo(coroType, new BasicBlock(_ctx, "fallThrough", function), new ReadOnlyCollection<IntPtr>(stateBBs), thisGEP, stateGEP, new ReadOnlyDictionary<IVariable, uint>(localGEP), new ReadOnlyDictionary<IExpression, uint>(otherGEP), mutArgsInd);
            InitializeCoroutine();

        }
        public InstructionGenerator(LLVMCodeGenerator _gen, ManagedContext ctx, IntPtr function, in CoroutineFrameInfo frameInfo, IntPtr irBuilder)
            : this(_gen, ctx, function, frameInfo.coroTp, frameInfo.numSuspendPoints, frameInfo.kind, frameInfo.localIdx, frameInfo.thisInd, frameInfo.stateInd, frameInfo.otherIdx, frameInfo.mutArgsInd, irBuilder) {

        }
        public virtual void InitializeCoroutine() {

            //assume a signature: coro_frame x T1& x T1& x ... x Tn& -> bool; for 0 <= n < short.MaxValue
            #region entry
            var stateGEP = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.StateIndex }, irb);
            var state = ctx.Load(stateGEP, irb);
            ctx.IntegerSwitch(state, coro.FallThroughTarget, coro.StateTargets.ToArray(), Enumerable.Range(0, coro.StateTargets.Count).Select(x => ctx.GetInt32((uint) x)).ToArray(), irb);
            ctx.ResetInsertPoint(coro.FallThroughTarget, irb);
            #endregion

            #region fallThrough
            if (coro.IsGeneratorCoroutine) {

                uint i = 1;
                foreach (var arg in methodTp.ArgumentTypes.Skip(1)) {

                    if (arg.IsByRef()) {
                        gen.TryGetType(arg.UnWrap(), out var retTy);
                        var nullVal = ctx.GetAllZeroValue(retTy);
                        var llArg = ctx.GetArgument(fn, i);
                        ctx.Store(llArg, nullVal, irb);
                    }

                    i++;
                }
                ctx.ReturnValue(ctx.False(), irb);
            }
            else if (coro.IsAsynchronousCoroutine) {
                ctx.ReturnValue(ctx.True(), irb);
            }
            #endregion
            ctx.ResetInsertPoint(coro.StateTargets[0], irb);

        }
        public bool DoBoundsChecks { get; set; } = true;
        public bool DoNullChecks { get; set; } = true;
        public bool AddVariable(IVariable vr, IntPtr vrmem) {
            locallyDefinedVariables.Back().PushBack(vrmem);
            return variables.TryAdd(vr, vrmem);
        }
        bool TryGetVariable(IVariable vr, IntPtr parent, out IntPtr vrmem) {
            if (variables.TryGetValue(vr, out vrmem))
                return true;
            if (gen.staticVariables.TryGetValue(vr, out vrmem))
                return true;
            if (coro.LocalVariableIndex != null && coro.LocalVariableIndex.TryGetValue(vr, out var ind)) {
                vrmem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, ind }, irb);
                return true;
            }
            if (gen.instanceFields.TryGetValue(vr, out var index)) {
                if (parent == default)
                    parent = ctx.GetArgument(fn, 0);
                vrmem = ctx.GetElementPtrConstIdx(parent, new[] { 0u, index }, irb);
                return true;
            }

            return false;
        }
        public IntPtr GetVariable(IVariable vr, IntPtr parent = default) {
            TryGetVariable(vr, parent, out var ret);
            return ret;
        }
        public virtual bool TryInstructionCodeGen(IStatement stmt) {
            bool succ = true;
            switch (stmt) {
                case null: {
                    return false;
                }
                case ReturnStatement retStmt: {
                    return TryReturnCodeGen(retStmt);
                }
                case BlockStatement block: {
                    locallyDefinedVariables.PushBack(default);
                    foreach (var nested in block.Statements) {
                        succ &= TryInstructionCodeGen(nested);
                        if (ctx.CurrentBlockIsTerminated(irb))
                            break;
                    }
                    if (!ctx.CurrentBlockIsTerminated(irb)) {
                        foreach (var alloca in locallyDefinedVariables.Back()) {
                            ctx.EndLifeTime(alloca, irb);
                        }
                    }
                    locallyDefinedVariables.PopBack();
                    return succ;

                }
                case InstructionBox box: {
                    if (box.HasValue) {
                        return TryInstructionCodeGen(box.Instruction);
                    }
                    break;
                }
                case ExpressionStmt exp: {
                    return TryExpressionCodeGen(exp.Expression, out _);
                }
                case IfStatement ite: {
                    succ &= TryExpressionCodeGen(ite.Condition, out var cond);
                    var thenBlock = new BasicBlock(ctx.Instance, "then", fn);
                    var endBlock = new BasicBlock(ctx.Instance, "endIf", fn);
                    var elseBlock = ite.ElseStatement != null
                        ? new BasicBlock(ctx.Instance, "else", fn)
                        : endBlock;
                    ctx.ConditionalBranch(thenBlock, elseBlock, cond, irb);
                    ctx.ResetInsertPoint(thenBlock, irb);
                    succ &= TryInstructionCodeGen(ite.ThenStatement);
                    if (!ctx.CurrentBlockIsTerminated(irb)) {
                        ctx.Branch(endBlock, irb);
                    }
                    if (ite.ElseStatement != null) {
                        ctx.ResetInsertPoint(elseBlock, irb);
                        succ &= TryInstructionCodeGen(ite.ElseStatement);
                        if (!ctx.CurrentBlockIsTerminated(irb)) {
                            ctx.Branch(endBlock, irb);
                        }
                    }
                    ctx.ResetInsertPoint(endBlock, irb);
                    return succ;
                }
                case WhileLoop wloop: {
                    return TryLoopCodeGen(wloop, wloop.Condition, wloop.Body, wloop.IsHeadControlled);
                }
                case ForLoop floop: {
                    if (floop.Initialization != null)
                        succ &= TryInstructionCodeGen(floop.Initialization);
                    if (floop.Increment != null)
                        return succ & TryForLoopCodeGen(floop, floop.Condition ?? Literal.True, floop.Body, floop.Increment);
                    else
                        return succ & TryLoopCodeGen(floop, floop.Condition, floop.Body, true);
                }
                case Declaration decl: {
                    IExpression defaultValue = null;
                    IntPtr dflt = IntPtr.Zero;
                    // Note: deconstructionDecl and randomInitDecl will be implemented in subclass which overrides this method
                    return decl.Variables.All(vr => {
                        bool ret = gen.TryGetVariableType(vr, out var vrTp);

                        bool isCoro = coro.LocalVariableIndex != null;
                        if (vr.DefaultValue != null) {
                            if (vr.DefaultValue != defaultValue) {
                                defaultValue = vr.DefaultValue;
                                ret &= TryExpressionCodeGen(defaultValue, out dflt);
                                RestoreSavedValue(defaultValue, ref dflt);
                                if (defaultValue.ReturnType != decl.Type) {
                                    ret &= TryCast(defaultValue.Position, dflt, defaultValue.ReturnType, decl.Type, out dflt);
                                }
                            }
                            if (isCoro) {
                                var mem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.LocalVariableIndex[vr] }, irb);

                                ctx.Store(mem, dflt, irb);
                            }
                            else
                                AddVariable(vr, ctx.DefineInitializedAlloca(fn, vrTp, dflt, vr.Signature.Name, irb, true));
                        }
                        else {
                            defaultValue = null;
                            if (isCoro) {
                                var mem = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.LocalVariableIndex[vr] }, irb);
                                ctx.Store(mem, ctx.GetAllZeroValue(vrTp), irb);
                            }
                            else
                                AddVariable(vr, ctx.DefineZeroinitializedAlloca(fn, vrTp, vr.Signature.Name, irb, true));
                        }

                        return ret;
                    });
                }
                case BreakStatement br: {
                    if (breakableStmts.TryGetValue(br.Target, out var tar)) {
                        ctx.Branch(tar.End, irb);
                        return true;
                    }
                    else {
                        return "The underlying context is no loop or switch-statement and therefore cannot be exited with a break-statement".Report(br.Position, false);
                    }
                }
                case ContinueStatement cont: {
                    if (breakableStmts.TryGetValue(cont.Target, out var tar)) {
                        if (tar.Start != IntPtr.Zero) {
                            ctx.Branch(tar.Start, irb);
                            return true;
                        }
                        else {
                            return "A switch-statement cannot be continued".Report(cont.Position, false);
                        }
                    }
                    else {
                        return "The underlying context is no loop and therefore cannot be continued with a continue-statement".Report(cont.Position, false);
                    }
                }
                case ForeachLoop rbfloop: {
                    IntPtr[] loopVrs;
                    IType[] loopVrTps;
                    VariableAccessExpression[] loopVariables;
                    if (rbfloop.TryGetDeclaration(out var decl)) {
                        succ &= TryInstructionCodeGen(decl);
                        loopVrs = (decl as Declaration).Variables.Select(x => GetVariable(x)).ToArray();
                        loopVariables = (decl as Declaration).Variables.Select(x => new VariableAccessExpression(decl.Position, x.Type, x)).ToArray();
                        loopVrTps = (decl as Declaration).Variables.Select(x => x.Type).ToArray();
                    }
                    else if (rbfloop.TryGetLoopVariables(out var vrs)) {
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
                        succ = false;
                        loopVrs = null;
                        loopVrTps = null;
                        loopVariables = null;
                    }
                    // dont enter rbfloopCodeGen when the loopVariables could not be determined
                    return succ && RangeBasedForlLoopCodeGen(rbfloop, loopVrs, loopVrTps, loopVariables, rbfloop.Range, rbfloop.Body);
                }
                case NopStatement nop: {
                    return true;
                }
                case Deconstruction dec: {
                    return TryDeconstructionCodeGen(dec);
                }
                case YieldStatement yst: {
                    return TryYieldCodeGen(yst);
                }
                case SwitchStatement swst: {
                    if (swst.Condition.ReturnType.IsIntegerType())
                        return TryIntegerSwitchCodeGen(swst);
                    return TrySwitchCodeGen(swst);
                }
                //TODO stmt
            }

            throw new NotImplementedException();
        }

        private bool TryIntegerSwitchCodeGen(SwitchStatement swst) {
            bool succ = TryExpressionCodeGen(swst.Condition, out var cond);
            if (!succ)
                cond = ctx.GetInt32(0);
            var endSwitch = new BasicBlock(ctx, "switchEnd", fn);
            breakableStmts[swst] = new RedirectTarget(null, endSwitch);
            var cases = Vector<IntPtr>.Reserve((uint) swst.Cases.Length);
            var caseAST = Vector<SwitchStatement.Case>.Reserve((uint) swst.Cases.Length);
            var caseConds = Vector<IntPtr>.Reserve((uint) swst.Cases.Length);
            IntPtr defaultTarget = endSwitch;
            SwitchStatement.Case dfltCase = null;
            bool hasDefault = false;
            foreach (var cas in swst.Cases) {
                if (cas.IsDefault) {
                    defaultTarget = new BasicBlock(ctx, "defaultCase", fn);
                    dfltCase = cas;
                    hasDefault = true;
                }
                else {
                    var bb = new BasicBlock(ctx, "case", fn);
                    foreach (var alt in cas.Patterns) {
                        if (alt is ILiteral lit) {
                            succ &= TryExpressionCodeGen(lit, out var litVal);
                            cases.Add(bb);
                            caseConds.Add(litVal);
                            caseAST.Add(cas);
                        }
                        else {
                            //TODO alt.Position
                            "The switch-statement on integers only supports literals as case-patterns".Report(cas.Position);
                        }
                    }
                }
            }
            ctx.IntegerSwitch(cond, defaultTarget, cases.AsArray(), caseConds.AsArray(), irb);
            if (hasDefault) {
                ctx.ResetInsertPoint(defaultTarget, irb);
                succ &= TryInstructionCodeGen(dfltCase.OnMatch);
                if (!ctx.CurrentBlockIsTerminated(irb))
                    ctx.Branch(endSwitch, irb);

            }

            foreach (var (ast, cas) in caseAST.Distinct().Zip(cases.Distinct())) {
                ctx.ResetInsertPoint(cas, irb);
                succ &= TryInstructionCodeGen(ast.OnMatch);
                if (!ctx.CurrentBlockIsTerminated(irb))
                    ctx.Branch(endSwitch, irb);
            }

            ctx.ResetInsertPoint(endSwitch, irb);
            return succ;
        }

        private bool TrySwitchCodeGen(SwitchStatement swst) {

            bool succ = TryExpressionCodeGen(swst.Condition, out var cond);
            if (!succ)
                cond = ctx.GetNullPtr();
            var endSwitch = new BasicBlock(ctx, "switchEnd", fn);
            breakableStmts[swst] = new RedirectTarget(null, endSwitch);
            foreach (var cas in swst.Cases) {
                if (!MatchCaseCodeGen(cas, swst.Condition.ReturnType, cond, out var matches)) {
                    succ = false;
                    continue;
                }
                BasicBlock
                    doMatch = new BasicBlock(ctx, "match", fn),
                    doNotMatch = new BasicBlock(ctx, "notMatch", fn);
                ctx.ConditionalBranch(doMatch, doNotMatch, matches, irb);
                ctx.ResetInsertPoint(doMatch, irb);
                succ &= TryInstructionCodeGen(cas.OnMatch);
                if (!ctx.CurrentBlockIsTerminated(irb))
                    ctx.Branch(endSwitch, irb);
                ctx.ResetInsertPoint(doNotMatch, irb);
            }

            ctx.Branch(endSwitch, irb);
            ctx.ResetInsertPoint(endSwitch, irb);
            return succ;
        }
        bool MatchRecursivePattern(SwitchStatement.RecursivePattern pat, IType condTy, IntPtr cond, out IntPtr ret) {
            var succ = true;
            var endRecPatMatch = new BasicBlock(ctx, "endRecursivePatternMatch", fn);
            var curBB = ctx.GetCurrentBasicBlock(irb);
            ctx.ResetInsertPoint(endRecPatMatch, irb);
            var toret = new PHINode(ctx, ctx.GetBoolType(), (uint) pat.SubPatterns.Length + (pat.TypecheckNecessary || !condTy.IsNotNullable() ? 2u : 1u), irb);
            ctx.ResetInsertPoint(curBB, irb);
            if (pat.TypecheckNecessary) {
                var inst = IsInstanceOf(cond, condTy, pat.BaseType, false);
                toret.AddMergePoint(ctx.False(), curBB);
                var nwBB = new BasicBlock(ctx, "isInstanceOf." + pat.BaseType.Signature, fn);
                ctx.ConditionalBranch(nwBB, endRecPatMatch, inst, irb);
                ctx.ResetInsertPoint(nwBB, irb);
            }
            else if (!condTy.IsNotNullable()) {
                var nn = ctx.IsNotNull(cond, irb);
                toret.AddMergePoint(ctx.False(), curBB);
                var nwBB = new BasicBlock(ctx, "isNotNull." + pat.BaseType.Signature, fn);
                ctx.ConditionalBranch(nwBB, endRecPatMatch, nn, irb);
                ctx.ResetInsertPoint(nwBB, irb);
            }
            if (condTy != pat.BaseType) {
                succ &= TryCast(pat.Position, cond, condTy, pat.BaseType, out cond, throwOnError: false);
            }
            var allocas = //new Vector<IntPtr>((uint) pat.SubPatterns.Length);
                Vector<IntPtr>.Reserve((uint) pat.SubPatterns.Length);
            foreach (var sub in pat.SubPatterns) {
                succ &= gen.TryGetType(sub.BaseType, out var ty);
                allocas.Add(ctx.DefineZeroinitializedAlloca(fn, ty, "", irb, false));
            }
            bool isVirt = pat.BaseType.CanBeInherited() && (pat.Deconstructor.IsVirtual() || pat.Deconstructor.IsOverride());
            // static semantics has already asserted, that all deconstructor arguments are mutable lvalue-references
            if (!TryCallCodeGen(pat.Position, pat.Deconstructor, pat.BaseType, cond, allocas.ToArray(), out _, isVirt)) {
                ret = ctx.GetNullPtr();
                return false;
            }
            {
                uint i = 0;
                foreach (var sub in pat.SubPatterns) {
                    var subCond = ctx.Load(allocas[i], irb);
                    succ &= MatchCasePatternCodeGen(sub, BasicSemantics.ArgumentType(pat.Deconstructor, out _, i), subCond, out var eq);
                    var nwBB = new BasicBlock(ctx, "matchesArg" + i, fn);
                    ctx.ConditionalBranch(nwBB, endRecPatMatch, eq, irb);
                    toret.AddMergePoint(ctx.False(), ctx.GetCurrentBasicBlock(irb));
                    ctx.ResetInsertPoint(nwBB, irb);
                    ++i;
                }
                ctx.Branch(endRecPatMatch, irb);
                toret.AddMergePoint(ctx.True(), ctx.GetCurrentBasicBlock(irb));
                ctx.ResetInsertPoint(endRecPatMatch, irb);
            }
            ret = toret;
            return succ;
        }
        bool MatchCasePatternCodeGen(SwitchStatement.IPattern pat, IType condTy, IntPtr cond, out IntPtr eq) {
            eq = ctx.False();
            var succ = true;
            switch (pat) {
                case SwitchStatement.RecursivePattern rec:
                    succ &= MatchRecursivePattern(rec, condTy, cond, out eq);
                    break;
                case ILiteral _:
                case VariableAccessExpression _:
                    var lit = pat as IExpression;
                    //TODO comparison overloads
                    if (!TryExpressionCodeGen(lit, out var litVal)) {
                        litVal = ctx.GetNullPtr();
                        succ = false;
                    }

                    if (lit.ReturnType.IsString()) {
                        TryStringComparisonCodeGen(litVal, cond, out eq);
                    }
                    else {
                        eq = ctx.CompareOp(litVal, cond, (sbyte) '!', true, lit.ReturnType.IsUnsignedNumericType(), irb);
                    }
                    break;
                case SwitchStatement.Wildcard wc:
                    eq = ctx.True();
                    break;
                case DeclarationExpression decl:
                    succ &= MatchDeclCodeGen(decl, condTy, cond, out eq);
                    break;
            }
            return succ;
        }
        bool MatchDeclCodeGen(DeclarationExpression decl, IType condTy, IntPtr cond, out IntPtr eq) {
            if (!gen.TryGetType(decl.ReturnType, out var declTy)) {
                eq = ctx.GetNullPtr();
                return false;
            }
            var succ = true;
            var vr = ctx.DefineAlloca(fn, declTy, decl.Variable.Signature.Name);
            variables[decl.Variable] = vr;
            var endBB = new BasicBlock(ctx, "endDeclMatch", fn);
            var currBB = ctx.GetCurrentBasicBlock(irb);
            ctx.ResetInsertPoint(endBB, irb);
            var toret = new PHINode(ctx, ctx.GetBoolType(), 1u + (decl.TypecheckNecessary ? 1u : 0u), irb);
            ctx.ResetInsertPoint(currBB, irb);
            if (decl.TypecheckNecessary) {
                var inst = IsInstanceOf(cond, condTy, decl.ReturnType, !decl.ReturnType.IsNotNullable());
                var nwBB = new BasicBlock(ctx, "declMatch", fn);
                ctx.ConditionalBranch(nwBB, endBB, inst, irb);
                toret.AddMergePoint(ctx.False(), currBB);
                ctx.ResetInsertPoint(nwBB, irb);
            }
            if (decl.BaseType != condTy)
                succ &= TryCast(decl.Position, cond, condTy, decl.ReturnType, out cond, throwOnError: false);
            ctx.Store(vr, cond, irb);
            ctx.Branch(endBB, irb);
            toret.AddMergePoint(ctx.True(), ctx.GetCurrentBasicBlock(irb));
            ctx.ResetInsertPoint(endBB, irb);
            eq = toret;
            return succ;
        }
        private bool MatchCaseCodeGen(SwitchStatement.Case cas, IType condTy, IntPtr cond, out IntPtr ret) {
            if (cas.IsDefault) {
                ret = ctx.True();
                return true;
            }
            else {
                bool succ = true;
                // in astfourthpass made sure, that only the intersection of defined variables is visible in the onMatch-block
                var endMatch = new BasicBlock(ctx, "endMatch", fn);
                var currBB = ctx.GetCurrentBasicBlock(irb);
                ctx.ResetInsertPoint(endMatch, irb);
                var toret = new PHINode(ctx, ctx.GetBoolType(), (uint) cas.Patterns.Length + 1, irb);
                ctx.ResetInsertPoint(currBB, irb);
                foreach (var pat in cas.Patterns) {
                    succ &= MatchCasePatternCodeGen(pat, condTy, cond, out var eq);
                    var nwBB = new BasicBlock(ctx, "matcher", fn);
                    toret.AddMergePoint(ctx.True(), ctx.GetCurrentBasicBlock(irb));
                    ctx.ConditionalBranch(endMatch, nwBB, eq, irb);
                    ctx.ResetInsertPoint(nwBB, irb);
                }
                toret.AddMergePoint(ctx.False(), ctx.GetCurrentBasicBlock(irb));
                ctx.Branch(endMatch, irb);
                ctx.ResetInsertPoint(endMatch, irb);
                ret = toret;
                return succ;
            }
        }

        private bool TryReturnCodeGen(ReturnStatement retStmt) {
            bool succ = true;
            if (retStmt.HasReturnValue) {
                IntPtr retVal;
                if (retStmt.ReturnValue is DefaultValueExpression dflt) {
                    if (!methodTp.ReturnType.IsVoid())
                        succ &= TryExpressionCodeGen(DefaultValueExpression.Get(methodTp.ReturnType), out retVal);
                    else {
                        retVal = default;
                        succ = $"The void-method {methodTp.Signature} must not return a value".Report(dflt.Position, false);
                    }
                }
                else
                    succ &= TryExpressionCodeGen(retStmt.ReturnValue, out retVal);
                if (succ) {
                    if (coro.IsAsynchronousCoroutine) {
                        retVal = gen.GetVoidPtrFromValue(retVal, retStmt.ReturnValue.ReturnType, irb);
                        ctx.Store(ctx.GetArgument(fn, 1), retVal, irb);
                        ctx.ReturnValue(ctx.True(), irb);
                    }
                    else {
                        ctx.ReturnValue(retVal, irb);
                    }
                }
            }
            else {
                if (coro.IsAsynchronousCoroutine)
                    ctx.ReturnValue(ctx.True(), irb);
                else
                    ctx.ReturnVoid(irb);
            }
            return succ;
        }
        private bool TryYieldCodeGen(YieldStatement yst) {
            if (!coro.IsGeneratorCoroutine) {
                string hint = coro.IsAsynchronousCoroutine
                    ? "Try to remove the 'async'-keyword and make sure, that the method is returning an iterator or an iterable value"
                    : "Make sure, that the method is returning an iterator or an iterable value";
                return $"A yield-statement is only valid in a generator-coroutine. {hint}".Report(yst.Position, false);
            }
            bool succ = true;
            if (yst.HasReturnValue) {

                if (methodTp.ArgumentTypes.Count == 0 || !methodTp.ArgumentTypes.First().IsByRef()) {
                    succ = "The generator-coroutine must not yield any value, since it has no parameter at the second position which is passed by reference".Report(yst.Position, false);
                }
                succ &= TryExpressionCodeGen(yst.ReturnValue, out var ret);
                succ = succ && TryCast(yst.ReturnValue.Position, ret, yst.ReturnValue.ReturnType, methodTp.ArgumentTypes.First().UnWrap(), out ret);
                if (succ)
                    ctx.Store(ctx.GetArgument(fn, 1), ret, irb);
            }
            coro.SetCurrentState(coro.CurrentState + 1);
            var stateGEP = ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, coro.StateIndex }, irb);
            ctx.Store(stateGEP, ctx.GetInt32((uint) coro.CurrentState), irb);
            ctx.ReturnValue(ctx.True(), irb);
            if (coro.CurrentState < coro.StateTargets.Count) {
                ctx.ResetInsertPoint(coro.StateTargets[coro.CurrentState], irb);
            }
            return succ;
        }

        protected bool TryGEPNthElement(IType rangeTy, IntPtr range, IntPtr n, out IntPtr gep) {
            if (rangeTy.IsArray()) {
                gep = ctx.GetElementPtr(range, new[] { ctx.GetInt32(0), ctx.GetInt32(1), n }, irb);
            }
            else if (rangeTy.IsArraySlice()) {
                gep = ctx.ExtractValue(range, 0, irb);
                gep = ctx.GetElementPtr(gep, new[] { n }, irb);
            }
            else if (rangeTy.IsVarArg()) {
                //TODO gep nth element of vararg
                throw new NotImplementedException();
            }
            else {
                gep = default;
                return false;
            }
            return true;
        }
        protected bool TryGEPFirstElement(IType rangeTy, IntPtr range, out IntPtr gep) {
            if (rangeTy.IsArray()) {
                gep = ctx.GetElementPtrConstIdx(range, new[] { 0u, 1u, 0u }, irb);
            }
            else if (rangeTy.IsArraySlice()) {
                gep = ctx.ExtractValue(range, 0, irb);
            }
            else if (rangeTy.IsVarArg()) {
                //TODO gep first element of vararg
                throw new NotImplementedException();
            }
            else {
                gep = default;
                return false;
            }
            return true;
        }
        bool TryCopyRange(IType destTy, IntPtr dest, IntPtr destOffs, IType srcTy, IntPtr src, IntPtr srcOffs, IntPtr count, bool cpy = false) {
            // copy range
            if (!TryGEPNthElement(destTy, dest, destOffs, out var destRange) || !TryGEPNthElement(srcTy, src, srcOffs, out var srcRange))
                return false;
            if (!gen.TryGetType((destTy as IWrapperType).ItemType, out var elemTy))
                return false;
            var elemSz = ctx.GetI32SizeOf(elemTy);
            count = ctx.ArithmeticBinOp(count, elemSz, (sbyte) '*', true, irb);
            if (cpy)
                ctx.MemoryCopy(destRange, srcRange, count, false, irb);
            else
                ctx.MemoryMove(destRange, srcRange, count, false, irb);
            return true;
        }
        bool TryCopyVarArg(IType destTy, IntPtr dest, IntPtr destOffs, VarArgType srcTy, IntPtr src, IntPtr srcOffs, IntPtr count, bool cpy = false) {
            if (!gen.TryGetType(srcTy.ItemType, out var itemTy))
                return false;

            // loop over all spans

            // NOTE: Follow structure of CopyVarArg.c
            //FIXME: There was a bug in CopyVarArg.c => in the loop the spanIndex++ was missing => fix it here too

            var intTy = ctx.GetIntType();
            var zero = ctx.GetInt32(0);
            var one = ctx.GetInt32(1);
            IntPtr
                b5 = ctx.GetCurrentBasicBlock(irb),
                b11 = new BasicBlock(ctx, "", fn),
                b15 = new BasicBlock(ctx, "", fn),
                b24 = new BasicBlock(ctx, "", fn),
                b27 = new BasicBlock(ctx, "", fn),
                b31 = new BasicBlock(ctx, "", fn),
                b37 = new BasicBlock(ctx, "", fn),
                b57 = new BasicBlock(ctx, "", fn);
            #region 5
            var totalOffset = ctx.ExtractValue(src, 3, irb);
            totalOffset = ctx.ArithmeticBinOp(totalOffset, srcOffs, (sbyte) '+', true, irb);
            var spans = ctx.ExtractValue(src, 0, irb);


            var cmp1 = ctx.CompareOp(totalOffset, zero, (sbyte) '!', true, true, irb);
            ctx.ConditionalBranch(b27, b11, cmp1, irb);

            #endregion
            #region 11
            ctx.ResetInsertPoint(b11, irb);
            var numSpans = ctx.ExtractValue(src, 1, irb);
            var cmp2 = ctx.CompareOp(numSpans, zero, (sbyte) '!', true, true, irb);
            ctx.ConditionalBranch(b27, b15, cmp2, irb);

            #endregion
            #region 15
            ctx.ResetInsertPoint(b15, irb);
            var spanIndex = new PHINode(ctx, intTy, 2, irb);
            spanIndex.AddMergePoint(zero, b11);
            // mergepoint nwSpanIndex, b24
            var actualIndex = new PHINode(ctx, intTy, 2, irb);
            actualIndex.AddMergePoint(zero, b11);
            // mergepoint nwActualIndex, b24

            var spanLength = ctx.LoadField(spans, new[] { spanIndex, one }, irb);
            var nwActualIndex = ctx.ArithmeticBinOp(spanLength, actualIndex, (sbyte) '+', true, irb);
            actualIndex.AddMergePoint(nwActualIndex, b24);
            var cmp3 = ctx.CompareOp(nwActualIndex, totalOffset, (sbyte) '<', false, true, irb);
            ctx.ConditionalBranch(b24, b27, cmp3, irb);
            #endregion
            #region 24
            ctx.ResetInsertPoint(b24, irb);
            var nwSpanIndex = ctx.ArithmeticBinOp(spanIndex, one, (sbyte) '+', true, irb);
            spanIndex.AddMergePoint(nwSpanIndex, b24);
            var cmp4 = ctx.CompareOp(nwSpanIndex, numSpans, (sbyte) '<', false, true, irb);
            ctx.ConditionalBranch(b15, b27, cmp4, irb);
            #endregion
            #region 27
            ctx.ResetInsertPoint(b27, irb);
            var spanIdx = new PHINode(ctx, intTy, 4, irb); // %28
            spanIdx.AddMergePoint(zero, b5);
            spanIdx.AddMergePoint(zero, b11);
            spanIdx.AddMergePoint(spanIndex, b15);
            spanIdx.AddMergePoint(nwSpanIndex, b24);

            var actIdx = new PHINode(ctx, intTy, 4, irb); // %29
            actIdx.AddMergePoint(zero, b5);
            actIdx.AddMergePoint(zero, b11);
            actIdx.AddMergePoint(actualIndex, b15);
            actIdx.AddMergePoint(nwActualIndex, b24);

            var cmp5 = ctx.CompareOp(count, zero, (sbyte) '!', true, true, irb);
            ctx.ConditionalBranch(b57, b31, cmp5, irb);
            #endregion
            #region 31
            ctx.ResetInsertPoint(b31, irb);
            var cmp6 = ctx.CompareOp(totalOffset, actIdx, (sbyte) '>', false, true, irb);
            var totalOffsetOrActIdx = ctx.ConditionalSelect(totalOffset, actIdx, cmp6, irb); // %33
            var localOffset = ctx.ArithmeticBinOp(totalOffsetOrActIdx, actIdx, (sbyte) '-', true, irb);
            ctx.Branch(b37, irb);
            #endregion
            #region 37
            ctx.ResetInsertPoint(b37, irb);
            var n = new PHINode(ctx, intTy, 2, irb); // %38
            n.AddMergePoint(count, b31);
            // mergepoint nwCount %55, b37
            var offset = new PHINode(ctx, intTy, 2, irb); // %39
            offset.AddMergePoint(localOffset, b31);
            offset.AddMergePoint(zero, b37);
            var destOffset = new PHINode(ctx, intTy, 2, irb); // %40
            destOffset.AddMergePoint(destOffs, b31);
            // mergepoint nwDestOffset %54, b37

            var actualSpanLen = ctx.LoadField(spans, new[] { spanIdx, one }, irb);// %43
            var localCount_1 = ctx.ArithmeticBinOp(actualSpanLen, offset, (sbyte) '-', true, irb); // %44
            var cmp7 = ctx.CompareOp(n, localCount_1, (sbyte) '<', false, true, irb);
            var localCount = ctx.ConditionalSelect(n, localCount_1, cmp7, irb); // %46

            if (!TryGEPNthElement(destTy, dest, destOffset, out var destPtr)) // %48
                return false;
            var srcPtr_1 = ctx.LoadField(spans, new[] { spanIdx, zero }, irb); // %50
            var srcPtr = ctx.GetElementPtr(srcPtr_1, new IntPtr[] { offset }, irb); // %52
            var byteCount = ctx.ArithmeticBinOp(localCount, ctx.GetI32SizeOf(itemTy), (sbyte) '*', true, irb);
            if (cpy)
                ctx.MemoryCopy(destPtr, srcPtr, byteCount, false, irb);
            else
                ctx.MemoryMove(destPtr, srcPtr, byteCount, false, irb);
            var nwDestOffset = ctx.ArithmeticBinOp(destOffset, localCount, (sbyte) '+', true, irb);
            var nwCount = ctx.ArithmeticBinOp(n, localCount, (sbyte) '-', true, irb);
            n.AddMergePoint(nwCount, b37);
            destOffset.AddMergePoint(nwDestOffset, b37);
            var cmp8 = ctx.CompareOp(nwCount, zero, (sbyte) '!', true, true, irb);
            ctx.ConditionalBranch(b57, b37, cmp8, irb);
            #endregion
            ctx.ResetInsertPoint(b57, irb);
            return true;
        }

        private bool TryDeconstructionCodeGen(Deconstruction dec) {
            //DOLATER consider copy-constructors for $-types
            bool succ = true;

            var rangeTp = dec.Range.ReturnType.IsFixedSizedArray()
                ? (dec.Range.ReturnType as IWrapperType).ItemType.AsArray()
                : dec.Range.ReturnType;

            succ &= //rangeTp.IsArray()? TryRefExpressionCodeGen(dec.Range, out var range): TryExpressionCodeGen(dec.Range, out range);
                TryRangeExpressionCodeGen(dec.Range, out var range);

            var bbEnd = new BasicBlock(ctx, "deconstruction_end", fn);

            succ &= TryGetArrayLength(dec.Range.Position, range, rangeTp, out var len);
            var elemTp = (rangeTp as IWrapperType).ItemType;
            if (!succ) {
                return false;
            }
            var one = ctx.GetInt32(1);
            var zero = ctx.GetInt32(0);
            var index = ctx.DefineZeroinitializedAlloca(fn, ctx.GetSizeTType(), ":index", irb, true);

            foreach (var dest in dec.Destination) {
                var currBB = new BasicBlock(ctx, $"{dest}_destination", fn);
                //range-check
                var ind = ctx.Load(index, irb);
                var indLTlen = ctx.CompareOp(ind, len, (sbyte) '<', false, true, irb);
                ctx.ConditionalBranch(currBB, bbEnd, indLTlen, irb);
                ctx.ResetInsertPoint(currBB, irb);

                if (dest is UnOp uo && uo.Operator == UnOp.OperatorKind.UNPACK) {
                    if (TryRangeExpressionCodeGen(uo.SubExpression, out var destRange)) {
                        succ &= TryGetArrayLength(uo.SubExpression.Position, destRange, uo.SubExpression.ReturnType, out var destLen);
                        var count = ctx.GetMin(destLen, ctx.ArithmeticBinOp(len, ind, (sbyte) '-', true, irb), true, irb);
                        succ &= TryCopyRange(uo.SubExpression.ReturnType, destRange, zero, rangeTp, range, ind, count);
                        ctx.Store(index, ctx.ArithmeticBinOp(ind, count, (sbyte) '+', true, irb), irb);
                    }
                    else {
                        succ = "This unpack-expression cannot be the destination of a deconstruction".Report(dest.Position, false);
                    }
                }
                else {
                    if (TryGetMemoryLocation(dest, out var mem)) {
                        IntPtr val;
                        if (rangeTp.IsArray()) {
                            val = ctx.LoadField(range, new[] { zero, one, ind }, irb);
                        }
                        else if (rangeTp.IsArraySlice()) {
                            val = ctx.LoadField(ctx.ExtractValue(range, 0, irb), new[] { ind }, irb);
                        }
                        else if (rangeTp.IsVarArg()) {
                            //TODO vararg-deconstruction
                            throw new NotImplementedException();
                        }
                        else {
                            // wird nicht auftreten
                            throw new InvalidProgramException();
                        }
                        succ &= TryCast(dest.Position, val, elemTp, dest.ReturnType, out val);
                        ctx.Store(mem, val, irb);
                        ctx.Store(index, ctx.ArithmeticBinOp(ind, one, (sbyte) '+', true, irb), irb);
                    }
                    else {
                        succ = "The non-lvalue expression cannot be destination of a deconstrution".Report(dest.Position, false);
                    }
                }

            }
            ctx.EndLifeTime(index, irb);
            ctx.Branch(bbEnd, irb);
            ctx.ResetInsertPoint(bbEnd, irb);
            return succ;
        }

        bool RangeBasedForlLoopCodeGen(ForeachLoop loopStmt, IntPtr[] loopVariables, IType[] loopVariableTypes, VariableAccessExpression[] loopVariableAccesses, IExpression range, IStatement body) {

            if (range.ReturnType.IsArray() || range.ReturnType.IsArraySlice()) {
                if (!TryRangeExpressionCodeGen(range, out var rng))
                    return false;
                //TODO vararg
                return ArrayBasedForLoopCodeGen(loopStmt, loopVariables, loopVariableAccesses, range, rng, range.ReturnType, body);
            }
            else if (range.ReturnType.IsVarArg()) {
                if (!TryExpressionCodeGen(range, out var vaa))
                    return false;
                return VarargBasedForLoopCodeGen(loopStmt, loopVariables, loopVariableAccesses, range, vaa, range.ReturnType, body);
            }
            else {
                if (!TryExpressionCodeGen(range, out var rng))
                    return false;
                return IterableBasedForLoopCodeGen(loopStmt, loopVariables, loopVariableTypes, rng, range.ReturnType, body, range is BaseExpression);
            }
        }


        IntPtr GetExpressionValue(IExpression ex, IntPtr exVal) {
            if (coro.OtherSaveIndex != null) {
                if (coro.OtherSaveIndex.TryGetValue(ex, out var slot))
                    return ctx.LoadFieldConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, slot }, irb);
                else if (ex is VariableAccessExpression vacc) {
                    TryExpressionCodeGen(vacc, out var ret);
                    return ret;
                }
            }
            return exVal;
        }
        protected void StoreExpressionValue(VariableAccessExpression destEx, IntPtr dest, IntPtr val) {
            if (destEx != null) {
                TryGetMemoryLocation(destEx, out dest);
            }
            ctx.Store(dest, val, irb);
        }
        [Obsolete]
        bool TryGetArrayLength(IType arrayTp, IntPtr arr, out IntPtr arrLen) {
            // array:  {i32, [0 x T]}*
            // fsArray:{i32, [length x T]}
            // span:   {T*, i32}

            // vaa:         spans  spanc  len   offs
            // vararg: {{T*, i32}*, i32, size_t,size_t};

            if (arrayTp.IsFixedSizedArray()) {
                arrLen = ctx.ExtractValue(arr, 0, irb);
            }
            else if (arrayTp.IsArray()) {
                var zero = ctx.GetInt32(0);
                arrLen = ctx.LoadField(arr, new[] { zero, zero }, irb);
            }
            else if (arrayTp.IsArraySlice()) {
                arrLen = ctx.ExtractValue(arr, 1, irb);
            }
            else if (arrayTp.IsVarArg()) {
                arrLen = ctx.ExtractValue(arr, 2, irb);
            }
            else {
                arrLen = IntPtr.Zero;
                return false;
            }
            return true;
        }
        bool TryGetArrayElement(IType arrayTp, IntPtr arr, IntPtr index, out IntPtr elem) {
            // array:  {i32, [0 x T]}*
            // fsArray:{i32, [length x T]}
            // span:   {T*, i32}

            /*if (arrayTp.IsFixedSizedArray()) {
                var tmp = ctx.DefineTypeInferredInitializedAlloca(fn, arr, "arrTmp", irb);
                elem = ctx.LoadField(tmp, new[] { ctx.GetInt32(0), ctx.GetInt32(1), index }, irb);
            }
            else */
            if (arrayTp.IsArray()) {
                elem = ctx.LoadField(arr, new[] { ctx.GetInt32(0), ctx.GetInt32(1), index }, irb);
            }
            else if (arrayTp.IsArraySlice()) {
                var ptr = ctx.ExtractValue(arr, 0, irb);
                elem = ctx.LoadField(ptr, new[] { index }, irb);
            }
            else if (arrayTp.IsVarArg()) {
                //TODO get vararg-element
                throw new NotImplementedException();
            }
            else {
                elem = IntPtr.Zero;
                return false;
            }
            return true;
        }
        bool IterableBasedForLoopCodeGen(ForeachLoop loopStmt, IntPtr[] loopVariables, IType[] loopVariableTypes, IntPtr range, IType rangeTp, IStatement body, bool rangeIsBase) {
            bool succ;
            if (loopVariables.Length != 1) {
                if (loopVariables.Length == 0) {
                    return "There must be a loop-variable in a range-based for-loop".Report(loopStmt.Position, false);
                }
                succ = $"There are too many loop-variables in the range-based for-loop; required: one, got: {loopVariables.Length}".Report(loopStmt.Position, false);
            }
            else
                succ = true;

            // assume, range is valid iterable
            // => has method getIterator -> iterator
            //IntPtr rangeBase, getIterator, tryGetNext;
            IMethod getIteratorMet = rangeTp.Context.InstanceContext.MethodsByName("getIterator").First(x => x.Arguments.Length == 0);
            IMethod tryGetNextMet = getIteratorMet.ReturnType.Context.InstanceContext.MethodsByName("tryGetNext").First(x => x.ReturnType.IsPrimitive(PrimitiveName.Bool) && x.Arguments.All(arg => arg.Type.IsByRef()));

            if (!TryCallCodeGen(loopStmt.Position, getIteratorMet, rangeTp, range, default(Span<IExpression>), out var iterator, getIteratorMet.IsVirtual() && !rangeIsBase || rangeTp.IsInterface()))
                return false;


            var condBlock = new BasicBlock(ctx, "loopCondition", fn);
            var loopBlock = new BasicBlock(ctx, "loop", fn);
            var endBlock = new BasicBlock(ctx, "loopEnd", fn);
            breakableStmts[loopStmt] = (condBlock, endBlock);

            if (!TryCallCodeGen(loopStmt.Position, tryGetNextMet, getIteratorMet.ReturnType, iterator, new[] { loopVariables[0] }, out var initCond, tryGetNextMet.IsVirtual() || rangeTp.IsInterface()))
                return false;

            //var initCond = ctx.GetCall(tryGetNext, new[] { iteratorBase, loopVariables[0] }, irb);
            ctx.ConditionalBranch(loopBlock, endBlock, initCond, irb);

            ctx.ResetInsertPoint(loopBlock, irb);

            succ &= TryInstructionCodeGen(body);

            if (!ctx.CurrentBlockIsTerminated(irb))
                ctx.Branch(condBlock, irb);
            ctx.ResetInsertPoint(condBlock, irb);


            //var llCond = ctx.GetCall(tryGetNext, new[] { iteratorBase, loopVariables[0] }, irb);
            if (!TryCallCodeGen(loopStmt.Position, tryGetNextMet, getIteratorMet.ReturnType, iterator, new[] { loopVariables[0] }, out var llCond, tryGetNextMet.IsVirtual() || rangeTp.IsInterface()))
                return false;

            ctx.ConditionalBranch(loopBlock, endBlock, llCond, irb);
            ctx.ResetInsertPoint(endBlock, irb);
            if (succ && loopStmt.EnableVectorization) {
                succ = "The iterable-based for-loop could not be vectorized. For making vectorization possible, ensure, that the iterable-length is a runtime-constant, for example by iterating over an array instead on a general iterable.".Report(loopStmt.Position, false);
            }
            return succ;
        }
        bool ArrayBasedForLoopCodeGen(ForeachLoop loopStmt, IntPtr[] loopVariables, VariableAccessExpression[] loopIVariables, IExpression rangeEx, IntPtr range, IType arrayTp, IStatement body) {
            bool succ;
            if (loopVariables.Length != 1) {
                if (loopVariables.Length == 0) {
                    return "There must be a loop-variable in a range-based for-loop".Report(loopStmt.Position, false);
                }
                succ = $"There are too many loop-variables in the range-based for-loop; required: one, got: {loopVariables.Length}".Report(loopStmt.Position, false);
            }
            else
                succ = true;
            uint rangeSlot = 0;
            bool iIsAlloca = coro.OtherSaveIndex is null || !coro.OtherSaveIndex.TryGetValue(rangeEx, out rangeSlot);
            // uint i = 0
            var i = coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(rangeEx, out rangeSlot)
                ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 1 }, irb)
                : ctx.DefineZeroinitializedAlloca(fn, ctx.GetSizeTType(), "i_tmp", irb, true);
            IntPtr GetIPtr() {
                return coro.OtherSaveIndex != null ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 1 }, irb) : i;
            }

            var condBlock = new BasicBlock(ctx, "loopCondition", fn);
            var loopBlock = new BasicBlock(ctx, "loop", fn);
            var endBlock = new BasicBlock(ctx, "loopEnd", fn);
            breakableStmts[loopStmt] = (condBlock, endBlock);
            ctx.Store(i, ctx.GetInt32(0), irb);
            succ &= TryGetArrayLength(rangeEx.Position, GetExpressionValue(rangeEx, range), arrayTp, out var arrLen);
            var initCond = ctx.CompareOp(arrLen, ctx.GetInt32(0), (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(loopBlock, endBlock, initCond, irb);


            ctx.ResetInsertPoint(loopBlock, irb);
            succ &= TryGetArrayElement(arrayTp, GetExpressionValue(rangeEx, range), ctx.Load(GetIPtr(), irb), out var elem);
            //ctx.Store(loopVariables[0], elem, irb);
            StoreExpressionValue(loopIVariables[0], loopVariables[0], elem);
            succ &= TryInstructionCodeGen(body);

            if (!ctx.CurrentBlockIsTerminated(irb))
                ctx.Branch(condBlock, irb);
            ctx.ResetInsertPoint(condBlock, irb);

            // i + 1
            i = GetIPtr();
            var itmp = ctx.ArithmeticBinOp(ctx.Load(i, irb), ctx.GetInt32(1), (sbyte) '+', true, irb);
            ctx.Store(i, itmp, irb);
            succ &= TryGetArrayLength(rangeEx.Position, GetExpressionValue(rangeEx, range), arrayTp, out var arrLen2);
            var llCond = ctx.CompareOp(arrLen2, itmp, (sbyte) '>', false, true, irb);

            ctx.ConditionalBranch(loopBlock, endBlock, llCond, irb);
            ctx.ResetInsertPoint(endBlock, irb);
            if (iIsAlloca)
                ctx.EndLifeTime(i, irb);
            if (loopStmt.EnableVectorization) {
                if (iIsAlloca && !ctx.ForceVectorizationForCurrentLoop(loopBlock, fn)) {
                    succ = "The range-based for-loop could not be vectorized".Report(loopStmt.Position, false);
                }
            }
            return succ;
        }
        bool VarArgStartIndex(IntPtr vaa, IntPtr spanIdxAlloca, IntPtr offsetAlloca, IntPtr additionalOffset) {
            // calculate start index for k (spanIndex) and i (offset)
            // see CopyVarArg.c

            var _4 = ctx.GetCurrentBasicBlock(irb);
            BasicBlock
                _8 = new BasicBlock(ctx, "", fn),
                _9 = new BasicBlock(ctx, "", fn),
                _15 = new BasicBlock(ctx, "", fn),
                _18 = new BasicBlock(ctx, "", fn),
                _22 = new BasicBlock(ctx, "", fn),
                _29 = new BasicBlock(ctx, "", fn),
                _loopexit = new BasicBlock(ctx, ".loopexit", fn);

            #region entry
            var _6 = ctx.ExtractValue(vaa, 2, irb);
            var _7 = ctx.CompareOp(_6, additionalOffset, (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(_9, _8, _7, irb);
            #endregion
            #region <label>:8
            ctx.ResetInsertPoint(_8, irb);
            ThrowOutOfBounds(additionalOffset, _6);
            #endregion
            #region <label>:9
            ctx.ResetInsertPoint(_9, irb);
            var _11 = ctx.ExtractValue(vaa, 3, irb);
            var _12 = ctx.ArithmeticBinOp(_11, additionalOffset, (sbyte) '+', true, irb);
            var _14 = ctx.CompareOp(_12, ctx.GetIntSZ(0), (sbyte) '!', true, true, irb);
            ctx.ConditionalBranch(_loopexit, _15, _14, irb);
            #endregion
            #region <label>:15
            ctx.ResetInsertPoint(_15, irb);
            var _17 = ctx.ExtractValue(vaa, 1, irb);
            ctx.Branch(_18, irb);
            #endregion
            #region <label>:18
            ctx.ResetInsertPoint(_18, irb);
            var _19 = new PHINode(ctx, ctx.GetIntType(), 2, irb);
            _19.AddMergePoint(ctx.GetInt32(0), _15);
            var _20 = new PHINode(ctx, ctx.GetSizeTType(), 2, irb);
            _20.AddMergePoint(ctx.GetIntSZ(0), _15);
            var _21 = ctx.CompareOp(_19, _17, (sbyte) '<', false, true, irb);
            ctx.ConditionalBranch(_22, _loopexit, _21, irb);
            #endregion
            #region <label>:22
            ctx.ResetInsertPoint(_22, irb);
            var _23 = ctx.ExtractValue(vaa, 0, irb);
            var _26 = ctx.LoadField(_23, new[] { _19, ctx.GetInt32(1) }, irb);
            var _27 = ctx.ArithmeticBinOp(_26, _20, (sbyte) '+', true, irb);
            _20.AddMergePoint(_27, _29);
            var _28 = ctx.CompareOp(_27, _12, (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(_loopexit, _29, _28, irb);
            #endregion
            #region <label>:29
            ctx.ResetInsertPoint(_29, irb);
            var _30 = ctx.ArithmeticBinOp(_19, ctx.GetInt32(1), (sbyte) '+', true, irb);
            _19.AddMergePoint(_30, _29);
            var _31 = ctx.CompareOp(_12, _27, (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(_18, _loopexit, _31, irb);
            #endregion
            #region .loopexit:
            ctx.ResetInsertPoint(_loopexit, irb);
            var _32 = new PHINode(ctx, ctx.GetSizeTType(), 4, irb);
            _32.AddMergePoint(ctx.GetIntSZ(0), _9);
            _32.AddMergePoint(_20, _18);
            _32.AddMergePoint(_27, _29);
            _32.AddMergePoint(_20, _22);
            var _33 = new PHINode(ctx, ctx.GetIntType(), 4, irb);
            _33.AddMergePoint(ctx.GetInt32(0), _9);
            _33.AddMergePoint(_19, _18);
            _33.AddMergePoint(_30, _29);
            _33.AddMergePoint(_19, _22);
            ctx.Store(spanIdxAlloca, _33, irb);
            var _34 = ctx.CompareOp(_12, _32, (sbyte) '>', false, true, irb);
            var _35 = ctx.ConditionalSelect(_12, _32, _34, irb);
            var _36 = ctx.ArithmeticBinOp(_35, _32, (sbyte) '-', true, irb);
            ctx.Store(offsetAlloca, _36, irb);
            #endregion
            return true;
        }
        bool VarargBasedForLoopCodeGen(ForeachLoop loopStmt, IntPtr[] loopVariables, VariableAccessExpression[] loopVariableAccesses, IExpression rangeEx, IntPtr vaa, IType vaaType, IStatement body) {
            bool succ;
            if (loopVariables.Length != 1) {
                if (loopVariables.Length == 0) {
                    return "There must be a loop-variable in a range-based for-loop".Report(loopStmt.Position, false);
                }
                succ = $"There are too many loop-variables in the range-based for-loop; required: one, got: {loopVariables.Length}".Report(loopStmt.Position, false);
            }
            else
                succ = true;

            var loopHeader = new BasicBlock(ctx, "loopHeader", fn);
            var loopBlock = new BasicBlock(ctx, "loop", fn);
            var condBlock = new BasicBlock(ctx, "loopCondition", fn);
            var outerCondBlock = new BasicBlock(ctx, "outerLoopCondition", fn);
            var endBlock = new BasicBlock(ctx, "loopEnd", fn);

            succ &= TryGetArrayLength(rangeEx.Position, GetExpressionValue(rangeEx, vaa), vaaType, out var vaaLen);
            var initCond = ctx.CompareOp(vaaLen, ctx.GetInt32(0), (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(loopHeader, endBlock, initCond, irb);
            ctx.ResetInsertPoint(loopHeader, irb);

            uint rangeSlot = 0;
            bool iIsAlloca = coro.OtherSaveIndex is null || !coro.OtherSaveIndex.TryGetValue(rangeEx, out rangeSlot);

            var k = coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(rangeEx, out rangeSlot)
                ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 1 }, irb)
                : ctx.DefineZeroinitializedAlloca(fn, ctx.GetIntType(), "k_tmp", irb, true);


            var i = coro.OtherSaveIndex != null && coro.OtherSaveIndex.TryGetValue(rangeEx, out rangeSlot)
                ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 2 }, irb)
                : ctx.DefineZeroinitializedAlloca(fn, ctx.GetSizeTType(), "i_tmp", irb, true);

            succ &= VarArgStartIndex(vaa, k, i, ctx.GetIntSZ(0));
            IntPtr GetIPtr() {
                return coro.OtherSaveIndex != null ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 2 }, irb) : i;
            }
            IntPtr GetKPtr() {
                return coro.OtherSaveIndex != null ? ctx.GetElementPtrConstIdx(ctx.GetArgument(fn, coro.MutableArgsIndex), new[] { 0u, rangeSlot + 1 }, irb) : k;
            }


            breakableStmts[loopStmt] = (condBlock, endBlock);
            //ctx.Store(i, ctx.GetInt32(0), irb);



            //ctx.ConditionalBranch(loopBlock, endBlock, initCond, irb);
            ctx.Branch(loopBlock, irb);

            ctx.ResetInsertPoint(loopBlock, irb);

            var spansPtr = ctx.ExtractValue(GetExpressionValue(rangeEx, vaa), 0, irb);
            var span = ctx.LoadField(spansPtr, new[] { ctx.Load(GetKPtr(), irb) }, irb);
            var elem = ctx.LoadField(ctx.ExtractValue(span, 0, irb), new[] { ctx.Load(GetIPtr(), irb) }, irb);
            StoreExpressionValue(loopVariableAccesses[0], loopVariables[0], elem);

            succ &= TryInstructionCodeGen(body);
            if (!ctx.CurrentBlockIsTerminated(irb))
                ctx.Branch(condBlock, irb);

            ctx.ResetInsertPoint(condBlock, irb);
            // ++i
            i = GetIPtr();
            var itmp = ctx.ArithmeticBinOp(ctx.Load(i, irb), ctx.GetInt32(1), (sbyte) '+', true, irb);
            ctx.Store(i, itmp, irb);
            span = ctx.LoadField(spansPtr, new[] { ctx.Load(GetKPtr(), irb) }, irb);
            var spanLen = ctx.ExtractValue(span, 1, irb);
            // i < span[k].length
            var innerCond = ctx.CompareOp(spanLen, ctx.Load(i, irb), (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(loopBlock, outerCondBlock, innerCond, irb);
            ctx.ResetInsertPoint(outerCondBlock, irb);
            //++k
            k = GetKPtr();
            var ktmp = ctx.ArithmeticBinOp(ctx.Load(k, irb), ctx.GetInt32(1), (sbyte) '+', true, irb);
            ctx.Store(k, ktmp, irb);
            //i = 0
            ctx.Store(GetIPtr(), ctx.GetIntSZ(0), irb);
            // k < spanc
            var spanc = ctx.ExtractValue(GetExpressionValue(rangeEx, vaa), 1, irb);
            var outerCond = ctx.CompareOp(spanc, ctx.Load(k, irb), (sbyte) '>', false, true, irb);
            ctx.ConditionalBranch(loopBlock, endBlock, outerCond, irb);
            ctx.ResetInsertPoint(endBlock, irb);

            if (iIsAlloca) {
                ctx.EndLifeTime(i, irb);
                ctx.EndLifeTime(k, irb);
            }
            if (loopStmt.EnableVectorization) {
                if (iIsAlloca && !ctx.ForceVectorizationForCurrentLoop(loopBlock, fn)) {
                    succ = "The range-based for-loop could not be vectorized".Report(loopStmt.Position, false);
                }
            }
            return succ;
        }
        bool TryLoopCodeGen(IStatement loopStmt, IExpression cond, IStatement body, bool headControlled) {
            bool succ = true;
            var condBlock = new BasicBlock(ctx, "loopCondition", fn);
            var loopBlock = new BasicBlock(ctx, "loop", fn);
            var endBlock = new BasicBlock(ctx, "loopEnd", fn);
            breakableStmts[loopStmt] = (condBlock, endBlock);
            if (headControlled) {
                succ &= TryExpressionCodeGen(cond, out var initCond);
                ctx.ConditionalBranch(loopBlock, endBlock, initCond, irb);
            }
            else {
                ctx.Branch(loopBlock, irb);
            }
            ctx.ResetInsertPoint(loopBlock, irb);
            succ &= TryInstructionCodeGen(body);
            if (!ctx.CurrentBlockIsTerminated(irb))
                ctx.Branch(condBlock, irb);

            ctx.ResetInsertPoint(condBlock, irb);
            succ &= TryExpressionCodeGen(cond, out var llCond);
            ctx.ConditionalBranch(loopBlock, endBlock, llCond, irb);

            ctx.ResetInsertPoint(endBlock, irb);
            return succ;
        }
        bool TryForLoopCodeGen(IStatement loopStmt, IExpression cond, IStatement body, IStatement incr) {
            bool succ = true;
            var condBlock = new BasicBlock(ctx, "loopCondition", fn);
            var loopBlock = new BasicBlock(ctx, "loop", fn);
            var endBlock = new BasicBlock(ctx, "loopEnd", fn);
            breakableStmts[loopStmt] = (condBlock, endBlock);

            succ &= TryExpressionCodeGen(cond, out var initCond);
            ctx.ConditionalBranch(loopBlock, endBlock, initCond, irb);

            ctx.ResetInsertPoint(loopBlock, irb);
            succ &= TryInstructionCodeGen(body);
            if (!ctx.CurrentBlockIsTerminated(irb))
                ctx.Branch(condBlock, irb);

            ctx.ResetInsertPoint(condBlock, irb);
            succ &= TryInstructionCodeGen(incr);
            succ &= TryExpressionCodeGen(cond, out var llCond);
            ctx.ConditionalBranch(loopBlock, endBlock, llCond, irb);

            ctx.ResetInsertPoint(endBlock, irb);
            return succ;
        }
        protected internal virtual bool FinalizeMethodBody() {
            var currBlock = ctx.GetCurrentBasicBlock(irb);
            bool hasPredecessors = ctx.CurrentBlockCountPredecessors(irb) != 0;
            if (!ctx.CurrentBlockIsTerminated(irb) && (currBlock == entryBlock || hasPredecessors)) {
                if (coro.IsAsynchronousCoroutine || coro.IsGeneratorCoroutine)
                    ctx.Branch(coro.FallThroughTarget, irb);
                else {
                    if (method.ReturnType.IsVoid())
                        ctx.ReturnVoid(irb);
                    else// static semantic analysis will enforce, that all paths return
                        ctx.Unreachable(irb);
                }
            }
            if (!hasPredecessors && currBlock != entryBlock) {
                BasicBlock.RemoveBasicBlock(currBlock);
            }
            return true;
        }
    }

}
