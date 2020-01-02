/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Parser;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    class ASTFourthPass : ASTThirdPass {
        class LambdaBlockContext {
            public readonly Stack<IContext> contextStack = new Stack<IContext>();
            public readonly Dictionary<IVariable, LambdaCapture> captures = new Dictionary<IVariable, LambdaCapture>();
            public readonly Dictionary<(LambdaCapture, IExpression), ICaptureExpression> captureAccesses
                = new Dictionary<(LambdaCapture, IExpression), ICaptureExpression>();
        }

        readonly Stack<IStatement> redirectTarget = new Stack<IStatement>();
        Method.Specifier currentMethod = Method.Specifier.None;
        IMethod currMet = null;
        IType currentReturnType = null;
        string fileName;
        readonly Dictionary<LambdaExpression, (FBlangParser.ExprContext, FBlangParser.BlockInstructionContext)> incompleteLambdas
            = new Dictionary<LambdaExpression, (FBlangParser.ExprContext, FBlangParser.BlockInstructionContext)>();
        readonly Stack<LambdaBlockContext> lambdaContextStack = new Stack<LambdaBlockContext>();
        readonly Stack<ISet<IVariable>> nonNullableStack = new Stack<ISet<IVariable>>();
        readonly Stack<(MacroCallParameters, InstructionBox)> unresolvedMacroCall = new Stack<(MacroCallParameters, InstructionBox)>();
        MacroFunction currentMacro = null;
        public ASTFourthPass(ASTThirdPass thirdPassResult)
            : base(thirdPassResult.Module, thirdPassResult.AllTypes, thirdPassResult.AllTypeTemplates, thirdPassResult.AllFields,
                  thirdPassResult.AllMethods, thirdPassResult.AllMethodTemplates, thirdPassResult.AllMacros) {
        }
        protected internal ASTFourthPass(FBModule mod,
            IEnumerable<(IType, FBlangParser.TypeDefContext)> allTypes,
            IEnumerable<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> allTypeTemplates,
            IEnumerable<(BasicVariable, FieldInfo)> allVariables,
            IEnumerable<(BasicMethod, MethodInfo)> allMethods,
            IEnumerable<(BasicMethodTemplate, FBlangParser.MethodDefContext)> allMethodTemplates,
            IEnumerable<(MacroFunction, FBlangParser.MacroDefContext)> allMacros)
            : base(mod, allTypes, allTypeTemplates, allVariables, allMethods, allMethodTemplates, allMacros) {
        }

        public new void Visit() {
            foreach (var macro in AllMacros) {
                fileName = macro.Item1.Position.FileName;
                using (PushContext(macro.Item1.NestedIn)) {
                    using (nonNullableStack.PushFrame(new HashSet<IVariable>())) {
                        currentMacro = macro.Item1;
                        macro.Item1.Body.Instruction = VisitBlockInstruction(macro.Item2.blockInstruction());
                        currentMacro.Finish();
                    }
                }
            }
            currentMacro = null;
            // first do including-fields, since the default-value of that field may already invoke an included method
            foreach (var fld in AllFields) {
                if (fld.Item2.IsIncluding) {
                    fileName = fld.Item1.Position.FileName;
                    HandleIncludingField(fld.Item1, fld.Item2);
                }
            }

            foreach (var fld in AllFields) {
                if (fld.Item2.DefaultValue != null) {
                    using (PushContext(fld.Item1.Parent)) {
                        fileName = fld.Item1.Position.FileName;
                        // field-initializers only see the static members
                        currentMethod = Method.Specifier.Static;
                        fld.Item1.DefaultValue = VisitExpression(fld.Item2.DefaultValue);
                        if (!fld.Item1.DefaultValue.IsError() && !Type.IsAssignable(fld.Item1.DefaultValue.ReturnType, fld.Item1.Type)) {
                            $"The initialization-value of the field {fld.Item1.Signature} cannot be converted to the required type {fld.Item1.Type.Signature}".Report(fld.Item2.DefaultValue.Position(fld.Item1.Position.FileName));
                        }
                    }
                }

            }
            foreach (var met in AllMethods) {
                bool noBody = met.Item1.IsAbstract()
                    || met.Item1.IsInternal()
                    || met.Item1.IsExternal();
                if (met.Item2.blockInstruction() != null || met.Item2.expr() != null) {
                    fileName = met.Item1.Position.FileName;
                    if (noBody) {
                        //assume: abstract, internal and external are disjoint
                        $"The {(met.Item1.Specifiers & (Method.Specifier.Abstract | Method.Specifier.Internal | Method.Specifier.External)).ToString().ToLower()} method {met.Item1.Signature} must not provide an implementation".Report(met.Item2.blockInstruction().Position(met.Item1.Position.FileName));
                    }

                    using (PushContext(met.Item1.Context)) {
                        using (nonNullableStack.PushFrame(new HashSet<IVariable>())) {
                            SetMethodOverride(met.Item1);
                            currentMethod = met.Item1.Specifiers;
                            currMet = met.Item1;
                            currentReturnType = met.Item1.ReturnType;
                            if (met.Item2.blockInstruction() != null)
                                met.Item1.Body.Instruction = VisitBlockInstruction(met.Item2.blockInstruction());
                            else {
                                if (met.Item1.ReturnType.IsVoid())
                                    met.Item1.Body.Instruction = new ExpressionStmt(met.Item2.expr().Position(fileName), VisitExpression(met.Item2.expr()));
                                else
                                    met.Item1.Body.Instruction = new ReturnStatement(met.Item2.expr().Position(fileName), VisitExpression(met.Item2.expr(), met.Item1.ReturnType));
                            }
                            if (met.Item1.IsInitCtor()) {
                                var fields = met.Item1.Context.DefinedInType.Context.InstanceContext.Variables;
                                var cmp = new FunctionalEquiComparer<IVariable>(
                                            (x, y) => x.Signature.Name == y.Signature.Name && (x.Type == y.Type ||
                                                                                               x.Type.IsByConstRef(out var brt1) && brt1.UnderlyingType == y.Type ||
                                                                                               y.Type.IsByConstRef(out var brt2) && brt2.UnderlyingType == x.Type),
                                            x => x.Signature.Name.GetHashCode() ^ x.Type.UnWrapAll().GetHashCode());
                                var fieldNames = fields.Values.ToHashSet(cmp);
                                fieldNames.IntersectWith(met.Item1.Arguments);

                                if (fieldNames.Any()) {
                                    var stmts = Vector<IStatement>.Reserve(1u + (uint) fieldNames.Count);
                                    var pos = met.Item1.Position;
                                    var tp = met.Item1.Context.DefinedInType;
                                    foreach (var arg in met.Item1.Arguments) {
                                        if (fieldNames.TryGetValue(arg, out var fld)) {
                                            stmts.Add(new ExpressionStmt(pos, new BinOp(pos, null,
                                                new VariableAccessExpression(pos, null, fld, new ThisExpression(pos, tp)), BinOp.OperatorKind.ASSIGN_NEW,
                                                new VariableAccessExpression(pos, null, arg))));
                                        }
                                    }
                                    stmts.Add(met.Item1.Body.Instruction);
                                    met.Item1.Body.Instruction = new BlockStatement(pos, stmts.AsArray(), met.Item1.Context);
                                }
                                else {
                                    "The init-constructor has no matching parameters. Fo clearness leave out the 'init' keyword".Warn(met.Item1.Position, WarningLevel.Low);
                                }
                            }
                            else if (met.Item1.IsCopyCtor()) {
                                "The auto-copy ctor is not implemented yet".Warn(met.Item1.Position, WarningLevel.VeryHigh);
                            }
                            met.Item1.Specifiers |= currentMethod;

                            VerifyValueReturningMethod(met.Item1);
                            if (met.Item1.IsConstructor()) {
                                var tp = NextTypeContext(new[] { met.Item1.NestedIn });
                                if (tp is IHierarchialType htp && htp.SuperType != null) {
                                    VerifyHasSuperCall(met.Item1, htp);
                                }
                            }
                            else if (met.Item1.IsCoroutine() && met.Item1.IsAsync() && !(met.Item1.NestedIn is ITypeContext tcx && tcx.Type != null && tcx.Type.IsActor())) {
                                EliminateReturnAwait(met.Item1);
                            }
                        }

                    }
                }
                else if (!noBody) {
                    $"The not-abstract method {met.Item1.Signature} must provide an implementation".Report(met.Item1.Position);
                }
            }
            foreach (var met in AllMethodTemplates) {
                var specs = met.Item1.Specifiers;

                bool noBody = specs.HasFlag(Method.Specifier.Abstract)
                    || specs.HasFlag(Method.Specifier.Internal)
                    || specs.HasFlag(Method.Specifier.External);
                if (met.Item2.blockInstruction() != null || met.Item2.expr() != null) {
                    fileName = met.Item1.Position.FileName;
                    if (noBody) {
                        //assume: abstract, internal and external are disjoint
                        $"The {(specs & (Method.Specifier.Abstract | Method.Specifier.Internal | Method.Specifier.External)).ToString().ToLower()} method {met.Item1.Signature} must not provide an implementation".Report(met.Item2.blockInstruction().Position(met.Item1.Position.FileName));
                    }

                    using (PushContext(met.Item1.Context)) {
                        using (nonNullableStack.PushFrame(new HashSet<IVariable>())) {
                            SetMethodOverride(met.Item1);
                            currentMethod = met.Item1.Specifiers;
                            currentReturnType = met.Item1.ReturnType;
                            if (met.Item2.blockInstruction() != null)
                                met.Item1.Body.Instruction = VisitBlockInstruction(met.Item2.blockInstruction());
                            else {
                                if (met.Item1.ReturnType.IsVoid())
                                    met.Item1.Body.Instruction = new ExpressionStmt(met.Item2.expr().Position(fileName), VisitExpression(met.Item2.expr()));
                                else
                                    met.Item1.Body.Instruction = new ReturnStatement(met.Item2.expr().Position(fileName), VisitExpression(met.Item2.expr(), met.Item1.ReturnType));
                            }
                            VerifyValueReturningMethod(met.Item1);

                            met.Item1.Specifiers |= currentMethod;
                            if ((met.Item1.Specifiers & (Method.Specifier.Coroutine | Method.Specifier.Async)) != Method.Specifier.None && !(met.Item1.NestedIn is ITypeContext tcx && tcx.Type != null && tcx.Type.IsActor())) {
                                EliminateReturnAwait(met.Item1);
                            }
                        }
                    }
                }
                else {
                    if (!noBody) {
                        $"The not-abstract method {met.Item1.Signature} must provide an implementation".Report(met.Item1.Position);
                    }
                }
            }
            currentMethod = Method.Specifier.None;
            using var mains = Module.MethodsByName("main").Skip(1).GetEnumerator();
            // more than one main-method
            while (mains.MoveNext()) {
                "The main-method cannot be overloaded".Report(mains.Current.Position);
            }
        }

        private static void HandleIncludingField(BasicVariable fld, FieldInfo info) {
            var fldTy = fld.Type.UnWrapAll();
            var methods = info.IncludingInterface.Context.InstanceContext.Methods.Values
                .Where(x => x.Visibility == Visibility.Public)
                .Select(x => {
                    if (fldTy.Context.InstanceContext.Methods.TryGetValue(x.Signature, out var mret))
                        return mret;
                    else {
                        $"The field-type {fldTy.Signature} does not define a method '{x.Signature}' which is required from the ascription-type {info.IncludingInterface.Signature}".Warn(fld.Position, WarningLevel.High);
                        return null;
                    }
                })
                .Where(x => x != null).ToArray();
            var tp = fld.DefinedInType;
            var tpCtx = tp.Signature.BaseGenericType != null
                ? tp.Signature.BaseGenericType.Context
                : tp.Context;
            foreach (var dlgMet in methods) {
                if (!info.NotInclude.Contains(dlgMet.Signature)) {
                    if (tpCtx.InstanceContext.Methods.TryGetValue(dlgMet.Signature, out var other)) {
                        if (other.IsAutoIncluded())//DOLATER: specify, which other including-field causes the error
                            $"Delegation ambiguity detected: The method {dlgMet.Signature} can be called from multiple sources".Report();
                    }
                    else {
                        // here we define the new method
                        var met = new BasicMethod(fld.Position,
                            dlgMet.Signature.Name,
                            dlgMet.Visibility,
                            dlgMet.ReturnType,
                            dlgMet.Arguments.Select(x => new BasicVariable(fld.Position,
                                                            x.Type,
                                                            x.VariableSpecifiers,
                                                            x.Signature.Name,
                                                            x.DefinedInType,
                                                            x.Visibility)
                                                        ).ToArray()
                            ) {
                            NestedIn = tp.Context
                        };
                        met.Context = new SimpleMethodContext(tpCtx.Module, Context.DefiningRules.Variables, tpCtx, args: met.Arguments);
                        met.Specifiers = (dlgMet.Specifiers | FBMethodSpecifier.AutoIncluded) & ~Method.Specifier.Abstract & ~Method.Specifier.Overrides;

                        var call = new CallExpression(fld.Position, dlgMet.ReturnType, dlgMet,
                            new VariableAccessExpression(fld.Position, fld.Type, fld, new ThisExpression(fld.Position, tp)),
                            met.Arguments.Select(x => x.Type.IsVarArg()
                            ? (IExpression) new UnOp(fld.Position, null, UnOp.OperatorKind.UNPACK, new VariableAccessExpression(fld.Position, x.Type, x))
                            : (IExpression) new VariableAccessExpression(fld.Position, x.Type, x))
                            .AsCollection(met.Arguments.Length));
                        if (call.ReturnType.IsVoid())
                            met.Body.Instruction = new ExpressionStmt(fld.Position, call);
                        else
                            met.Body.Instruction = new ReturnStatement(fld.Position, call);
                        tpCtx.DefineMethod(met);
                    }
                }
            }
            //TODO include also public method-templates
        }

        IDisposable PushContext(IContext ctx) {
            var frame1 = contextStack.PushFrame(ctx);
            if (lambdaContextStack.TryPeek(out var lambdaCtxStack)) {
                var frame2 = lambdaCtxStack.contextStack.PushFrame(ctx);
                return frame1.Combine(frame2);
            }
            return frame1;
        }
        void SetMethodOverride(IDeclaredMethod met) {
            if (!met.Specifiers.HasFlag(Method.Specifier.Virtual))
                return;
            var superTp = met.NestedIn is ITypeContext tcx && tcx.Type is IHierarchialType htp ? htp.SuperType : null;
            if (superTp is null)
                return;

            if (met is IMethod m) {
                var mets = superTp.Context.InstanceContext.MethodsByName(m.Signature.Name);

                var res = Module.Semantics.BestFittingMethod(m.Position, mets, m.Arguments.Select(x => x.Type).AsCollection(m.Arguments.Length), m.ReturnType, out var ov);
                if (res == Error.None) {
                    if (m is BasicMethod bm) {
                        bm.Overrides = ov;
                        bm.Specifiers |= Method.Specifier.Overrides;
                    }
                }
                else if (res == Error.AmbiguousTarget) {
                    $"The virtual method {m.Signature} overrides a method from the superclass, which is ambiguous".Report(m.Position);
                }

            }
            else if (met is IMethodTemplate<IMethod> tm) {
                var mets = superTp.Context.InstanceContext.MethodTemplatesByName(tm.Signature.Name);
                var res = Module.Semantics.BestFittingMethod(tm.Position, mets, tm.Arguments.Select(x => x.Type).AsCollection(tm.Arguments.Length), tm.ReturnType, out var ov);
                if (res == Error.None) {
                    if (tm is BasicMethodTemplate btm) {
                        btm.Specifiers |= Method.Specifier.Overrides;
                        btm.Overrides = ov.Signature.BaseMethodTemplate;
                    }
                }
                else if (res == Error.AmbiguousTarget) {
                    $"The virtual method {tm.Signature} overrides a method from the superclass, which is ambiguous".Report(tm.Position);
                }
            }
        }
        void VerifyValueReturningMethod(IDeclaredMethod met) {
            if (met.Body != null && !met.Body.Instruction.IsError() && !met.Specifiers.HasFlag(Method.Specifier.Coroutine)) {

                var expectedRetTp = met.Specifiers.HasFlag(Method.Specifier.Async | Method.Specifier.Coroutine) ? (met.ReturnType as IWrapperType).ItemType : met.ReturnType;
                var unreachableStmts = new List<IStatement>();
                var cfg = Module.Semantics.CreateControlFlowGraph(met.Body.Instruction, expectedRetTp, unreachableStmts, x => x is ReturnStatement);
                if (expectedRetTp != null && !expectedRetTp.IsPrimitive(PrimitiveName.Void)) {
                    if (!cfg.AllPathsTerminate()) {
                        "Not all paths return a value".Report(met.Position);
                    }
                }
                if (Module.WarningLevel > 0) {
                    unreachableStmts.ForEach(x => Console.WriteLine($"Warning {x.Position}: Unreachable statement detected"));
                }
            }
        }
        void VerifyHasSuperCall(IDeclaredMethod ctor, IHierarchialType declaredIn) {
            if (ctor.Body != null && !ctor.Body.Instruction.IsError()) {
                var statementsBeforeTerminator = new List<IStatement>();
                var cfg = Module.Semantics.CreateControlFlowGraph(ctor.Body.Instruction, Type.Top, statementsBeforeTerminator, x => x is SuperCall, RelativeStatementPosition.Before);
                if (Module.WarningLevel > 1) {
                    statementsBeforeTerminator.ForEach(x => Console.WriteLine($"Warning {x.Position}: Detected statement before call to super-constructor. This might be unsafe"));
                }
                if (!cfg.AllPathsTerminate()) {
                    var parameterlessCtor = declaredIn.SuperType.Context.InstanceContext.LocalContext.MethodsByName("ctor").Where(x => x.Arguments.Length == 0);
                    if (parameterlessCtor.Any()) {
                        ctor.Body.Instruction = new BlockStatement(ctor.Body.Instruction.Position,
                            new[] {
                                new SuperCall(ctor.Body.Instruction.Position, declaredIn.SuperType, parameterlessCtor.First(), Array.Empty<IExpression>()) ,
                                ctor.Body.Instruction
                            },
                            ctor.Context
                        );
                    }
                    else {
                        "Not all paths call a constructor of the superclass".Report(ctor.Position);
                    }
                }
            }
        }
        void EliminateReturnAwait(IDeclaredMethod met) {
            if (met.Body.HasValue) {
                bool noLongerCoroutine;
                if ((met.ReturnType as IWrapperType).ItemType.IsVoid()) {
                    var rets = new List<(IStatement[], int)>();
                    if (noLongerCoroutine = IdentifyAwaitVoid(met.Body.Instruction, rets)) {
                        if (rets.Count == 1 && rets[0].Item1 is null) {
                            met.Body.Instruction = new ReturnStatement(met.Body.Instruction.Position, ((met.Body.Instruction as ExpressionStmt).Expression as UnOp).SubExpression);
                        }
                        else
                            rets.ForEach(x => x.Item1[x.Item2] = new ReturnStatement(x.Item1[x.Item2].Position, ((x.Item1[x.Item2] as ExpressionStmt).Expression as UnOp).SubExpression));
                    }
                }
                else {
                    var rets = new List<ReturnStatement>();
                    if (noLongerCoroutine = IdentifyReturnAwait(met.Body.Instruction, rets))
                        rets.ForEach(x => x.ReturnValue = (x.ReturnValue as UnOp).SubExpression);
                }
                if (noLongerCoroutine) {

                    if (met is BasicMethod bm)
                        bm.Specifiers &= ~Method.Specifier.Async & ~Method.Specifier.Coroutine;
                    else if (met is BasicMethodTemplate btm)
                        btm.Specifiers &= ~Method.Specifier.Async & ~Method.Specifier.Coroutine;
                }
            }

        }
        bool IdentifyAwaitVoid(IStatement stmt, ICollection<(IStatement[], int)> rets, IStatement[] par = null, int parIdx = 0) {
            if (stmt is ExpressionStmt est && est.Expression is UnOp uo && uo.Operator == UnOp.OperatorKind.AWAIT) {
                rets.Add((par, parIdx));
                return true;
            }
            else if (stmt is BlockStatement blck) {

                for (int i = 0; i < blck.Statements.Length; ++i) {
                    if (i == blck.Statements.Length - 1 || blck.Statements[i + 1] is ReturnStatement) {
                        if (!IdentifyAwaitVoid(blck.Statements[i], rets, blck.Statements, i))
                            return false;
                    }
                    else {
                        if (HasAwaitExpression(blck.Statements[i]))
                            return false;
                    }
                }
                return true;
            }
            else
                return !HasAwaitExpression(stmt);
        }
        bool IdentifyReturnAwait(IStatement stmt, ICollection<ReturnStatement> rets) {
            if (stmt is ReturnStatement rst && rst.ReturnValue is UnOp uo && uo.Operator == UnOp.OperatorKind.AWAIT) {
                rets.Add(rst);
                return true;
            }
            else {

                if (stmt.GetExpressions().Any(HasAwaitExpression))
                    return false;
                foreach (var s in stmt.GetStatements()) {
                    if (!IdentifyReturnAwait(s, rets))
                        return false;
                }
                return true;
            }
        }
        bool HasAwaitExpression(IExpression ex) {
            if (ex is UnOp uo && uo.Operator == UnOp.OperatorKind.AWAIT) {
                return true;
            }
            else {
                return ex.GetExpressions().Any(HasAwaitExpression);
            }
        }
        bool HasAwaitExpression(IStatement stmt) {
            if (stmt.GetExpressions().Any(HasAwaitExpression))
                return true;
            return stmt.GetStatements().Any(HasAwaitExpression);
        }
        protected IContext NextInstanceContext() {
            foreach (var ctx in contextStack) {
                if (ctx is ITypeContext tc) {
                    return tc.InstanceContext;
                }
                else if (ctx is ITypeTemplateContext ttc) {
                    return ttc.InstanceContext;
                }
            }
            return null;
        }
        void UpdateNotNullSet(bool add, params IVariable[] vars) {
            if (add) {
                if (!nonNullableStack.Peek().IsSupersetOf(vars)) {
                    nonNullableStack.ExchangeTop(new HashSet<IVariable>(nonNullableStack.Peek()));
                    nonNullableStack.Peek().UnionWith(vars);
                }
            }
            else {
                var toRemove = vars.Where(x => nonNullableStack.Peek().Contains(x));
                if (toRemove.Any()) {
                    nonNullableStack.ExchangeTop(new HashSet<IVariable>(nonNullableStack.Peek().Except(toRemove)));
                }
            }
        }

        bool IsLValue(IExpression ex) {
            return ex.IsLValue(currMet) || ex is VariableAccessExpression vra && vra.ParentExpression is ThisExpression && currentMethod.HasFlag(Method.Specifier.Constructor);
        }

        protected IType NextTypeContext() {
            return NextTypeContext(contextStack);
        }
        void DoDefineVariables(params IVariable[] vars) {
            DoDefineVariables(vars, contextStack.Peek());
        }
        void DoDefineVariables(IVariable[] vars, IContext ctx) {
            foreach (var vr in vars) {
                if (vr.Signature.Name.StartsWith('%')) {
                    if (currentMacro != null) {
                        if (!currentMacro.LocalContext.DefineVariable(vr).Get(out var fail)) {
                            if (fail == CannotDefine.AlreadyExisting)
                                $"Cannot define macro-local variable {vr.Signature}: A variable with the name {vr.Signature.Name} is already defined in {ctx.Name}".Report(vr.Position);
                            else
                                $"Cannot define macro-local variable {vr.Signature}".Report(vr.Position);
                        }
                    }
                    else {
                        "Variables with the '%'-prefix can only be defined in a macro-context".Report(vr.Position);
                    }
                }
                else if (!ctx.DefineVariable(vr).Get(out var fail)) {
                    if (fail == CannotDefine.AlreadyExisting)
                        $"Cannot define variable {vr.Signature}: A variable with the name {vr.Signature.Name} already exists in {ctx.Name}".Report(vr.Position);
                    else
                        $"Cannot define variable {vr.Signature}".Report(vr.Position);
                }
            }
        }
        void DoDefineVariables(Declaration decl, IContext ctx) {
            DoDefineVariables(decl.Variables, ctx);
        }

        bool TryGetInternalExternalCall(Position pos, string name, IExpression[] args, bool isInternalCall, IType expectedRetTy, out IExpression ret, ErrorBuffer err) {
            //TODO create internalcall/externalcall
            switch (name) {
                case "cprintln":
                case "cprint": {
                    if (args.Length != 1) {
                        ret = err.Report($"The built-in method {name} needs exactly one argument of type string; got {string.Join(", ", args.Select(x => x.ReturnType.Signature))}", pos, Expression.Error);
                        return false;
                    }
                    if (!Type.IsAssignable(args[0].ReturnType, PrimitiveType.String)) {
                        ret = err.Report($"The {args[0].ReturnType.Signature}-argument of the built-in method {name} cannot be converted to string", pos, Expression.Error);
                        return false;
                    }
                    IExpression arg = args[0].ReturnType == PrimitiveType.String
                        ? args[0]
                        : new TypecastExpression(args[0].Position, args[0], PrimitiveType.String);
                    ret = new CallExpression(pos, PrimitiveType.Void, Module.MethodsByName(name).First(), null, args);
                    return true;
                }
                case "randomUInt": {
                    if (args.Length == 0) {
                        if (expectedRetTy != null && !expectedRetTy.IsError() && !Type.IsAssignable(expectedRetTy, PrimitiveType.UInt))
                            err.Report($"The return-type uint is not compatible with the expected type {expectedRetTy}");
                        Module.TryGetInternalMethod("randomUInt", out var met);
                        ret = new CallExpression(pos, PrimitiveType.UInt, met, null, Array.Empty<IExpression>());
                        return true;
                    }
                    else if (args.Length == 2) {

                        if (!Type.IsAssignable(args[0].ReturnType, PrimitiveType.UInt) || !Type.IsAssignable(args[1].ReturnType, PrimitiveType.UInt))
                            err.Report($"The arguments of the internal function 'randomUInt' must be both of type uint", pos);

                        if (expectedRetTy != null && !expectedRetTy.IsError() && !Type.IsAssignable(expectedRetTy, PrimitiveType.UInt))
                            err.Report($"The return-type uint is not compatible with the expected type {expectedRetTy}");
                        Module.TryGetInternalMethod("randomUIntRange", out var met);
                        ret = new CallExpression(pos, PrimitiveType.UInt, met, null, args);
                        return true;
                    }
                    else {
                        err.Report($"The internal function 'randomUInt' does not accept {args.Length} arguments. It needs exactly two arguments of type uint", pos);
                        ret = Expression.Error;
                        return false;
                    }
                }
            }
            throw new NotImplementedException();
        }
        IExpression GetInternalExternalCall(Position pos, string name, IExpression[] args, bool isInternalCall, IType expectedRetTy) {
            TryGetInternalExternalCall(pos, name, args, isInternalCall, expectedRetTy, out var ret, null);
            return ret;
        }
        IContext CurrentNamespace() {
            return contextStack.Peek().FirstWhere(ctx => Module.IsNamespaceContext(ctx, out _));
        }
        IExpression InstanceVariableWithImplicitThis(Position pos, IVariable vr, IType inst, ErrorBuffer err) {
            if (inst != null) {
                if (inst.Context.InstanceContext.Variables.Contains(new KeyValuePair<Variable.Signature, IVariable>(vr.Signature, vr))) {
                    var ret = CreateVariableAccessExpression(pos, null, vr, new ThisExpression(pos, inst));
                    if (IsLambdaCapture(vr, out var cap))
                        return GetCaptureAccessExpression(pos, null, ret, cap, new ThisExpression(pos, inst));
                    return ret;
                }
                else {
                    return err.Report($"The type {inst.Signature} does not define an instance-variable {vr.Signature}", pos, Expression.Error);
                }
            }
            else {
                return err.Report($"The instance-variable {vr.Signature} is not defined in this context", pos, Expression.Error);
            }
        }
        /* bool TryGetCallees(FBlangParser.ExContext parentAndName, IType expectedRetTy, out IEnumerable<IMethod> ret, ErrorBuffer err) {
             //TODO get all possible callees
             throw new NotImplementedException();
         }*/
        bool TryGetCallInternal(FBlangParser.ExContext parentAndName, IExpression[] args, IType expectedRetTy, out IExpression ret, ErrorBuffer err, string fileName) {
            // retrieve method (and parent-expr) from parentAndName and create call

            if (parentAndName is FBlangParser.MemberAccessExprContext memb) {
                if (VisitMemberAccessExpr(memb, out var membPar, out var membParTy, out var membCtx, out var membName, expectedRetTy, err)) {
                    if (membParTy != null) {
                        ret = Module.Semantics.CreateCall(parentAndName.Position(fileName), expectedRetTy ?? Type.Top, membParTy, membPar, membName, args);
                        return !ret.IsError();
                    }
                    else if (membName != null) {
                        var glob = ((IEnumerable<IDeclaredMethod>) membCtx.MethodsByName(membName)).Concat(membCtx.MethodTemplatesByName(membName));
                        if (args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack)) {

                            ret = new CallExpression(parentAndName.Position(fileName), membName, glob, null, args, expectedRetTy);
                            return true;
                        }
                        var met = Module.Semantics.BestFittingMethod(parentAndName.Position(fileName), glob, args.Select(x => x.MinimalType()).AsCollection(args.Length), expectedRetTy ?? Type.Top);


                        ret = Module.Semantics.CreateCall(parentAndName.Position(fileName), met.ReturnType, met, null, args);

                        return !met.IsError();
                    }
                    else {
                        //wird wahrscheinlich nicht auftreten
                        err.Report("The call-expression must invoke a method", parentAndName.Position(fileName), Expression.Error);
                        ret = Expression.Error;
                        return false;
                    }
                }
                else {
                    ret = Expression.Error;
                    return false;
                }
            }
            else if (parentAndName is FBlangParser.LiteralExprContext litEx && litEx.literal().Ident() != null) {
                var _err = new ErrorBuffer();
                if (TryVisitEx(parentAndName, out var parent, expectedRetTy, _err)) {
                    ret = Module.Semantics.CreateCall(parentAndName.Position(fileName), expectedRetTy ?? Type.Top, parent.ReturnType, parent, null, args);
                    return !ret.IsError();
                }
                else {
                    var name = parentAndName.GetText();
                    var mets = (contextStack.Peek().MethodsByName(name) as IEnumerable<IDeclaredMethod>).Concat(contextStack.Peek().MethodTemplatesByName(name));
                    if (!mets.Any()) {
                        // _err.Flush();
                        err.ReportFrom(_err);
                        ret = Expression.Error;
                        return false;
                    }
                    else {
                        if (args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack)) {
                            ret = new CallExpression(parentAndName.Position(fileName), name, mets, null, args, expectedRetTy);
                            return true;
                        }
                        var met = Module.Semantics.BestFittingMethod(parentAndName.Position(fileName), mets, args.Select(x => x.MinimalType()).AsCollection(args.Length), expectedRetTy ?? Type.Top);
                        if (met.IsStatic()) {
                            ret = Module.Semantics.CreateCall(parentAndName.Position(fileName), met.ReturnType, met, null, args);
                            return !met.IsError();
                        }
                        else {
                            var tc = NextTypeContext();
                            if (tc is null) {
                                tc = (contextStack.Peek() as ITypeTemplateContext)?.TypeTemplate.BuildType()
                                    ?? err.Report($"The instance-method {met.Signature} cannot be called without explicit object in {contextStack.Peek().Name}", parentAndName.Position(fileName), Type.Error);
                            }
                            var parentEx = new ThisExpression(parentAndName.Position(fileName), tc);
                            ret = Module.Semantics.CreateCall(parentEx.Position, met.ReturnType, met, parentEx, args, isCallVirt: Module.Semantics.IsCallVirt(met, parentEx));
                            return !tc.IsError() && !met.IsError();
                        }
                    }
                }
            }
            else {
                //var parent = VisitEx(parentAndName);
                if (!TryVisitEx(parentAndName, out var parent, null, err)) {
                    ret = Expression.Error;
                    return false;
                }
                ret = Module.Semantics.CreateCall(parentAndName.Position(fileName), expectedRetTy ?? Type.Top, parent.ReturnType, parent, null, args);
                return !ret.IsError();
            }
        }
        bool TryGetCall(FBlangParser.ExContext parentAndName, IExpression[] args, IType expectedRetTy, out IExpression ret, ErrorBuffer err, string fileName) {
            var succ = TryGetCallInternal(parentAndName, args, expectedRetTy, out ret, err, fileName);
            if (succ && ret is CallExpression ce && !ce.IsEphemeral) {
                var formalArgs = ce.Callee.Arguments;
                for (int i = 0; i < formalArgs.Length; ++i) {
                    if (ce.Arguments[i] is LambdaExpression lambda && incompleteLambdas.TryGetValue(lambda, out var lambdaBody)) {
                        if (AnalyzeLambdaBody(lambdaBody.Item1, lambdaBody.Item2, lambda, formalArgs[i].Type, err)) {
                            incompleteLambdas.Remove(lambda);
                        }
                        else {
                            // error already reported
                            succ = false;
                        }
                    }
                }
                var callingCtx = contextStack.Peek();
                var memberDefCtx = ce.Callee.NestedIn;
                var minVis = VisibilityHelper.MinimumVisibility(memberDefCtx, callingCtx);
                if (ce.Callee.Visibility < minVis) {
                    $"The {ce.Callee.Visibility} method {ce.Callee.Signature} is not visible in {contextStack.Peek().Name}".Report(ce.Position);
                }
            }

            return succ;
        }
        IExpression GetCall(FBlangParser.ExContext parentAndName, IExpression[] args, IType expectedRetTy, string fileName) {
            TryGetCall(parentAndName, args, expectedRetTy, out var ret, null, fileName);
            return ret;
        }
        static IStatement GetRandomDeconstruction(Position pos, IEnumerable<IExpression> dest, IType randomDatasource) {
            //TODO overload static operator <-
            throw new NotImplementedException();
        }
        bool CheckReturnType(ref IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (expectedReturnType.IsTop() || ret.ReturnType.IsPrimitive(PrimitiveName.Null) && !expectedReturnType.IsValueType()) {
                return true;
            }
            if (expectedReturnType.IsAwaitable() && !ret.ReturnType.IsAwaitable()) {
                if (CheckReturnType(ref ret, (expectedReturnType as IWrapperType).ItemType, err)) {
                    if (!currentMethod.HasFlag(Method.Specifier.Coroutine))
                        ret = new CompletedTaskExpression(ret.Position, ret);
                    return true;
                }
                return false;
            }
            if (expectedReturnType != null && !expectedReturnType.IsTop() && !expectedReturnType.IsError() && !ret.IsError() && !Type.IsAssignable(ret.ReturnType, expectedReturnType)) {
                if (expectedReturnType.IsPrimitive(PrimitiveName.Bool)) {
                    if (ret.ReturnType.IsPrimitive()) {
                        ret = new TypecastExpression(ret.Position, ret, PrimitiveType.Bool);
                        return true;
                    }
                    else if (ret.ReturnType.OverloadsOperator(CompilerInfrastructure.Structure.OverloadableOperator.Bool, out var mets, SimpleMethodContext.VisibleMembers.Instance)) {

                        var callee = Module.Semantics.BestFittingMethod(ret.Position, mets, Array.Empty<IType>(), PrimitiveType.Bool, err);
                        ret = new CallExpression(ret.Position, PrimitiveType.Bool, callee, ret, Array.Empty<IExpression>());
                        return !callee.IsError();
                    }
                }
                else {
                    ret = err.Report($"The expression-type {ret.ReturnType.Signature} cannot be converted to {expectedReturnType.Signature}", ret.Position, Expression.Error);
                    return false;
                }
            }
            return !ret.IsError();
        }
        public bool TryVisitExpression([NotNull] FBlangParser.ExprContext context, out IExpression ret, IType expectedReturnType, ErrorBuffer err = null, IEnumerable<IType> expectedArgTypes = null) {
            bool succ;
            if (context.ex() != null) {
                succ = TryVisitEx(context.ex(), out ret, expectedReturnType, err, expectedArgTypes);
            }
            else {
                //assignExp
                succ = TryVisitAssignExp(context.assignExp(), out ret, expectedReturnType, err);
            }
            if (succ) {
                ret.NotNullableVars = nonNullableStack.Peek();
            }
            return succ;
        }
        bool TryVisitIndexerSet(FBlangParser.IndexerExprContext leftCtx, FBlangParser.ExprContext rightCtx, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitActualArglist(leftCtx.actualArglist(), out var idx, err)
                | !TryVisitEx(leftCtx.ex(), out var par, null, err)
                | !TryVisitExpression(rightCtx, out var rhs, expectedReturnType, err)) {
                ret = Expression.Error;
                return false;
            }
            var args = idx.Concat(new[] { rhs }).AsCollection(idx.Length + 1);
            ret = Module.Semantics.CreateIndexer(leftCtx.Position(fileName), PrimitiveType.Void, par.ReturnType, par, args, err);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        public bool TryVisitAssignExp([NotNull] FBlangParser.AssignExpContext context, out IExpression ret, IType expectedReturnType, ErrorBuffer err = null) {

            if (context.ex() is FBlangParser.IndexerExprContext indEx) {
                return TryVisitIndexerSet(indEx, context.expr(), out ret, expectedReturnType, err);
            }
            if (!TryVisitEx(context.ex(), out var lhs, expectedReturnType, err)) {
                //TODO lvalue-indexer
                ret = Expression.Error;
                return false;
            }
            if (!TryVisitExpression(context.expr(), out var rhs, lhs.ReturnType, err)) {
                ret = Expression.Error;
                return false;
            }
            if (context.assignOp().ModifierAssignOp() != null) {
                BinOp.OperatorKind op = BinOp.OperatorKind.ADD;
                switch (context.assignOp().ModifierAssignOp().GetText()) {
                    case "+=":
                        op = BinOp.OperatorKind.ADD;
                        break;
                    case "-=":
                        op = BinOp.OperatorKind.SUB;
                        break;
                    case "*=":
                        op = BinOp.OperatorKind.MUL;
                        break;
                    case "/=":
                        op = BinOp.OperatorKind.DIV;
                        break;
                    case "%=":
                        op = BinOp.OperatorKind.REM;
                        break;
                    case "|=":
                        op = BinOp.OperatorKind.OR;
                        break;
                    case "&=":
                        op = BinOp.OperatorKind.AND;
                        break;
                    case "^=":
                        op = BinOp.OperatorKind.XOR;
                        break;
                    case "<<=":
                        op = BinOp.OperatorKind.LSHIFT;
                        break;
                    case ">>=":
                        op = BinOp.OperatorKind.SRSHIFT;
                        break;
                    case ">>>=":
                        op = BinOp.OperatorKind.URSHIFT;
                        break;
                }
                rhs = Module.Semantics.CreateBinOp(context.assignOp().Position(fileName), null, lhs, op, rhs, err);
            }

            ret = Module.Semantics.CreateBinOp(context.Position(fileName), null, lhs, BinOp.OperatorKind.ASSIGN_NEW, rhs, err);
            if (!IsLValue(lhs)) {
                ret = Expression.Error;
                return err.Report("The destination of an assignment must have an address", context.Position(fileName), false);
            }
            if (lhs is VariableAccessExpression vre) {
                if (!rhs.IsNotNullable() && vre.Variable.IsNotNull()) {
                    return err.Report($"The nullable value {rhs} cannot be assigned to the not-nullable variable {vre.Variable.Signature}", context.Position(fileName), false);
                }
                UpdateNotNullSet(rhs.IsNotNullable(), vre.Variable);
            }
            return true;
        }
        public IExpression VisitAssignExp([NotNull] FBlangParser.AssignExpContext context, IType expectedReturnType) {
            if (TryVisitAssignExp(context, out var ret, expectedReturnType)) {
                return ret;
            }
            return Expression.Error;
        }
        bool IsLambdaCapture(IVariable vr, out LambdaCapture cap) {
            if (lambdaContextStack.TryPeek(out var blockCtx)) {

                if (blockCtx.captures.TryGetValue(vr, out cap))
                    return true;
                if (!FBSemantics.ContainVariable(vr, blockCtx.contextStack)) {
                    cap = new LambdaCapture(vr);
                    blockCtx.captures.Add(vr, cap);
                    return true;
                }

            }
            cap = default;
            return false;
        }
        ICaptureExpression GetCaptureAccessExpression(Position pos, IType retTy, IExpression orig, LambdaCapture cap, IExpression parentExpression = null, bool isBaseCapture = false) {
            if (lambdaContextStack.TryPeek(out var blockCtx)) {

                if (blockCtx.captureAccesses.TryGetValue((cap, parentExpression), out var acc))
                    return acc;
                acc = isBaseCapture
                        ? new BaseCaptureExpression(pos, retTy, cap, parentExpression)
                        : new CaptureAccessExpression(pos, orig, cap, parentExpression);
                blockCtx.captureAccesses.Add((cap, parentExpression), acc);
                return acc;
            }
            else {
                // ERROR ? 
                throw new InvalidOperationException();
            }
        }
        public bool TryVisitEx([NotNull] FBlangParser.ExContext context, out IExpression ret, IType expectedReturnType, ErrorBuffer err = null, IEnumerable<IType> expectedArgTypes = null) {
            switch (context) {
                case FBlangParser.SubExprContext ex:
                    return TryVisitExpression(ex.expr(), out ret, expectedReturnType, err);
                case FBlangParser.ArrInitializerExprContext ex:
                    return TryVisitArrInitializerExpr(ex, out ret, expectedReturnType, err, expectedArgTypes);
                case FBlangParser.LiteralExprContext ex:
                    return TryVisitLiteralExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.CallExprContext ex:
                    return TryVisitCallExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.InternalExternalCallExprContext ex:
                    return TryVisitInternalExternalCallExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.IndexerExprContext ex:
                    return TryVisitIndexerExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.SliceExprContext ex:
                    return TryVisitSliceExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.TypecastExprContext ex:
                    return TryVisitTypecastExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.AsTypeExprContext ex:
                    return TryVisitAsTypeExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.PreUnopExprContext ex:
                    return TryVisitPreUnOpExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.PostUnopExprContext ex:
                    return TryVisitPostUnOpExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.NewObjExprContext ex:
                    return TryVisitNewObjExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.NewArrExprContext ex:
                    return TryVisitNewArrExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.MemberAccessExprContext ex:
                    return TryVisitMemberAccessExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.MulExprContext ex:
                    return TryVisitMulExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.AddExprContext ex:
                    return TryVisitAddExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.ShiftExprContext ex:
                    return TryVisitShiftExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.OrderExprContext ex:
                    return TryVisitOrderExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.IsTypeExprContext ex:
                    return TryVisitIsTypeExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.InstanceofExprContext ex:
                    return TryVisitInstanceofExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.EqualsExprContext ex:
                    return TryVisitEqualsExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.AndExprContext ex:
                    return TryVisitAndExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.XorExprContext ex:
                    return TryVisitXorExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.OrExprContext ex:
                    return TryVisitOrExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.LandExprContext ex:
                    return TryVisitLAndExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.LorExprContext ex:
                    return TryVisitLOrExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.ConditionalExprContext ex:
                    return TryVisitConditionalExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.ContiguousRangeExprContext ex:
                    return TryVisitContiguousRangeExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.ThisExprContext ex: {
                    var tp = NextTypeContext();
                    if (tp is null) {
                        ret = err.Report("The 'this' expression cannot be used outside of a type", context.Position(fileName), Expression.Error);
                        return false;
                    }
                    else if (currentMethod.HasFlag(Method.Specifier.Static)) {
                        ret = err.Report("The 'this' expression cannot be used in a static context", context.Position(fileName), Expression.Error);
                        return false;
                    }
                    if (lambdaContextStack.TryPeek(out var blockCtx)) {
                        ret = GetCaptureAccessExpression(context.Position(fileName), tp, null, LambdaCapture.This(tp));
                    }
                    else
                        ret = new ThisExpression(context.Position(fileName), tp);
                    return true;
                }
                case FBlangParser.SuperExprContext ex: {
                    var tp = NextTypeContext();
                    IHierarchialType htp;
                    if (tp is null) {
                        ret = err.Report("The 'super' expression cannot be used outside of a type", context.Position(fileName), Expression.Error);
                        return false;
                    }
                    else if (currentMethod.HasFlag(Method.Specifier.Static)) {
                        ret = err.Report("The 'super' expression cannot be used in a static context", context.Position(fileName), Expression.Error);
                        return false;
                    }
                    else if (!(tp is IHierarchialType) || (htp = tp as IHierarchialType).SuperType == null) {
                        ret = err.Report($"The 'super' expression is not available in the type {tp.Signature}, because it has no superclass", context.Position(fileName), Expression.Error);
                        return false;
                    }
                    if (lambdaContextStack.TryPeek(out var blockCtx)) {
                        ret = GetCaptureAccessExpression(context.Position(fileName), htp.SuperType, null, LambdaCapture.Base(tp), isBaseCapture: true);
                    }
                    else
                        ret = new BaseExpression(context.Position(fileName), htp.SuperType);
                    return true;
                }
                case FBlangParser.LambdaExprContext ex: {
                    var names = ex.names() != null ? Array.ConvertAll(ex.names().Ident(), x => x.GetText()) : Array.Empty<string>();
                    return TryVisitLambdaExpr(ex.expr(), ex.blockInstruction(), names, out ret, expectedReturnType, err);
                }
                case FBlangParser.LambdaFnExprContext ex:
                    return TryVisitLambdaExpr(ex.expr(), ex.blockInstruction(), new[] { ex.Ident().GetText() }, out ret, expectedReturnType, err);
                case FBlangParser.ConcurrentForExprContext ex:
                    ret = VisitConcurrentForLoop(ex.concForeachLoop());
                    return !ret.IsError();
                case FBlangParser.ReduceExprContext ex:
                    return TryVisitReduceExpr(ex, out ret, expectedReturnType, err);
                case FBlangParser.DeclExprContext ex:
                    return TryVisitDeclExpr(ex, out ret, err);

            }
            ret = err.Report("Unknown expression", context.Position(fileName), Expression.Error);
            return false;
        }

        private bool TryVisitDeclExpr(FBlangParser.DeclExprContext ex, out IExpression ret, ErrorBuffer err) {
            var name = ex.localVarName().GetText();
            var vis = ex.Public() != null ? Visibility.Public : Visibility.Private;
            var initializer = VisitEx(ex.ex());
            var varTy = initializer.ReturnType;
            var vr = new BasicVariable(ex.Position(fileName), varTy, Variable.Specifier.LocalVariable, name, null, vis) { DefaultValue = initializer };
            DoDefineVariables(vr);
            ret = new DeclarationExpression(ex.Position(fileName), vr);
            return true;
        }

        private bool TryVisitReduceExpr(FBlangParser.ReduceExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            bool succ = true;
            succ &= TryVisitExpression(ex.collection, out var dataSource, null, err);
            IExpression seed;
            if (ex.seed != null)
                succ &= TryVisitExpression(ex.seed, out seed, expectedReturnType, err);
            else
                seed = null;
            IMethod met = null;
            BinOp.OperatorKind op = default;
            if (succ) {
                succ &= TryVisitReduction(ex.reduction(), seed, dataSource, out met, out op, expectedReturnType, err);
            }
            ret = succ ? new ReduceExpression(ex.Position(fileName), dataSource, seed, op, met, ex.Reduce() is null) : Expression.Error;
            return succ;
        }

        private bool TryVisitReduction(FBlangParser.ReductionContext context, IExpression seed, IExpression dataSource, out IMethod met, out BinOp.OperatorKind op, IType expectedReturnType, ErrorBuffer err) {
            if (context.Ident() != null) {
                op = default;
                var name = context.Ident().GetText();
                var mets = contextStack.Peek().MethodsByName(name).Where(x => x.Arguments.Length == 2 && x.IsStatic() && x.ReturnType == x.Arguments[0].Type);

                if (mets.Any()) {
                    if (!mets.HasCount(2)) {
                        met = mets.First();
                        return true;
                    }
                    mets = mets.Where(x => dataSource.ReturnType.UnWrap().IsSubTypeOf(x.Arguments[0].Type.UnWrap()));
                    if (mets.Any()) {
                        if (!mets.HasCount(2)) {
                            met = mets.First();
                            return true;
                        }
                        if (seed != null) {
                            mets = mets.Where(x => seed.ReturnType.UnWrap().IsSubTypeOf(x.Arguments[0].Type.UnWrap()));
                            if (mets.Any()) {
                                if (!mets.HasCount(2)) {
                                    met = mets.First();
                                    return true;
                                }
                                met = Method.Error;
                                return err.Report($"The reduction-function is ambiguous. Possible reductions are: {string.Join(", ", mets.Select(x => x.Signature))}", context.Position(fileName), false);
                            }
                            met = Method.Error;
                            return err.Report($"There are not reduction-functions with the name {name} and the argument-types {seed.ReturnType.Signature} and {dataSource.ReturnType.Signature}", context.Position(fileName), false);
                        }
                        met = Method.Error;
                        return err.Report($"The reduction-function is ambiguous. Possible reductions are: {string.Join(", ", mets.Select(x => x.Signature))}", context.Position(fileName), false);
                    }
                    else {
                        met = Method.Error;
                        return err.Report($"There are no reduction-functions with the name {name} and the first argument of type {seed.ReturnType.Signature}", context.Position(fileName), false);
                    }

                }
                else {
                    met = Method.Error;
                    return err.Report($"There are no compatible reduction-functions with the name {name} in {contextStack.Peek().Name}", context.Position(fileName), false);
                }
            }
            else if (context.reductionOperator() != null) {
                met = null;
                var opTok = context.reductionOperator();
                if (opTok.Minus() != null) {
                    op = BinOp.OperatorKind.SUB;
                }
                else if (opTok.Pointer() != null) {
                    op = BinOp.OperatorKind.MUL;
                }
                else if (opTok.Div() != null) {
                    op = BinOp.OperatorKind.DIV;
                }
                else if (opTok.Percent() != null) {
                    op = BinOp.OperatorKind.REM;
                }
                else if (opTok.Amp() != null) {
                    op = BinOp.OperatorKind.AND;
                }
                else if (opTok.Xor() != null) {
                    op = BinOp.OperatorKind.XOR;
                }
                else {
                    var txt = opTok.GetText();
                    if (txt == "+") {
                        op = BinOp.OperatorKind.ADD;
                    }
                    else if (txt == "|") {
                        op = BinOp.OperatorKind.OR;
                    }
                    else {
                        op = default;
                        return err.Report("Fatal error: invalid grammar", context.Position(fileName), false);
                    }
                }
                return true;
            }
            else {
                met = null;
                op = default;
                return err.Report("Fatal error: invalid grammar", context.Position(fileName), false);
            }
        }

        private bool TryVisitSliceExpr(FBlangParser.SliceExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            bool succ = true;

            succ &= TryVisitEx(ex.ex(), out var parent, null, err);
            IExpression offset, count;
            if (ex.offset != null)
                succ &= TryVisitExpression(ex.offset, out offset, PrimitiveType.UInt, err);
            else
                offset = Literal.UInt(0);

            if (ex.count != null)
                succ &= TryVisitExpression(ex.count, out count, PrimitiveType.UInt, err);
            else
                count = CreateVariableAccessExpression(ex.Position(fileName), PrimitiveType.UInt, parent.ReturnType.Context.TryGetVariableByName("length", ex.Position(fileName)), parent);
            var parentTy = parent.ReturnType is ModifierType mt ? mt.UnderlyingType : parent.ReturnType;
            if (parentTy.UnWrap() is AggregateType agg)
                ret = new RangedIndexerExpression(ex.Position(fileName), agg.ItemType.AsSpan(), parent, offset, count);
            else {
                ret = Expression.Error;
                succ = succ && err.Report($"Only Arrays, Slices and Varargs can be sliced, not {parentTy}", ex.Position(fileName), false);
            }
            return succ;
        }

        private bool TryVisitContiguousRangeExpr(FBlangParser.ContiguousRangeExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            bool succ = true;
            var fromTo = ex.ex();
            succ &= TryVisitEx(fromTo[0], out var from, null);
            succ &= TryVisitEx(fromTo[1], out var to, null);
            if (!succ) {
                ret = Expression.Error;
                return false;
            }
            ret = new ContiguousRangeExpression(ex.Position(fileName), null, from, to);
            return succ;
        }

        private bool TryInferLambdaParameters(Position pos, string[] names, out IType retTy, out FunctionType retFnTy, IType expectedLambdaType, ErrorBuffer err) {
            //TODO infer lambda-function signature
            if (expectedLambdaType != null) {
                if (Module.Semantics.IsFunctional(expectedLambdaType, out retFnTy)) {
                    retTy = expectedLambdaType;
                    //args = retFnTy.ArgumentTypes.Select(x => new BasicVariable(pos, x, Variable.Specifier.FormalArgument, "arg", null)).ToArray();
                    if (retFnTy.ArgumentTypes.Count == names.Length) {
                        return true;
                    }
                    else {
                        return $"A lambda-expression with {names.Length} parameter(s) cannot be converted to {expectedLambdaType.Signature}, because it has {Math.Abs(names.Length - retFnTy.ArgumentTypes.Count)} too {(names.Length > retFnTy.ArgumentTypes.Count ? "many" : "few")} arguments".Report(pos, false);
                    }
                }
                else {
                    //args = Array.Empty<IVariable>();
                    retTy = Type.Error;
                    return $"A lambda-expression cannot be converted to {expectedLambdaType.Signature}".Report(pos, false);
                }
            }
            else {
                //args = //Enumerable.Range(0, names.Length).Select(x => new BasicVariable(pos, Type.Top, Variable.Specifier.FormalArgument, "arg" + x, null)).ToArray();
                // Array.Empty<IVariable>();
                retTy = retFnTy = new FunctionType(pos, Module, "lambda", Type.Top, Collection.Repeat(Type.Top, names.Length), Visibility.Internal);
                return true;
            }
        }
        private bool AnalyzeLambdaBody(FBlangParser.ExprContext context, FBlangParser.BlockInstructionContext biContext, LambdaExpression lambda, IType expectedReturnType, ErrorBuffer err) {
            bool succ = true;
            var pos = context?.Position(fileName) ?? biContext.Position(fileName);
            var ctx = lambda.Context;
            succ &= Module.Semantics.IsFunctional(expectedReturnType, out var retFnTy);
            int index = 0;
            var args = retFnTy.ArgumentTypes.Select(x => new BasicVariable(pos, x, Variable.Specifier.FormalArgument, lambda.ArgumentNames[index++], null)).ToArray();
            lambda.Arguments = args;
            succ &= lambda.TryResetReturnType(expectedReturnType);
            foreach (var arg in args) {
                if (!ctx.DefineVariable(arg).Get(out var fail)) {
                    if (fail == CannotDefine.AlreadyExisting)
                        succ = $"The lambda-argument {arg.Signature.Name} is already defined".Report(pos, false);
                    else
                        succ = $"The lambda-argument {arg.Signature.Name} cannot be defined".Report(pos, false);
                }
            }
            var lambdaBodyBlockCtx = new LambdaBlockContext();
            //lambdaBodyCtxStack.Push(ctx);
            using (lambdaContextStack.PushFrame(lambdaBodyBlockCtx)) {
                using (PushContext(ctx)) {

                    if (context != null) {

                        succ &= TryVisitExpression(context, out var body, retFnTy.ReturnType, err);
                        if (body.ReturnType.IsPrimitive(PrimitiveName.Void)) {
                            //TODO assert, that body is a valid ExpressionStmt
                            lambda.Body.Instruction = new ExpressionStmt(pos, body);
                        }
                        else {
                            lambda.Body.Instruction = new ReturnStatement(pos, body);
                        }
                    }
                    else {
                        var body = VisitBlockInstruction(biContext);
                        lambda.Body.Instruction = body;
                    }
                }
            }

            lambda.Captures = lambdaBodyBlockCtx.captures.Values;
            lambda.CaptureAccesses = lambdaBodyBlockCtx.captureAccesses.Values;
            return succ;
        }
        private bool TryVisitLambdaExpr(FBlangParser.ExprContext context, FBlangParser.BlockInstructionContext biContext, string[] names, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            bool succ = true;
            if (context is null && biContext is null) {
                ret = Expression.Error;
                // Wird wahrscheinlich nicht auftreten
                return false;
            }
            var pos = context != null ? context.Position(fileName) : biContext.Position(fileName);
            succ &= TryInferLambdaParameters(pos, names, /*out var args, */out var retTy, out var retFnTy, expectedReturnType, err);
            var ctx = contextStack.Peek().NewScope(Context.DefiningRules.Variables);

            var lambda = new LambdaExpression(pos, retTy, ctx, names) /*{ Arguments = args }*/;

            if (expectedReturnType != null) {
                succ &= AnalyzeLambdaBody(context, biContext, lambda, retTy, err);
            }
            else {
                incompleteLambdas.Add(lambda, (context, biContext));
            }
            ret = lambda;
            return succ;
        }

        private bool TryVisitConditionalExpr(FBlangParser.ConditionalExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            var subExprs = ex.ex();
            if (!TryVisitEx(subExprs[0], out var cond, PrimitiveType.Bool, err)
                || !TryVisitEx(subExprs[1], out var thenCase, expectedReturnType, err)
                || !TryVisitEx(subExprs[2], out var elseCase, expectedReturnType, err)) {
                ret = Expression.Error;
                return false;
            }
            ret = new ConditionalExpression(ex.Position(fileName), null, cond, thenCase, elseCase);
            return true;
        }
        private bool TryVisitBinOp(FBlangParser.ExContext[] exs, BinOp.OperatorKind op, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitEx(exs[0], out var lhs, null, err)
                || !TryVisitEx(exs[1], out var rhs, null, err)) {
                ret = Expression.Error;
                return false;
            }
            ret = Module.Semantics.CreateBinOp(lhs.Position, null, lhs, op, rhs, err);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        private bool TryVisitUnOp(FBlangParser.ExContext ex, UnOp.OperatorKind op, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            IType subTy = null;
            switch (op) {
                case UnOp.OperatorKind.LNOT:
                    subTy = PrimitiveType.Bool;
                    break;
                case UnOp.OperatorKind.AWAIT:
                    if (expectedReturnType != null && !expectedReturnType.IsError())
                        subTy = expectedReturnType.AsAwaitable();
                    break;
            }

            if (op == UnOp.OperatorKind.UNPACK && currentMacro != null && currentMacro.HasVarArgs && currentMacro.VarArgs.Name == ex.GetText()) {
                ret = new ExpressionParameterPackUnpack(ex.Position(fileName), currentMacro.VarArgs);
                if (expectedReturnType != null && !expectedReturnType.IsTop())
                    currentMacro.VarArgs.Constraints.Add(new[] { new ExpressionParameter.Constraint(expectedReturnType, false) });
                return true;
            }
            if (!TryVisitEx(ex, out var subEx, subTy, err)) {
                ret = Expression.Error;
                return false;
            }

            ret = Module.Semantics.CreateUnOp(ex.Position(fileName), null, op, subEx, err);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        private bool TryVisitComparisonBinOp(FBlangParser.ExContext[] exs, BinOp.OperatorKind op, out IExpression ret, ErrorBuffer err) {
            return TryVisitBinOp(exs, op, out ret, PrimitiveType.Bool, err);
        }
        private bool TryVisitLOrExpr(FBlangParser.LorExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitBinOp(ex.ex(), BinOp.OperatorKind.LOR, out ret, null, err);
        }
        private bool TryVisitLAndExpr(FBlangParser.LandExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitBinOp(ex.ex(), BinOp.OperatorKind.LAND, out ret, expectedReturnType, err);
        }

        private bool TryVisitOrExpr(FBlangParser.OrExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitBinOp(ex.ex(), BinOp.OperatorKind.OR, out ret, expectedReturnType, err);
        }
        private bool TryVisitXorExpr(FBlangParser.XorExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitBinOp(ex.ex(), BinOp.OperatorKind.XOR, out ret, expectedReturnType, err);
        }

        private bool TryVisitAndExpr(FBlangParser.AndExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitBinOp(ex.ex(), BinOp.OperatorKind.AND, out ret, expectedReturnType, err);
        }
        private bool TryVisitEqualsExpr(FBlangParser.EqualsExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            BinOp.OperatorKind op;
            if (ex.Equals() != null) {
                op = BinOp.OperatorKind.EQ;
            }
            else {
                op = BinOp.OperatorKind.NEQ;
            }
            return TryVisitComparisonBinOp(ex.ex(), op, out ret, err);
        }
        private bool TryVisitInstanceofExpr(FBlangParser.InstanceofExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitEx(ex.ex(), out var inst, null, err)
                | !TryVisitTypeIdent(ex.typeIdent(), out var tp, err, fileName)) {
                ret = Expression.Error;
                return false;
            }
            ret = new InstanceofExpression(ex.Position(fileName), inst, tp);
            return true;
        }

        private bool TryVisitIsTypeExpr(FBlangParser.IsTypeExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitTypeIdent(ex.lhs, out var lhs, err, fileName)
                 | !TryVisitTypeIdent(ex.rhs, out var rhs, err, fileName)) {
                ret = Expression.Error;
                return false;
            }
            ret = new IsTypeExpression(ex.Position(fileName), lhs, rhs);
            return true;
        }
        static BinOp.OperatorKind GetOrderOp(FBlangParser.OrderOpContext context) {
            if (context.LT() != null)
                return BinOp.OperatorKind.LT;
            else if (context.LE() != null)
                return BinOp.OperatorKind.LE;
            else if (context.GE() != null)
                return BinOp.OperatorKind.GE;
            else if (context.GT() != null)
                return BinOp.OperatorKind.GT;
            else
                throw new ArgumentException();
        }
        private bool TryVisitOrderExpr(FBlangParser.OrderExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            /*BinOp.OperatorKind op;
            if (ex.LT() != null) {
                op = BinOp.OperatorKind.LT;
            }
            else if (ex.LE() != null) {
                op = BinOp.OperatorKind.LE;
            }
            else if (ex.GE() != null) {
                op = BinOp.OperatorKind.GE;
            }
            else {
                op = BinOp.OperatorKind.GT;
            }
            return TryVisitComparisonBinOp(ex.ex(), op, out ret, err);*/
            var exs = new Stack<FBlangParser.ExContext>();
            var orderOps = new Stack<BinOp.OperatorKind>();
            exs.Push(ex.ex()[1]);
            orderOps.Push(GetOrderOp(ex.orderOp()));
            {
                FBlangParser.ExContext e;
                for (e = ex.ex()[0]; e is FBlangParser.OrderExprContext oec; e = oec.ex()[0]) {
                    exs.Push(oec.ex()[1]);
                    orderOps.Push(GetOrderOp(oec.orderOp()));
                }
                exs.Push(e);
            }


            bool succ = TryVisitEx(exs.Pop(), out var lhs, null, err)
                & TryVisitEx(exs.Pop(), out var rhs, null, err);


            rhs = rhs.IsError() || !exs.Any() ? rhs : new DeclarationExpression(lhs.Position,
                new BasicVariable(rhs.Position, rhs.ReturnType, Variable.Specifier.LocalVariable | Variable.Specifier.Final, ":tmp", null) {
                    DefaultValue = rhs
                });
            ret = Module.Semantics.CreateBinOp(lhs.Position, PrimitiveType.Bool, lhs, orderOps.Pop(), rhs, err);
            succ &= CheckReturnType(ref ret, PrimitiveType.Bool, err);

            foreach (var x in exs) {
                lhs = rhs;
                succ &= TryVisitEx(x, out rhs, null, err);
                rhs = rhs.IsError() ? rhs : new DeclarationExpression(lhs.Position,
                new BasicVariable(rhs.Position, rhs.ReturnType, Variable.Specifier.LocalVariable | Variable.Specifier.Final, ":tmp", null) {
                    DefaultValue = rhs
                });
                var curr = Module.Semantics.CreateBinOp(lhs.Position, PrimitiveType.Bool, lhs, orderOps.Pop(), rhs, err);
                succ &= CheckReturnType(ref curr, PrimitiveType.Bool, err);
                ret = new BinOp(ret.Position, PrimitiveType.Bool, ret, BinOp.OperatorKind.LAND, curr);
            }
            return succ;
        }
        private bool TryVisitShiftExpr(FBlangParser.ShiftExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            BinOp.OperatorKind op;
            if (ex.LShift() != null)
                op = BinOp.OperatorKind.LSHIFT;
            else if (ex.srShift() != null)
                op = BinOp.OperatorKind.SRSHIFT;
            else
                op = BinOp.OperatorKind.URSHIFT;
            return TryVisitBinOp(ex.ex(), op, out ret, expectedReturnType, err);
        }
        private bool TryVisitAddExpr(FBlangParser.AddExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            var op = ex.Minus() != null ? BinOp.OperatorKind.SUB : BinOp.OperatorKind.ADD;

            return TryVisitBinOp(ex.ex(), op, out ret, expectedReturnType, err);
        }
        private bool TryVisitMulExpr(FBlangParser.MulExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            BinOp.OperatorKind op;
            if (ex.Pointer() != null)
                op = BinOp.OperatorKind.MUL;
            else if (ex.Div() != null)
                op = BinOp.OperatorKind.DIV;
            else
                op = BinOp.OperatorKind.REM;
            return TryVisitBinOp(ex.ex(), op, out ret, expectedReturnType, err);
        }
        private bool TryVisitMemberAccessExpr(FBlangParser.MemberAccessExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return VisitMemberAccessExpr(ex, out ret, out _, out _, out _, expectedReturnType, err) && ret != null;
        }

        private bool TryVisitNewArrExpr(FBlangParser.NewArrExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitActualArglist(ex.actualArglist(), out var dim, err)) {
                ret = Expression.Error;
                return false;
            }
            var num = dim.FirstOrDefault(x => !x.ReturnType.IsNumericType());
            if (num != null) {
                err.Report("The lengths of all array-dimensions must be numeric values", num.Position);
                ret = Expression.Error;
                return false;
            }
            IType itemType = null;
            if (ex.typeIdent() != null) {
                if (!TryVisitTypeIdent(ex.typeIdent(), out itemType, err, fileName)) {
                    ret = Expression.Error;
                    return false;
                }
            }
            else {
                if (expectedReturnType is null || expectedReturnType.IsTop()) {
                    err.Report("The type for the array-elements cannot be inferred and therefor must be specified explicitly", ex.Position(fileName));
                    ret = Expression.Error;
                    return false;
                }
                else if (!expectedReturnType.IsArray()) {
                    err.Report($"No array-type can be assigned to the non-array type {expectedReturnType}");
                    ret = Expression.Error;
                    return false;
                }
                else
                    itemType = expectedReturnType.Cast<ArrayType>().ItemType;
            }
            ret = new NewArrayExpression(ex.Position(fileName), itemType, dim[0]);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        private bool TryVisitNewObjExpr(FBlangParser.NewObjExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            IExpression[] args;
            if (ex.actualArglist() != null) {
                if (!TryVisitActualArglist(ex.actualArglist(), out args, err)) {
                    ret = Expression.Error;
                    return false;
                }
            }
            else {
                args = Array.Empty<IExpression>();
            }

            if (ex.typeIdent() != null) {
                if (!TryVisitTypeIdent(ex.typeIdent(), out expectedReturnType, err, fileName)) {
                    ret = Expression.Error;
                    return false;
                }
            }
            else {
                if (expectedReturnType is null || expectedReturnType.IsTop()) {
                    err.Report("The type for the instantiated object cannot be inferred and therefore must be specified explicitly", ex.Position(fileName));
                    ret = Expression.Error;
                    return false;
                }
                else
                    expectedReturnType = expectedReturnType.UnWrapNatural();
            }
            ret = Module.CreateNewObjectExpression(ex.Position(fileName), expectedReturnType, args, err);

            return !ret.IsError();
        }
        private bool TryVisitPostUnOpExpr(FBlangParser.PostUnopExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            UnOp.OperatorKind op;
            if (ex.Dots() != null)
                op = UnOp.OperatorKind.UNPACK;
            else if (ex.IncDec().GetText() == "++")
                op = UnOp.OperatorKind.INCR_POST;
            else
                op = UnOp.OperatorKind.DECR_POST;
            return TryVisitUnOp(ex.ex(), op, out ret, expectedReturnType, err);
        }

        private bool TryVisitPreUnOpExpr(FBlangParser.PreUnopExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            UnOp.OperatorKind op;
            if (ex.Minus() != null)
                op = UnOp.OperatorKind.NEG;
            else if (ex.Tilde() != null)
                op = UnOp.OperatorKind.NOT;
            else if (ex.ExclamationMark() != null)
                op = UnOp.OperatorKind.LNOT;
            else if (ex.Defer() != null) {
                // async ( <=> Task.Run(..))
                IType taskType = expectedReturnType.IsAwaitable() ? (expectedReturnType as IWrapperType).ItemType : null;
                var ctx = contextStack.Peek().NewScope(Context.DefiningRules.Variables);
                var lctx = new LambdaBlockContext();
                using (lambdaContextStack.PushFrame(lctx)) {
                    using (PushContext(ctx)) {
                        if (!TryVisitEx(ex.ex(), out var task, taskType, err)) {
                            ret = Expression.Error;
                            return false;
                        }
                        ret = new RunAsyncExpression(ex.Position(fileName), task) { Captures = lctx.captures.Values, CaptureAccesses = lctx.captureAccesses.Values };
                    }
                }


                return true;
            }
            else if (ex.Await() != null)
                op = UnOp.OperatorKind.AWAIT;
            else {
                if (ex.IncDec().GetText() == "++")
                    op = UnOp.OperatorKind.INCR_PRE;
                else
                    op = UnOp.OperatorKind.DECR_PRE;
            }
            return TryVisitUnOp(ex.ex(), op, out ret, expectedReturnType, err);
        }
        private bool TryVisitTypecastExpr(FBlangParser.TypecastExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {

            if (!TryVisitLocalTypeIdent(ex.localTypeIdent(), out var tp, err, fileName)
                || !TryVisitEx(ex.ex(), out var sub, tp, err)) {
                ret = Expression.Error;
                return false;
            }
            ret = sub.ReturnType == tp ? sub : new TypecastExpression(ex.Position(fileName), sub, tp);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        private bool TryVisitAsTypeExpr(FBlangParser.AsTypeExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitLocalTypeIdent(ex.localTypeIdent(), out var tp, err, fileName)
                || !TryVisitEx(ex.ex(), out var sub, null, err)) {
                // im Gegensatz zu TypecastExpr, kein expectedReturnType für subExpression
                ret = Expression.Error;
                return false;
            }
            if (Type.IsAssignable(sub.ReturnType, tp) || Type.IsAssignable(tp, sub.ReturnType))
                ret = new TypecastExpression(ex.Position(fileName), sub, tp, false);
            else
                ret = new DefaultValueExpression(ex.Position(fileName), tp);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }
        /// <summary>
        /// Visits RVALUE-Indexer expressions
        /// </summary>
        private bool TryVisitIndexerExpr(FBlangParser.IndexerExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            if (!TryVisitActualArglist(ex.actualArglist(), out var idx, err)
                | !TryVisitEx(ex.ex(), out var par, null, err)) {
                ret = Expression.Error;
                return false;
            }
            ret = Module.Semantics.CreateIndexer(ex.Position(fileName), expectedReturnType, par.ReturnType, par, idx, err);
            return CheckReturnType(ref ret, expectedReturnType, err);
        }

        private bool TryVisitInternalExternalCallExpr(FBlangParser.InternalExternalCallExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            var name = ex.Ident().GetText();
            IExpression[] args;
            if (ex.actualArglist() is null)
                args = Array.Empty<IExpression>();
            else if (!TryVisitActualArglist(ex.actualArglist(), out args, err)) {
                ret = Expression.Error;
                return false;
            }
            return TryGetInternalExternalCall(ex.Position(fileName), name, args, ex.InternalCall() != null, expectedReturnType, out ret, err);
        }
        private bool TryVisitCallExpr(FBlangParser.CallExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            IExpression[] args;
            if (ex.actualArglist() is null)
                args = Array.Empty<IExpression>();
            else if (!TryVisitActualArglist(ex.actualArglist(), out args, err)) {
                ret = Expression.Error;
                return false;
            }
            return TryGetCall(ex.ex(), args, expectedReturnType, out ret, err, fileName);
        }
        private bool TryVisitLiteralExpr(FBlangParser.LiteralExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err) {
            return TryVisitLiteral(ex.literal(), out ret, expectedReturnType, err, fileName);
        }

        protected override bool TryVisitLiteral(FBlangParser.LiteralContext lit, out IExpression ret, IType expectedReturnType, ErrorBuffer err, string fileName) {
            var _err = new ErrorBuffer();
            if (lit.IntLit() != null) {
                ret = IntLiteral(lit.Position(fileName), lit.IntLit().GetText(), lit.Minus() != null, _err);
            }
            else if (lit.FloatLit() != null) {
                ret = FloatLiteral(lit.Position(fileName), lit.FloatLit().GetText(), lit.Minus() != null, _err);
            }
            else if (lit.CharLit() != null) {
                ret = Literal.SolveCharEscapes(lit.Position(fileName), lit.CharLit().GetText(), 1, lit.CharLit().GetText().Length - 2);
            }
            else if (lit.StringLit() != null) {
                ret = Literal.SolveStringEscapes(lit.Position(fileName), lit.StringLit().GetText(), 1, lit.StringLit().GetText().Length - 2);
            }
            else if (lit.BoolLit() != null) {
                ret = lit.BoolLit().GetText() == "true" ? Literal.True : Literal.False;
            }
            else if (lit.Null() != null) {
                ret = Literal.Null;
            }
            else if (lit.Default() != null) {
                //TODO infer default-type
                if (expectedReturnType.IsByRef())
                    "ByRef types do not have a default value".Report(lit.Position(fileName));
                ret = new DefaultValueExpression(lit.Position(fileName), expectedReturnType ?? Type.Top);
            }
            else if (lit.MacroLocalIdent() != null) {
                if (currentMacro != null) {
                    var name = lit.MacroLocalIdent().GetText();
                    var vr = currentMacro.LocalContext.TryGetVariableByName(name, lit.Position(fileName));
                    if (vr != null) {
                        ret = CreateVariableAccessExpression(lit.Position(fileName), null, vr);
                        if (IsLambdaCapture(vr, out var cap))
                            ret = GetCaptureAccessExpression(lit.Position(fileName), null, ret, cap);
                    }
                    else {
                        ret = _err.Report($"A macro-local variable {name} is not defined in {currentMacro.Name}", lit.Position(fileName), Expression.Error);
                    }
                }
                else {
                    ret = _err.Report("A macro-local variable cannot be used outsode of a macro", lit.Position(fileName), Expression.Error);
                }
            }
            else {
                //Ident
                var vr= FBSemantics.Instance.BestFittingVariable(lit.Position(fileName),
                                                         contextStack.Peek().VariablesByName(lit.Ident().GetText()),
                                                         expectedReturnType);
                // var vr = contextStack.Peek().TryGetVariableByName(lit.Ident().GetText(), lit.Position(fileName));
                if (vr != null && !vr.IsError()) {

                    if (vr.IsStatic() || !vr.IsStatic() && vr.IsLocalVariable()) {
                        ret = CreateVariableAccessExpression(lit.Position(fileName), null, vr);
                        if (IsLambdaCapture(vr, out var cap))
                            ret = GetCaptureAccessExpression(lit.Position(fileName), null, ret, cap);
                    }
                    else {
                        var inst = NextTypeContext();
                        if (inst != null)
                            ret = InstanceVariableWithImplicitThis(lit.Position(fileName), vr, inst, _err);
                        else
                            ret = _err.Report($"Cannot access instance-variable {vr.Signature} from static context", lit.Position(fileName), Expression.Error);
                    }
                }
                else if (currentMacro != null) {
                    var name = lit.Ident().GetText();

                    if (currentMacro.NamedArguments.TryGetValue(name, out var exParam))
                        ret = new ExpressionParameterAccess(lit.Ident().Position(fileName), exParam);
                    else
                        ret = _err.Report($"A variable or method with the name '{lit.Ident().GetText()}' is not defined in {currentMacro.NestedIn.Name}", lit.Position(fileName), Expression.Error);

                }
                else {
                    ret = _err.Report($"A variable or method with the name '{lit.Ident().GetText()}' is not defined in {contextStack.Peek().Name}", lit.Position(fileName), Expression.Error);
                }
            }
            if (_err.Any()) {
                err.ReportFrom(_err);
                ret = Expression.Error;
                return false;
            }
            return true;
        }

        private bool TryVisitArrInitializerExpr(FBlangParser.ArrInitializerExprContext ex, out IExpression ret, IType expectedReturnType, ErrorBuffer err, IEnumerable<IType> expectedArgTypes = null) {
            if (!TryVisitActualArglist(ex.actualArglist(), out var args, err)) {
                ret = Expression.Error;
                return false;
            }
            var retTy = ArrayInitializerExpression.InferredReturnType(ex.Position(fileName), args) as IWrapperType;
            if (expectedReturnType.UnWrap() is AggregateType expAgg) {
                if (retTy.ItemType != expAgg.ItemType && retTy.ItemType.IsSubTypeOf(expAgg.ItemType)) {
                    // implicit cast
                    retTy = expAgg;
                }
            }
            ret = new ArrayInitializerExpression(ex.Position(fileName), retTy, args);

            return CheckReturnType(ref ret, expectedReturnType, err);
        }

        public IExpression VisitEx([NotNull] FBlangParser.ExContext context, IType expectedReturnType = null) {
            if (TryVisitEx(context, out var ret, expectedReturnType)) {
                return ret;
            }
            return Expression.Error;
        }
        public IExpression VisitExpression([NotNull] FBlangParser.ExprContext context, IType expectedReturnType = null) {
            if (TryVisitExpression(context, out var ret, expectedReturnType)) {
                return ret;
            }
            return Expression.Error;
        }
        VariableAccessExpression CreateVariableAccessExpression(Position pos, IType retTy, IVariable vr, IExpression parent = null) {
            var ret = new VariableAccessExpression(pos, retTy, vr, parent);
            if (parent != null) {
                //vr is field
                var callingCtx = contextStack.Peek();
                var memberDefCtx = vr.DefinedInType?.Context ?? (IContext) Module;
                var minVis = VisibilityHelper.MinimumVisibility(memberDefCtx, callingCtx);
                if (vr.Visibility < minVis) {
                    $"The {vr.Visibility} field {vr.Signature} is not visible in {contextStack.Peek().Name}".Report(pos);
                }
            }
            else if (vr.Signature.Name.StartsWith('%') && vr.Visibility < Visibility.Public && currentMacro != null && currentMacro.Finished) {
                $"The macro-private variable {vr.Signature} cannot be accessed from outside".Report(pos);
            }
            return ret;
        }
        static VariableAccessExpression CreateVariableAccessExpression(Position pos, IType expectedRetTy, string name, IExpression parent) {

            return new VariableAccessExpression(pos, expectedRetTy, name, parent);
        }
        public bool VisitMemberAccessExpr([NotNull] FBlangParser.MemberAccessExprContext context, out IExpression memberParent, out IType memberParentType, out IContext memberContext, out string memberName, IType expectedReturnType, ErrorBuffer err = null) {
            // visit member access
            //!! handle literal->ident specially (visitliteral cannot see parent)
            // This is the root of the memberAccess
            // => chaining handled in this method
            memberContext = contextStack.Peek();
            {
                if (context.lhs is FBlangParser.LiteralExprContext litEx && litEx.literal().Ident() != null) {
                    memberName = litEx.literal().GetText();
                    var vr = memberContext.TryGetVariableByName(memberName, litEx.literal().Position(fileName));

                    if (vr != null) {
                        memberParentType = vr.Type;
                        if (vr.IsStatic() || !vr.IsStatic() && vr.IsLocalVariable()) {
                            // Do Not capture the LHS
                            //if (IsLambdaCapture(vr, out var cap))
                            //    vr = cap;
                            memberParent = CreateVariableAccessExpression(litEx.literal().Position(fileName), null, vr);
                        }
                        else {
                            var inst = NextTypeContext();
                            if (inst != null)
                                memberParent = InstanceVariableWithImplicitThis(litEx.literal().Position(fileName), vr, inst, err);
                            else {
                                memberParent = err.Report($"Cannot access instance-variable {vr.Signature} from static context", litEx.literal().Position(fileName), Expression.Error);
                                return false;
                            }
                        }
                        memberContext = memberParentType.Context.InstanceContext;
                    }
                    else if (currentMacro != null && currentMacro.NamedArguments.TryGetValue(memberName, out var exParam)) {
                        memberParentType = Type.Top;
                        memberParent = new ExpressionParameterAccess(litEx.literal().Position(fileName), exParam);
                    }
                    else {

                        //typename or namespacename
                        var tp = memberContext.TypesByName(memberName).FirstOrDefault(x => !x.Signature.GenericActualArguments.Any());
                        if (tp != null) {
                            memberParent = null;
                            memberParentType = tp;
                            memberContext = tp.Context.StaticContext;
                        }
                        else {
                            // namespacename
                            var nCtx = CurrentNamespace();
                            memberParentType = null;
                            memberParent = null;
                            if (nCtx is null) {
                                memberContext = null;
                                return err.Report($"The identifier '{memberName}' is not defined in {contextStack.Peek().Name}", litEx.literal().Position(fileName), false);
                            }
                            Module.IsNamespaceContext(nCtx, out var nsq);
                            for (var i = nsq; i != null; i = i.Next) {
                                if (i.Value == memberName) {
                                    memberContext = Module.NamespaceContext(i);
                                    break;
                                }
                            }
                            if (memberContext is null) {
                                return err.Report($"The namespace '{string.Join(".", nsq.Reverse())}' does not contain a definition for the identifier '{memberName}'", litEx.literal().Position(fileName), false);
                            }
                        }
                    }
                }
                else if (context.lhs is FBlangParser.LiteralExprContext _litEx && _litEx.literal().MacroLocalIdent() != null) {
                    memberName = _litEx.literal().GetText();
                    if (currentMacro != null) {
                        var vr = currentMacro.LocalContext.TryGetVariableByName(memberName, _litEx.literal().Position(fileName));
                        if (vr != null) {
                            memberParentType = vr.Type;
                            memberParent = CreateVariableAccessExpression(_litEx.literal().Position(fileName), null, vr);
                            memberContext = memberParentType.Context.InstanceContext;
                        }
                        else {
                            memberParent = err.Report($"The macro-local variable {memberName} is not defined in {currentMacro.NestedIn.Name}", _litEx.literal().Position(fileName), Expression.Error);
                            memberParentType = Type.Error;
                        }
                    }
                    else {
                        memberParent = err.Report($"A macro-local variable cannot be used outside of a macro", _litEx.literal().Position(fileName), Expression.Error);
                        memberParentType = Type.Error;
                    }
                }
                else {
                    memberName = null;
                    if (!TryVisitEx(context.lhs, out memberParent, null, err)) {
                        memberParentType = Type.Error;
                        return false;
                    }
                    else {
                        memberParentType = memberParent.ReturnType;
                        memberContext = memberParentType.Context.InstanceContext;
                    }
                }
            }
            // rhs
            while (context.rhs is FBlangParser.MemberAccessExprContext) {
                context = context.rhs as FBlangParser.MemberAccessExprContext;
                if (context.lhs is FBlangParser.LiteralExprContext litEx && litEx.literal().Ident() != null) {
                    memberName = litEx.literal().Ident().GetText();
                    var vr = memberContext.TryGetVariableByName(memberName, litEx.literal().Position(fileName));


                    if (vr != null) {
                        memberParentType = vr.Type;
                        // Do not capture the LHS of the RHS
                        //if (IsLambdaCapture(vr, out var cap))
                        //    vr = cap;
                        memberParent = CreateVariableAccessExpression(litEx.literal().Position(fileName), null, vr, memberParent);

                        memberContext = memberParentType.Context.InstanceContext;
                    }
                    else if (memberParent != null && memberParentType.IsTop()) {
                        memberParent = CreateVariableAccessExpression(litEx.literal().Position(fileName), Type.Top, memberName, memberParent);
                    }
                    else if (memberParent != null) {
                        return err.Report($"The instance-variable or instance-method '{memberName}' is not defined in {(memberParentType != null ? memberParentType.Context.Name : memberContext.Name)}", litEx.literal().Position(fileName), false);
                    }
                    else {

                        //typename or namespacename
                        var tp = memberContext.TypesByName(memberName).FirstOrDefault(x => !x.Signature.GenericActualArguments.Any());
                        if (tp != null) {
                            memberParent = null;
                            memberParentType = tp;
                            memberContext = tp.Context.StaticContext;
                        }
                        else if (memberParentType != null) {
                            return err.Report($"The type '{memberParentType.Signature}' does not define a type or static variable '{memberName}'", litEx.literal().Position(fileName), false);
                        }
                        else {
                            // namespacename
                            memberParentType = null;
                            memberParent = null;
                            if (Module.IsNamespaceContext(memberContext, out var nsq)) {

                                for (var i = nsq; i != null; i = i.Next) {
                                    if (i.Value == memberName) {
                                        memberContext = Module.NamespaceContext(i);
                                        break;
                                    }
                                }
                                if (memberContext is null) {
                                    return err.Report($"The namespace '{string.Join(".", nsq.Reverse())}' does not contain a definition for the identifier '{memberName}'", litEx.Position(fileName), false);
                                }
                            }
                            else {
                                // wird nicht vorkommen
                                return err.Report($"The current context does not define a member '{memberName}'", litEx.literal().Position(fileName), false);
                            }
                        }
                    }
                }
                else {
                    memberName = null;
                    if (!TryVisitEx(context.lhs, out memberParent, null, err)) {
                        memberParentType = Type.Error;
                        return false;
                    }
                    else {
                        memberParentType = memberParent.ReturnType;
                    }
                }
            }
            //context.rhs is not MemberAccessExpr
            {

                if (context.rhs is FBlangParser.LiteralExprContext litEx && litEx.literal().Ident() != null) {
                    memberName = litEx.literal().Ident().GetText();
                    /*var vr= FBSemantics.Instance.BestFittingVariable(litEx.literal().Position(fileName),
                                                             memberContext.VariablesByName(memberName),
                                                             expectedReturnType);*/
                    //var vr = memberContext.TryGetVariableByName(memberName, litEx.literal().Position(fileName));
                    var vrErr= FBSemantics.Instance.BestFittingVariable(litEx.literal().Position(fileName),
                                                                      memberContext.VariablesByName(memberName),
                                                                      expectedReturnType, out var vr);
                    var mets = memberContext.DeclaredMethodsByName(memberName);

                    if (vrErr == Error.None) {
                        memberParentType = vr.Type;
                        IExpression membPar = CreateVariableAccessExpression(litEx.literal().Position(fileName), null, vr, memberParent);
                        if (IsLambdaCapture(vr, out var cap))
                            memberParent = GetCaptureAccessExpression(litEx.Position(fileName), null, membPar, cap, memberParent);

                        memberParent = membPar;
                        memberName = null;
                        return true;
                    }
                    else if (mets.Any()) {
                        // lhs must be captured
                        if (memberParent is VariableAccessExpression vre && IsLambdaCapture(vre.Variable, out var cap)) {
                            // memberParent = new CaptureAccessExpression(vre.Position, vre, cap);
                            memberParent = GetCaptureAccessExpression(vre.Position, vre.ReturnType, vre, cap, vre.ParentExpression);
                        }
                        return true;
                    }
                    else if (memberParent != null && memberParentType.IsTop()) {
                        return true;
                    }
                    else if (memberParent != null) {
                        //ContextReplace cr;
                        memberContext.VariablesByName(memberName);
                        return vrErr == Error.NoValidTarget && err.Report($"The instance-variable or instance-method '{memberName}' is not defined in {(memberParentType != null ? memberParentType.Context.Name : memberContext.Name)}", litEx.literal().Position(fileName), false);
                    }
                    else {

                        //typename (namespacename is not allowed here)
                        var tp = memberContext.TypesByName(memberName).FirstOrDefault(x => !x.Signature.GenericActualArguments.Any());
                        if (tp != null) {
                            memberParent = null;
                            memberParentType = tp;
                            memberContext = tp.Context.StaticContext;
                            return true;
                        }
                        else if (memberParentType != null) {
                            return err.Report($"The type '{memberParentType.Signature}' does not define a type or static variable or static method '{memberName}'", litEx.literal().Position(fileName), false);
                        }
                        else {
                            if (Module.IsNamespaceContext(memberContext, out var nsq))
                                return err.Report($"The namespace '{string.Join(".", nsq.Reverse())}' does not contain a definition for the identifier '{memberName}'", litEx.Position(fileName), false);
                            return err.Report($"The current context does not contain a definition for the identifier '{memberName}'", litEx.Position(fileName), false);
                        }

                    }
                }
                else {
                    memberName = null;
                    if (!TryVisitEx(context.rhs, out memberParent, null, err)) {
                        memberParentType = Type.Error;
                        return false;
                    }
                    else {
                        memberParentType = memberParent.ReturnType;
                        return true;
                    }
                }
            }

        }

        public IExpression VisitLiteral([NotNull] FBlangParser.LiteralContext context, IType expectedReturnType = null) {
            TryVisitLiteral(context, out var ret, expectedReturnType, null, fileName);
            return ret;
        }
        public IExpression[] VisitActualArglist([NotNull] FBlangParser.ActualArglistContext context) {
            return context.expr().Select(x => VisitExpression(x)).ToArray();
        }
        public bool TryVisitActualArglist([NotNull] FBlangParser.ActualArglistContext context, out IExpression[] ret, ErrorBuffer err, IEnumerable<IType> expectedArgTypes = null) {
            var exprs = context.expr();
            ret = new IExpression[exprs.Length];
            bool toret = true;
            for (int i = 0; i < ret.Length; ++i) {
                toret &= TryVisitExpression(exprs[i], out var curr, null, err, expectedArgTypes);
                ret[i] = curr;
            }
            return toret;
        }
        public IStatement VisitInstruction([NotNull] FBlangParser.InstructionContext context) {
            if (context.blockInstruction() != null) {
                return VisitBlockInstruction(context.blockInstruction());
            }
            else
                return VisitInst(context.inst());
        }
        public IStatement VisitInst([NotNull] FBlangParser.InstContext context) {
            if (context.ifStmt() != null) {
                return VisitIfStmt(context.ifStmt());
            }
            else if (context.whileLoop() != null) {
                return VisitWhileLoop(context.whileLoop());
            }
            else if (context.doWhileLoop() != null) {
                return VisitDoWhileLoop(context.doWhileLoop());
            }
            else if (context.forLoop() != null) {
                return VisitForLoop(context.forLoop());
            }
            else if (context.foreachLoop() != null) {
                return VisitForeachLoop(context.foreachLoop());
            }
            else if (context.concForeachLoop() != null) {
                var taskTy = PrimitiveType.Void.AsAwaitable();
                return new ExpressionStmt(context.Position(fileName),
                    new CallExpression(context.Position(fileName),
                        PrimitiveType.Void,
                        taskTy.Context.InstanceContext.MethodsByName("wait").Single(),
                        VisitConcurrentForLoop(context.concForeachLoop()),
                        Array.Empty<IExpression>()
                    )
                );
            }
            else if (context.switchStmt() != null) {
                return VisitSwitchStmt(context.switchStmt());
            }
            else if (context.tryCatchStmt() != null) {
                return VisitTryCatchStmt(context.tryCatchStmt());
            }
            else if (context.stmt() != null) {
                return VisitStatement(context.stmt());
            }
            // will not reach this point
            return "Invalid statement".Report(context.Position(fileName), Statement.Error);
        }

        public TryCatchFinallyStatement VisitTryCatchStmt([NotNull] FBlangParser.TryCatchStmtContext context) {
            var blocks = context.blockInstruction();
            IStatement tryBlock = VisitBlockInstruction(blocks[0]);
            IStatement finallyBlock;
            bool hasFinally;
            if (context.Finally() != null) {
                finallyBlock = VisitBlockInstruction(blocks[^1]);
                hasFinally = true;
            }
            else {
                finallyBlock = null;
                hasFinally = false;
            }
            var catchStmts = new List<IStatement>();
            foreach (var block in MemoryExtensions.AsSpan(blocks, 1, blocks.Length - (hasFinally ? 2 : 1))) {
                catchStmts.Add(VisitBlockInstruction(block));
            }
            return new TryCatchFinallyStatement(context.Position(fileName), tryBlock, catchStmts, finallyBlock);
        }
        public SwitchStatement VisitSwitchStmt([NotNull] FBlangParser.SwitchStmtContext context) {
            var value = VisitExpression(context.expr());
            var ret = new SwitchStatement(context.Position(fileName), value, null);
            redirectTarget.Push(ret);
            var cases = (from cas in context.@case()
                         select VisitCase(cas, value.ReturnType)
                         ).ToArray();
            var dfltCases = cases.OfType<SwitchStatement.DefaultCase>();
            if (dfltCases.HasCount(2)) {
                "A switch-statement must not have more than one default-case".Report(dfltCases.ElementAt(1).Position);
            }
            ret.Cases = cases;
            redirectTarget.Pop();
            return ret;
        }
        public SwitchStatement.Case VisitCase([NotNull] FBlangParser.CaseContext context, IType switchTy) {

            switch (context) {
                case FBlangParser.ElseCaseContext cElse: {
                    var inst = VisitInstruction(cElse.instruction());
                    return new SwitchStatement.DefaultCase(cElse.Position(fileName), inst);
                }
                case FBlangParser.NormalCaseContext cNormal: {
                    var (pats, ins) = VisitNormalCase(cNormal);
                    Module.Semantics.ValidateAll(pats, switchTy, out var definedVars);
                    var bodyCtx = contextStack.Peek().NewScope(true);
                    using (contextStack.PushFrame(bodyCtx)) {
                        DoDefineVariables(definedVars.ToArray());
                        var inst = VisitInstruction(ins);
                        return new SwitchStatement.Case(cNormal.Position(fileName), inst, pats.Select(x => x.Item1).ToArray());
                    }

                }
                /*case FBlangParser.BiCaseContext cBi: {
                    var (pats, ins) = VisitBiCase(cBi);
                    Module.Semantics.ValidateAll(pats, switchTy);
                    var inst = VisitBlockInstruction(ins);
                    return new SwitchStatement.Case(cBi.Position(fileName), inst, pats);
                }*/
                default:
                    throw new ArgumentException();
            }
        }

        /*private (SwitchStatement.IPattern[], FBlangParser.BlockInstructionContext) VisitBiCase(FBlangParser.BiCaseContext cBi) {
            var patterns = cBi.casePattern().Select(x => VisitCasePattern(x)).Append(VisitNonRecursiveCasePattern(cBi.nonRecursiveCasePattern())).ToArray();
            //var inst = VisitBlockInstruction(cBi.blockInstruction());
            return (patterns, cBi.blockInstruction());
        }*/

        private ((SwitchStatement.IPattern, IContext)[], FBlangParser.InstructionContext) VisitNormalCase(FBlangParser.NormalCaseContext cNormal) {
            var patterns = cNormal.casePattern().Select(x => {
                var patCtx = contextStack.Peek().NewScope(true);
                SwitchStatement.IPattern ret;
                using (contextStack.PushFrame(patCtx)) {
                    ret = VisitCasePattern(x);
                }
                return (ret, patCtx);
            }).ToArray();

            return (patterns, cNormal.instruction());
        }

        /*private SwitchStatement.Case VisitElseCase(FBlangParser.ElseCaseContext cElse) {
            var inst = VisitInstruction(cElse.instruction());
            return new SwitchStatement.DefaultCase(cElse.Position(fileName), inst);
        }*/

        SwitchStatement.IPattern VisitCasePattern([NotNull] FBlangParser.CasePatternContext context) {
            if (context.recursiveCasePattern() != null)
                return VisitRecursiveCasePattern(context.recursiveCasePattern());
            else if (context.nonRecursiveCasePattern() != null)
                return VisitNonRecursiveCasePattern(context.nonRecursiveCasePattern());
            else
                throw new ArgumentException();
        }
        SwitchStatement.IPattern VisitRecursiveCasePattern(FBlangParser.RecursiveCasePatternContext context) {
            var type = context.typeIdent() != null ? VisitTypeIdent(context.typeIdent(), fileName) : Type.Top;
            var subs = from subx in context.casePatternList().casePattern()
                       select VisitCasePattern(subx);
            return new SwitchStatement.RecursivePattern(context.Position(fileName), type, subs);
        }
        SwitchStatement.IPattern VisitNonRecursiveCasePattern(FBlangParser.NonRecursiveCasePatternContext context) {
            if (context.Percent() != null)
                return new SwitchStatement.Wildcard();
            else if (context.literal() != null)
                return VisitLiteral(context.literal()) is SwitchStatement.IPattern pat ? pat : new SwitchStatement.Wildcard();
            else if (context.sdecl() != null)
                return VisitSDecl(context.sdecl(), null, false, true) is SwitchStatement.IPattern pat ? pat : new SwitchStatement.Wildcard();
            else
                throw new ArgumentException();
        }

        void InferConditionalNotNull(IExpression condition, ICollection<IVariable> notNullable, ICollection<IVariable> nullable) {
            if (condition is BinOp bo) {
                if (bo.Operator == BinOp.OperatorKind.EQ || bo.Operator == BinOp.OperatorKind.NEQ) {
                    if (bo.Operator == BinOp.OperatorKind.NEQ)
                        (notNullable, nullable) = (nullable, notNullable);
                    if (bo.Left is VariableAccessExpression || bo.Right is VariableAccessExpression) {
                        VariableAccessExpression vre;
                        IExpression other;
                        if (bo.Right is VariableAccessExpression) {
                            vre = bo.Right as VariableAccessExpression;
                            other = bo.Left;
                        }
                        else {
                            vre = bo.Left as VariableAccessExpression;
                            other = bo.Right;
                        }

                        if (other == Literal.Null) {
                            nullable.Add(vre.Variable);
                        }
                        else if (other.IsNotNullable()) {
                            notNullable.Add(vre.Variable);
                        }
                    }
                }
                else if (bo.Operator == BinOp.OperatorKind.LAND) {
                    InferConditionalNotNull(bo.Left, notNullable, nullable);
                    InferConditionalNotNull(bo.Right, notNullable, nullable);
                }
                //TODO other operators;
            }
        }
        public IfStatement VisitIfStmt([NotNull] FBlangParser.IfStmtContext context) {
            var ifNoElse = context.ifNoElseStmt();
            var elseinst = context.instruction();
            var cond = VisitExpression(ifNoElse.expr(), PrimitiveType.Bool);
            IStatement thenCase, elseCase = null;
            ISet<IVariable> thenNotNull, elseNotNull;

            List<IVariable>
                thenNotNullable = new List<IVariable>(),
                elseNotNullable = new List<IVariable>();

            InferConditionalNotNull(cond, thenNotNullable, elseNotNullable);
            IDisposable nonNullableFrame;
            if (thenNotNullable.Count > 0 || elseNotNullable.Count > 0) {
                nonNullableFrame = nonNullableStack.PushFrame(new HashSet<IVariable>(nonNullableStack.Peek()));
                nonNullableStack.Peek().Except(elseNotNullable);
                nonNullableStack.Peek().UnionWith(thenNotNullable);
            }
            else {
                nonNullableFrame = nonNullableStack.PushFrame(nonNullableStack.Peek());
            }

            using (nonNullableFrame) {
                thenCase = VisitInstruction(ifNoElse.instruction());
                thenNotNull = nonNullableStack.Peek();
            }
            //null;
            if (elseinst != null) {
                if (elseNotNullable.Count > 0 || thenNotNullable.Count > 0) {
                    nonNullableFrame = nonNullableStack.PushFrame(new HashSet<IVariable>(nonNullableStack.Peek()));
                    nonNullableStack.Peek().ExceptWith(thenNotNullable);
                    nonNullableStack.Peek().UnionWith(elseNotNullable);
                }
                else {
                    nonNullableFrame = nonNullableStack.PushFrame(nonNullableStack.Peek());
                }
                using (nonNullableFrame) {
                    elseCase = VisitInstruction(elseinst);
                    elseNotNull = nonNullableStack.Peek();
                }
            }
            else {
                elseNotNull = nonNullableStack.Peek();
            }
            // merge thenNotnull and elseNotNull
            nonNullableStack.ExchangeTop(new HashSet<IVariable>(thenNotNull.Intersect(elseNotNull)));

            return new IfStatement(context.Position(fileName), cond, thenCase, elseCase);

        }
        public WhileLoop VisitWhileLoop([NotNull] FBlangParser.WhileLoopContext context) {
            var cond = VisitExpression(context.expr());
            var ret = new WhileLoop(context.Position(fileName), cond, null);
            redirectTarget.Push(ret);
            var body = VisitInstruction(context.instruction());
            ret.Body = body;
            redirectTarget.Pop();
            return ret;
        }
        public DoWhileLoop VisitDoWhileLoop([NotNull] FBlangParser.DoWhileLoopContext context) {
            var cond = VisitExpression(context.expr());
            var ret = new DoWhileLoop(context.Position(fileName), cond, null);
            redirectTarget.Push(ret);
            var body = VisitInstruction(context.instruction());
            ret.Body = body;
            redirectTarget.Pop();
            return ret;
        }
        public ForLoop VisitForLoop([NotNull] FBlangParser.ForLoopContext context) {
            var ctx = contextStack.Peek().NewScope(true);
            using (PushContext(ctx)) {
                var init = context.forInit() != null ? VisitForInit(context.forInit()) : null;

                /*if (init is Declaration decl) {
                    ctx = ctx.NewScope(Context.DefiningRules.Variables);
                    DoDefineVariables(decl, ctx);
                }*/
                ForLoop ret;


                var cond = context.expr() != null ? VisitExpression(context.expr()) : null;
                var incr = context.stmt() != null ? VisitStatement(context.stmt()) : null;

                ret = new ForLoop(context.Position(fileName), init, cond, incr, null, ctx);
                using (redirectTarget.PushFrame(ret)) {
                    var body = VisitInstruction(context.instruction());
                    ret.Body = body;
                }
                return ret;
            }

        }
        public IStatement VisitForInit([NotNull] FBlangParser.ForInitContext context) {
            if (context.deconstructStmt() != null)
                return VisitDeconstructStmt(context.deconstructStmt());
            else if (context.assignExp() != null)
                return new ExpressionStmt(context.Position(fileName), VisitAssignExp(context.assignExp(), null));
            else
                return VisitDeclaration(context.declaration());
        }
        public ForeachLoop VisitForeachLoop([NotNull] FBlangParser.ForeachLoopContext context) {

            var range = VisitExpression(context.expr());
            ForeachLoop ret;
            bool hasDecl;
            IDisposable ctxFrame = null;
            if (context.foreachInit().decl() != null) {
                hasDecl = true;
                var ctx = contextStack.Peek().NewScope(Context.DefiningRules.Variables);
                // contextStack.Push(ctx);
                ctxFrame = PushContext(ctx);
                //var decl = VisitDecl(context.foreachInit().decl());
                var (ty, spec, names, vis) = DeclarationInfos(context.foreachInit().decl());
                bool isByRef = ty.IsByRef();
                bool isByConstRef = ty.IsByConstRef();
                if (!range.ReturnType.IsArraySlice() && !range.ReturnType.IsArray()) {
                    "ByRef iteration is only supported for arrays and slices. Try to iterate by value or copy the range into an array first".Report(range.Position); //DOLATER: support byref iterators
                }
                var tyStem = isByRef ? ty.UnWrap() : ty;
                Declaration decl;
                if (tyStem.IsTop()) {
                    if (!range.ReturnType.IsTop()) {

                        var over = Module.IsIterable(range.ReturnType);

                        if (over.IsSingelton(out ty)) {
                            if (isByRef) {
                                ty = isByConstRef ? ty.AsByConstRef() : ty.AsByRef();

                            }

                            decl = new Declaration(context.Position(fileName), ty, spec, names, vis: vis);
                        }
                        else {
                            decl = new Declaration(context.Position(fileName), ty, spec, names, vis: vis);
                            $"The type of the loop-variable{(names.Length > 1 ? "s" : "")} {string.Join(", ", names)} cannot be inferred from the range".Report(context.expr().Position(fileName));
                        }
                    }
                    else
                        decl = new Declaration(context.Position(fileName), ty, spec, names, vis: vis);
                }
                else {
                    decl = new Declaration(context.Position(fileName), ty, spec, names, vis: vis);
                    if (!range.ReturnType.IsTop() && !Module.IsIterableOver(range.ReturnType, tyStem))
                        $"The range-object of the foreach-loop is not iterable over {decl.Type.Signature}".Report(context.expr().Position(fileName));
                }
                DoDefineVariables(decl, ctx);

                ret = new ForeachLoop(context.Position(fileName), decl, range, null, ctx);
            }
            else {
                hasDecl = false;
                var arglist = VisitActualArglist(context.foreachInit().actualArglist());
                foreach (var arg in arglist.Where(x => !IsLValue(x))) {
                    "The loop-variable(s) for a range-based for-loop must be lvalue".Report(arg.Position);
                }
                //DOLATER tuple-iterators
                if (arglist.Length != 1)
                    "The foreach-loop cannot iterate over multiple values at the same time".Report(context.foreachInit().actualArglist().Position(fileName));
                else if (!Module.IsIterableOver(range.ReturnType, arglist[0].ReturnType))
                    $"The range-object of the foreach-loop is not iterable over {arglist[0].ReturnType.Signature}".Report(context.expr().Position(fileName));
                ret = new ForeachLoop(context.Position(fileName), arglist, range, null, contextStack.Peek());
            }
            if (context.Simd() != null)
                ret.EnableVectorization = true;

            using (redirectTarget.PushFrame(ret)) {
                ret.Body = VisitInstruction(context.instruction());
            }
            if (hasDecl)
                //contextStack.Pop();
                ctxFrame.Dispose();
            return ret;
        }
        private IExpression VisitConcurrentForLoop(FBlangParser.ConcForeachLoopContext context) {
            var range = VisitExpression(context.expr());
            var expectedVarTy = Type.Top;
            if (range.ReturnType.UnWrap() is AggregateType agg) {
                expectedVarTy = agg.ItemType;
            }
            else
                "The range of the concurrent for-loop is not iterable".Report(context.Position(fileName));
            var headerCtx = contextStack.Peek().NewScope(true);

            var ctx = contextStack.Peek().NewScope(true);
            var lctx = new LambdaBlockContext();
            Declaration decl;
            DoDefineVariables(decl = VisitDecl(context.decl(), expectedVarTy), ctx);
            using (lambdaContextStack.PushFrame(lctx)) {
                using (PushContext(ctx)) {

                    var body = VisitInstruction(context.instruction());
                    var lambda = new LambdaExpression(context.instruction().Position(fileName),
                        new FunctionType(context.instruction().Position(fileName),
                            Module,
                            "concurrentForBody",
                            PrimitiveType.Void,
                            decl.Variables.Select(x => x.Type).AsCollection(decl.Variables.Length),
                            Visibility.Internal
                        ),
                        ctx,
                        decl.Variables.Select(x => x.Signature.Name).ToArray()
                    ) {
                        Arguments = decl.Variables,
                        CaptureAccesses = lctx.captureAccesses.Values,
                        Captures = lctx.captures.Values
                    };
                    lambda.Body.Instruction = body;
                    return new ConcurrentForLoop(context.Position(fileName), range, lambda, decl) { EnableVectorization = context.Simd() != null };
                }

            }
        }
        public IStatement VisitStatement([NotNull] FBlangParser.StmtContext context) {
            if (context.assignExp() != null) {
                return new ExpressionStmt(context.Position(fileName), VisitAssignExp(context.assignExp(), null));
            }
            else if (context.declaration() != null) {
                return VisitDeclaration(context.declaration());
            }
            else if (context.deconstructStmt() != null) {
                return VisitDeconstructStmt(context.deconstructStmt());
            }
            else if (context.awaitStmt() != null) {
                return VisitAwaitStmt(context.awaitStmt());
            }
            else if (context.callStmt() != null) {
                return VisitCallStmt(context.callStmt());
            }
            else if (context.incStmt() != null) {
                return VisitIncStmt(context.incStmt());
            }
            else if (context.returnStmt() != null) {
                return VisitReturnStmt(context.returnStmt());
            }
            else if (context.yieldStmt() != null) {
                return VisitYieldStmt(context.yieldStmt());
            }
            else if (context.shiftStmt() != null) {
                // shift-stmt
                var shift = context.shiftStmt();
                BinOp.OperatorKind op;
                if (shift.LShift() != null)
                    op = BinOp.OperatorKind.LSHIFT;
                else if (shift.srShift() != null)
                    op = BinOp.OperatorKind.SRSHIFT;
                else
                    op = BinOp.OperatorKind.URSHIFT;
                TryVisitBinOp(shift.ex(), op, out var ret, null, null);
                return new ExpressionStmt(context.Position(fileName), ret);
            }
            else if (context.deferStmt() != null) {
                // run-async
                var ex = VisitEx(context.deferStmt().ex());
                return new ExpressionStmt(context.Position(fileName), new RunAsyncExpression(context.Position(fileName), ex));
            }
            else if (context.breakStmt() != null) {
                if (redirectTarget.TryPeek(out var tar))
                    return new BreakStatement(context.Position(fileName), tar);
                else
                    return "Break is not allowed in this context (only allowed in loops and switch-cases)".Report(context.Position(fileName), Statement.Error);
            }
            else if (context.continueStmt() != null) {
                if (redirectTarget.TryPeek(out var tar) && !(tar is SwitchStatement))
                    return new ContinueStatement(context.Position(fileName), tar);
                else
                    return "Continue is not allowed in this context (only allowed in loops)".Report(context.Position(fileName), Statement.Error);
            }
            else if (context.superStmt() != null) {
                var tp = NextTypeContext();
                IHierarchialType htp;
                if (tp is null) {
                    return "The super-constructor cannot be used outside of a type".Report(context.Position(fileName), Statement.Error);
                }
                else if (currentMethod.HasFlag(Method.Specifier.Static)) {
                    return "The super-constructor cannot be called in a static context".Report(context.Position(fileName), Statement.Error);
                }
                else if (!(tp is IHierarchialType) || (htp = tp as IHierarchialType).SuperType == null) {
                    return $"The super-constructor is not available in the type {tp.Signature}, because it has no superclass".Report(context.Position(fileName), Statement.Error);
                }
                else if (!currentMethod.HasFlag(Method.Specifier.Constructor)) {
                    return $"The super-constructor can only be called from a constructor".Report(context.Position(fileName), Statement.Error);
                }
                IExpression[] args;
                if (context.superStmt().actualArglist() != null) {
                    if (!TryVisitActualArglist(context.superStmt().actualArglist(), out args, null))
                        return Statement.Error;
                }
                else {
                    args = Array.Empty<IExpression>();
                }
                var ctor = Module.GetConstructor(context.Position(fileName), htp.SuperType, args, null);
                if (ctor.IsError())
                    return Statement.Error;
                return new SuperCall(context.Position(fileName), htp.SuperType, ctor, args);
            }
            else if (context.macroStmt() != null) {
                return VisitMacroStmt(context.macroStmt());
            }
            else if (context.identStmt() != null) {
                return VisitIdentStmt(context.identStmt());
            }
            //wird nicht auftreten
            return "Unknown Statement".Report(context.Position(fileName), Statement.Error);
        }

        private IStatement VisitIdentStmt(FBlangParser.IdentStmtContext context) {
            var blockName = context.Ident().GetText();
            if (currentMacro != null && currentMacro.HasCapturedStatement && currentMacro.CapturedStatement.Name == blockName) {
                return new StatementParameterAccess(context.Position(fileName), currentMacro.CapturedStatement);
            }
            else {
                return "An identifier can only be a statement, when it is the use of a statement-parameter inside of a macro".Report(context.Position(fileName), Statement.Error);
            }
        }

        private IStatement VisitMacroStmt(FBlangParser.MacroStmtContext context) {
            return context switch
            {
                FBlangParser.MacroCallContext mc => VisitMacroCallStmt(mc),
                FBlangParser.MacroCaptureContext mc => VisitMacroCaptureStmt(mc),
                _ => throw new ArgumentException(nameof(context)),
            };
        }

        private IStatement VisitMacroCaptureStmt(FBlangParser.MacroCaptureContext context) {
            if (currentMacro != null) {
                "Calling macros from another macro is not allowed".Report(context.Position(fileName));
                return Statement.Error;
            }
            var name = context.Ident().GetText();
            IExpression parent = null;
            if (context.ex() != null) {
                parent = VisitEx(context.ex());
            }

            var macro = parent != null && !parent.IsError() ? parent.ReturnType.Context.InstanceContext.MacroByName(name) : contextStack.Peek().MacroByName(name);
            if (macro is null) {
                return $"The macro {name} is not defined in {contextStack.Peek().Name}".Report(context.Position(fileName), Statement.Error);
            }
            else if (macro.Arguments.Any()) {
                return $"The macro {macro.Name} has too few arguments: it needs {(macro.HasVarArgs ? "at least " : "")}{macro.NamedArguments.Count} arguments.".Report(context.Position(fileName), Statement.Error);
            }
            else {

                var requiredArgs = new Dictionary<ExpressionParameter, IExpression>();

                var optionalArgs = ((ExpressionParameterPack, IExpression[])?) null;

                var curMacro = currentMacro;
                currentMacro = macro;
                var capture = VisitInstruction(context.instruction());
                currentMacro = curMacro;
                if (macro.HasCapturedStatement) {
                    var callParameters = new MacroCallParameters(context.Position(fileName), requiredArgs, optionalArgs, (macro.CapturedStatement, capture), Module.Semantics, contextStack, parent, macro);

                    callParameters.MakeReadOnly();
                    macro.Body.Instruction.TryReplaceMacroParameters(callParameters, out var ret);
                    return ret;
                }
                else {
                    return "The parameterless macro-call syntax can only be used with macros which capture the next statement".Report(context.Position(fileName), Statement.Error);
                }
            }
        }

        private IStatement VisitMacroCallStmt(FBlangParser.MacroCallContext context) {
            if (currentMacro != null) {
                "Calling macros from another macro is not allowed".Report(context.Position(fileName));
                return Statement.Error;
            }
            var name = context.Ident().GetText();
            IExpression parent = null;
            if (context.ex() != null) {
                parent = VisitEx(context.ex());
            }

            var macro = parent != null && !parent.IsError() ? parent.ReturnType.Context.InstanceContext.MacroByName(name) : contextStack.Peek().MacroByName(name);
            if (macro is null) {
                return $"The macro {name} is not defined in {contextStack.Peek().Name}".Report(context.Position(fileName), Statement.Error);
            }
            else {

                var args = context.actualArglist() != null ? VisitActualArglist(context.actualArglist()) : Array.Empty<IExpression>();
                if (args.Length < macro.NamedArguments.Count) {
                    return $"The macro {macro.Name} has too few arguments: it needs {(macro.HasVarArgs ? "at least " : "")}{macro.NamedArguments.Count} arguments.".Report(context.Position(fileName), Statement.Error);
                }
                var requiredArgs = args.Take(macro.NamedArguments.Count).Zip(macro.NamedArguments.Values).ToDictionary(x => x.Second, x => x.First);

                var optionalArgs = macro.HasVarArgs ? (macro.VarArgs, args.Skip(macro.NamedArguments.Count).ToArray()) : ((ExpressionParameterPack, IExpression[])?) null;

                var callParameters = new MacroCallParameters(context.Position(fileName), requiredArgs, optionalArgs, null, Module.Semantics, contextStack, parent, macro);
                if (macro.HasCapturedStatement) {
                    var ret = new InstructionBox();
                    unresolvedMacroCall.Push((callParameters, ret));
                    return ret;
                }
                else {
                    callParameters.MakeReadOnly();
                    macro.Body.Instruction.TryReplaceMacroParameters(callParameters, out var ret);

                    return ret;
                }
            }
        }

        public IStatement VisitAwaitStmt([NotNull] FBlangParser.AwaitStmtContext context) {
            var awaitable = VisitEx(context.ex());
            IType retTy;

            currentMethod |= Method.Specifier.Coroutine | Method.Specifier.Async;

            if (awaitable.ReturnType.IsAwaitable()) {
                retTy = ((AggregateType) awaitable.ReturnType).ItemType;
            }
            else {
                retTy = "The type of the awaited value is not async".ReportTypeError(context.Position(fileName));
            }
            return new ExpressionStmt(context.Position(fileName), new UnOp(context.Position(fileName), retTy, UnOp.OperatorKind.AWAIT, awaitable));
        }
        void OptimizeCall(ref IExpression exp) {
            if (exp is CallExpression ce) {
                if (ce.Callee.IsInternal() && ce.Callee.Signature.Name == "wait" && ce.ParentExpression is CallExpression delay && delay.Callee.IsInternal() && delay.Callee.Signature.Name == "delay" && Module.TryGetInternalMethod("thread_sleep", out var sleep)) {
                    exp = new CallExpression(ce.Position, PrimitiveType.Void, sleep, null, delay.Arguments.ToArray());
                }
            }
        }
        public IStatement VisitCallStmt([NotNull] FBlangParser.CallStmtContext context) {
            IExpression callExp;
            if (context is FBlangParser.NormalCallStmtContext ncStmt) {
                var parentAndName = ncStmt.ex();
                var args = ncStmt.actualArglist() != null ? VisitActualArglist(ncStmt.actualArglist()) : Array.Empty<IExpression>();
                callExp = GetCall(parentAndName, args, null, fileName);
            }
            else if (context is FBlangParser.InternalExternalCallStmtContext intExt) {
                var name = intExt.Ident().GetText();
                bool isInternal = intExt.InternalCall() != null;
                var args = VisitActualArglist(intExt.actualArglist());
                callExp = GetInternalExternalCall(intExt.Position(fileName), name, args, isInternal, null);
            }
            else//wird nicht auftreten
                callExp = Expression.Error;
            OptimizeCall(ref callExp);
            return new ExpressionStmt(context.Position(fileName), callExp);
        }
        public IStatement VisitIncStmt([NotNull] FBlangParser.IncStmtContext context) {
            var expr = VisitEx(context.ex());
            UnOp.OperatorKind op;
            if (context.pre != null) {
                if (context.IncDec().GetText() == "++")
                    op = UnOp.OperatorKind.INCR_PRE;
                else
                    op = UnOp.OperatorKind.DECR_PRE;
            }
            else {
                if (context.IncDec().GetText() == "++")
                    op = UnOp.OperatorKind.INCR_POST;
                else
                    op = UnOp.OperatorKind.DECR_POST;
            }
            return new ExpressionStmt(context.Position(fileName), Module.Semantics.CreateUnOp(context.Position(fileName), Type.Top, op, expr));
        }
        public YieldStatement VisitYieldStmt([NotNull] FBlangParser.YieldStmtContext context) {
            var expr = context.expr() != null ? VisitExpression(context.expr()) : null;
            currentMethod |= Method.Specifier.Coroutine;
            return new YieldStatement(context.Position(fileName), expr);
        }
        public ReturnStatement VisitReturnStmt([NotNull] FBlangParser.ReturnStmtContext context) {
            var expr = context.expr() != null ? VisitExpression(context.expr(), currentReturnType) : Expression.Error;
            if (!expr.IsError() && currentReturnType.IsByRef() && !expr.IsLValue(currMet)) {
                "Cannot return an rvalue as lvalue".Report(expr.Position);
            }
            return new ReturnStatement(context.Position(fileName), expr);
        }
        public Declaration VisitDeclaration([NotNull] FBlangParser.DeclarationContext context) {
            var (varTy, specs, names, vis) = DeclarationInfos(context.decl());
            Declaration ret;
            bool hasDefault = false;
            if (context.expr() != null && context.LeftArrow() is null) {
                hasDefault = true;
                // one default value for all variables
                var dflt = VisitExpression(context.expr(), varTy);
                if (varTy.IsTop())
                    varTy = dflt.ReturnType;
                ret = new Declaration(context.Position(fileName), varTy, specs, names, dflt, vis);
                UpdateNotNullSet(dflt.IsNotNullable(), ret.Variables);
                if (!dflt.IsNotNullable() && varTy.IsNotNullable()) {
                    $"The non-nullable variables {string.Join(", ", ret.Variables.Select(x => x.Signature))} cannot be initialized with the nullable value{dflt}".Report(context.Position(fileName));
                }

            }
            else if (context.LeftArrow() != null) {
                hasDefault = true;
                if (context.expr() != null) {
                    var range = VisitExpression(context.expr());
                    if (range.ReturnType.UnWrap() is AggregateType agg) {
                        if (varTy.IsTop())
                            varTy = agg.ItemType;
                    }
                    else//TODO deconstruct overloads
                        $"Values of type {range.ReturnType.Signature} cannot be deconstructed in a declaration".Report(range.Position);
                    ret = new DeconstructDeclaration(context.Position(fileName), varTy, specs, names, range, vis);
                }
                else {
                    var randTy = VisitTypeIdent(context.typeIdent(), fileName);
                    ret = new RandomInitDeclaration(context.Position(fileName), varTy, specs, names, randTy, vis);
                }
            }
            else {
                if (varTy.IsTop())
                    $"The type of the variable{(names.Length > 1 ? "s" : "")} {string.Join(", ", names)} cannot be inferred".Report(context.Position(fileName));
                ret = new Declaration(context.Position(fileName), varTy, specs, names, vis: vis);
            }

            if (ret.Type.IsByRef() || ret.Type.IsRef()) {
                //if (!hasDefault)
                //    "References must be initialized on declaration".Report(ret.Position);
                "Reference-variables are not supported yet. They will be added in a future version of the language".Report(ret.Position);
            }
            DoDefineVariables(ret, contextStack.Peek());
            return ret;
        }
        public (IType, Variable.Specifier, string[], Visibility) DeclarationInfos([NotNull] FBlangParser.DeclContext context) {
            IType varTy;
            if (context.localVarTy().Var() != null) {
                varTy = Type.Top;
                if (context.localVarTy().Amp() != null)
                    varTy = varTy.AsByRef();
            }
            else {
                varTy = VisitLocalTypeIdent(context.localVarTy().localTypeIdent(), fileName);
            }

            Variable.Specifier specs = Variable.Specifier.LocalVariable;
            if (context.localVarTy().Final() != null)
                specs |= Variable.Specifier.Final;
            else if (context.localVarTy().Volatile() != null)
                specs |= Variable.Specifier.Volatile;

            var names = context.localVarName().Select(x => {
                var ret = x.GetText();
                /*if (ret.StartsWith('%') && currentMacro == null) {
                    "Local variables which start with a percent-sign can only be declared in a macro-definition".Report(x.Position(fileName));
                }*/
                return ret;
            }).ToArray();
            Visibility vis = context.Public() != null ? Visibility.Public : Visibility.Private;
            if (vis == Visibility.Public && currentMacro is null) {
                "The 'public' modifier for local variables is only allowed in macro-bodies".Report(context.Public().Position(fileName));
            }
            return (varTy, specs, names, vis);
        }
        public (IType, Variable.Specifier, string, Visibility) DeclarationInfos([NotNull] FBlangParser.SdeclContext context) {
            var varTy = context.localVarTy().Var() != null ? Type.Top : VisitLocalTypeIdent(context.localVarTy().localTypeIdent(), fileName);
            Variable.Specifier specs = Variable.Specifier.LocalVariable;
            if (context.localVarTy().Final() != null)
                specs |= Variable.Specifier.Final;
            else if (context.localVarTy().Volatile() != null)
                specs |= Variable.Specifier.Volatile;

            var name = context.localVarName().GetText();
            Visibility vis = context.Public() != null ? Visibility.Public : Visibility.Private;
            if (vis == Visibility.Public && currentMacro is null) {
                "The 'public' modifier for local variables is only allowed in macro-bodies".Report(context.Public().Position(fileName));
            }
            return (varTy, specs, name, vis);
        }
        public Declaration VisitDecl([NotNull] FBlangParser.DeclContext context, IType expectedVarType) {
            var (varTy, specs, names, vis) = DeclarationInfos(context);
            if (varTy.IsTop()) {
                if (expectedVarType is null || expectedVarType.IsTop())
                    "Variables with inferred type must have an initializer".Report(context.Position(fileName));
                else
                    varTy = expectedVarType;
            }
            return new Declaration(context.Position(fileName), varTy, specs, names, vis: vis);
        }
        public DeclarationExpression VisitSDecl([NotNull] FBlangParser.SdeclContext context, IType expectedVarType, bool expectInitializerOnTypeDeduction = true, bool defaultBot = false) {
            var (varTy, specs, name, vis) = DeclarationInfos(context);
            IExpression dflt = null;
            bool needTypeCheckAsPattern = varTy.IsTop();
            if (varTy.IsTop()) {
                if (expectedVarType != null && !expectedVarType.IsTop()) {
                    varTy = expectedVarType;
                }

                if (context.ex() != null) {
                    dflt = VisitEx(context.ex(), expectedVarType);
                    varTy = dflt.ReturnType;
                }
                else if (expectInitializerOnTypeDeduction)
                    "Variables with inferred type must have an initializer".Report(context.Position(fileName));
                else if (defaultBot)
                    varTy = Type.Bot;

            }
            else if (context.ex() != null) {
                dflt = VisitEx(context.ex(), varTy);
                if (!Type.IsAssignable(dflt.ReturnType, varTy)) {
                    $"The initializer of the inline-variable '{name}' with type {dflt.ReturnType.Signature} cannot be assigned to the variable-type {varTy.Signature}".Report(dflt.Position);
                }
            }

            //return new Declaration(context.Position(fileName), varTy, specs, new[] { name }, vis: vis);
            var vr = new BasicVariable(context.localVarName().Position(fileName), varTy, specs, name, null, vis) { DefaultValue = dflt };
            DoDefineVariables(vr);
            return new DeclarationExpression(context.Position(fileName), vr) { TypecheckNecessary = needTypeCheckAsPattern };
        }
        public IStatement VisitDeconstructStmt([NotNull] FBlangParser.DeconstructStmtContext context) {
            var lvalues = VisitActualArglist(context.actualArglist());
            foreach (var x in lvalues.Where(x => !IsLValue(x))) {
                "The deconstruction target does not have an address".Report(x.Position);
            }
            if (context.expr() != null) {
                var err = new ErrorBuffer();
                if (TryVisitExpression(context.expr(), out var expr, null, err)) {
                    //return new Deconstruction(context.Position(fileName), lvalues, expr, true);
                    return Module.Semantics.CreateDeconstruction(context.Position(fileName), lvalues, expr, met: currMet);
                }
                else {
                    var ident = context.expr().Constituents<ITerminalNode>().FirstOrDefault(x =>
                        FBlangParser.DefaultVocabulary.GetSymbolicName(x.Symbol.Type) == "Ident"
                    );
                    if (ident.GetText() == context.expr().GetText()) {
                        var tp = TypeFromSig(new Type.Signature(ident.GetText(), null), ident.Position(fileName));
                        return GetRandomDeconstruction(context.Position(fileName), lvalues, tp);
                    }
                    else {
                        err.Flush();
                        return Statement.Error;
                    }
                }
            }
            else {
                var tp = VisitTypeIdent(context.typeIdent(), fileName);
                return GetRandomDeconstruction(context.Position(fileName), lvalues, tp);
            }
        }
        public BlockStatement VisitBlockInstruction([NotNull] FBlangParser.BlockInstructionContext context) {
            var stmts = new List<IStatement>();
            var innerScope = contextStack.Peek().NewScope(Context.DefiningRules.Variables);
            using (PushContext(innerScope)) {
                MacroCallParameters macroArgs = null;
                InstructionBox box = null;
                var umc = unresolvedMacroCall.Count;
                foreach (var stmt in context.instruction()) {
                    if (unresolvedMacroCall.Any())
                        (macroArgs, box) = unresolvedMacroCall.Pop();
                    var curMacro = currentMacro;
                    if (box != null) {
                        currentMacro = macroArgs.Callee;
                    }
                    var nwStmt = VisitInstruction(stmt);
                    if (box != null) {
                        currentMacro = curMacro;
                        macroArgs.TrySetStatementCapture(macroArgs.Callee.CapturedStatement, nwStmt);
                        macroArgs.MakeReadOnly();
                        macroArgs.Callee.Body.Instruction.TryReplaceMacroParameters(macroArgs, out nwStmt);
                        box.Instruction = nwStmt;
                    }
                    else {
                        stmts.Add(nwStmt);
                    }
                }
                if (box != null) {
                    Console.Write("");
                }
                if (unresolvedMacroCall.Count > umc) {
                    $"The macro-call requires a following statement to capture".Report(macroArgs.Position);
                }
            }
            return new BlockStatement(context.Position(fileName), stmts.ToArray(), innerScope);
        }
        protected override IType VisitTypeIdent([NotNull] FBlangParser.TypeIdentContext context, string fileName) {
            var ret = base.VisitTypeIdent(context, fileName);
            if (!ret.IsError()) {
                var callingCtx = contextStack.Peek();
                var memberDefCtx = ret.DefinedIn;
                var minVis = VisibilityHelper.MinimumVisibility(memberDefCtx, callingCtx);
                if (ret.Visibility < minVis) {
                    $"The {ret.Visibility} type {ret.Signature} is not visible in {contextStack.Peek().Name}".Report(context.Position(fileName));
                }
            }
            return ret;
        }
    }
}
