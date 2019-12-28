/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/


using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    using Antlr4.Runtime.Misc;
    using CompilerInfrastructure;
    using CompilerInfrastructure.Contexts;
    using CompilerInfrastructure.Expressions;
    using CompilerInfrastructure.Structure;
    using CompilerInfrastructure.Structure.Macros;
    using CompilerInfrastructure.Structure.Types;
    using CompilerInfrastructure.Structure.Types.Generic;
    using CompilerInfrastructure.Utils;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// Read method- and field-declarations; 
    /// </summary>
    /// <seealso cref="FBc.FBlangBaseVisitor{object}" />
    class ASTThirdPass : ASTSecondPass {
        EquatableLList<string> namespaceQualifier = null;
        readonly List<(BasicVariable, FieldInfo)> allFields
            = new List<(BasicVariable, FieldInfo)>();

        readonly List<(BasicMethod, MethodInfo)> allMethods
            = new List<(BasicMethod, MethodInfo)>();

        readonly List<(BasicMethodTemplate, FBlangParser.MethodDefContext)> allMethodTemplates
            = new List<(BasicMethodTemplate, FBlangParser.MethodDefContext)>();
        readonly List<(MacroFunction, FBlangParser.MacroDefContext)> allMacros
            = new List<(MacroFunction, FBlangParser.MacroDefContext)>();

        public ASTThirdPass(ASTSecondPass secondPassResult)
            : base(secondPassResult.Module, secondPassResult.AllTypes, secondPassResult.AllTypeTemplates) {
            AllFields = allFields;
            AllMethods = allMethods;
            AllMethodTemplates = allMethodTemplates;
            AllMacros = allMacros;
        }
        protected ASTThirdPass(FBModule mod,
            IEnumerable<(IType, FBlangParser.TypeDefContext)> allTypes,
            IEnumerable<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> allTypeTemplates,
            IEnumerable<(BasicVariable, FieldInfo)> allVariables,
            IEnumerable<(BasicMethod, MethodInfo)> allMethods,
            IEnumerable<(BasicMethodTemplate, FBlangParser.MethodDefContext)> allMethodTemplates,
            IEnumerable<(MacroFunction, FBlangParser.MacroDefContext)> allMacros)
            : base(mod, allTypes, allTypeTemplates) {
            AllFields = allVariables;
            AllMethods = allMethods;
            AllMethodTemplates = allMethodTemplates;
            AllMacros = allMacros;
        }
        protected internal ASTThirdPass(FBModule mod,
            IEnumerable<(IType, FBlangParser.TypeDefContext)> allTypes,
            IEnumerable<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> allTypeTemplates)
            : base(mod, allTypes, allTypeTemplates) {
            AllFields = allFields;
            AllMethods = allMethods;
            AllMethodTemplates = allMethodTemplates;
            AllMacros = allMacros;
        }

        protected internal bool ParentIsActor() {
            return contextStack.Peek() is ITypeContext tcx && tcx.Type != null && tcx.Type.IsActor()
                || contextStack.Peek() is ITypeTemplateContext ttcx && ttcx.TypeTemplate.TypeSpecifiers.HasFlag(Type.Specifier.Actor);
        }

        public IEnumerable<(BasicVariable, FieldInfo)> AllFields {
            get;
        }

        public IEnumerable<(BasicMethod, MethodInfo)> AllMethods {
            get;
        }

        public IEnumerable<(BasicMethodTemplate, FBlangParser.MethodDefContext)> AllMethodTemplates {
            get;
        }

        public IEnumerable<(MacroFunction, FBlangParser.MacroDefContext)> AllMacros {
            get;
        }

        protected override GenericLiteralParameter LiteralParameterByName(string name) {
            foreach (var ctx in contextStack) {
                if (ctx is SimpleTypeTemplateContext sttc) {
                    if (sttc.TypeTemplate != null) {
                        var candidates = sttc.TypeTemplate.Signature.GenericFormalparameter.Where(x => x.Name == name);
                        foreach (var ret in candidates) {
                            if (ret is GenericLiteralParameter genLit)
                                return genLit;
                        }
                        if (candidates.Any())
                            break;
                    }
                }
                else if (ctx is SimpleMethodContext smc) {
                    if (smc.MethodTemplate != null) {
                        var candidates = smc.MethodTemplate.Signature.GenericFormalparameter.Where(x => x.Name == name);
                        foreach (var ret in candidates) {
                            if (ret is GenericLiteralParameter genLit)
                                return genLit;
                        }
                        if (candidates.Any())
                            break;
                    }
                }
            }
            return null;
        }
        public void VisitGlobals([NotNull] FBlangParser.ProgramContext context, string fileName) {
            //read method- and fielddeclarations

            //TODO read type-extensions (from ASTFirstPass.AllExtensions)

            //read global methoddeclarations
            foreach (var glob in context.global()) {
                VisitGlobal(glob, fileName);
            }
        }
        void VisitGlobal([NotNull] FBlangParser.GlobalContext glob, string fileName) {
            if (glob.globalMethod() != null) {
                VisitGlobalMethod(glob.globalMethod(), fileName);
            }
            else if (glob.mainMethodDef() != null) {
                VisitMainMethod(glob.mainMethodDef(), fileName);
            }
            else if (glob.@namespace() != null) {
                VisitNamespace(glob.@namespace(), fileName);
            }
            else if (glob.globalMacro() != null) {
                VisitGlobalMacro(glob.globalMacro(), fileName);
            }
            else if (glob.typeDef() != null) {
                // typedefs are already handled in pass1; type-members will be handled in Visit()
                return;
            }
            else {
                throw new ArgumentException("Invalid global construct at " + glob.Position(fileName));
            }
        }

        private void VisitMainMethod(FBlangParser.MainMethodDefContext context, string fileName) {
            var retTy = context.primitiveName() != null
                ? TypeFromSig(new Type.Signature(context.primitiveName().GetText(), null), context.primitiveName().Position(fileName))
                : PrimitiveType.Void;
            var args = context.cmdLineParamList() != null ? VisitCmdLineParamList(context.cmdLineParamList(), fileName) : List.Empty<IVariable>();

            var met = new BasicMethod(context.Position(fileName), "main", Visibility.Public, retTy, args) {
                Specifiers = Method.Specifier.Static
            };
            met.Context = new SimpleMethodContext(Module,
                        new[] { null, contextStack.Peek() },
                        SimpleMethodContext.VisibleMembers.Static,
                        args
                    ) {
                Method = met
            };
            met.NestedIn = contextStack.Peek();
            allMethods.Add((met, new MethodInfo(context)));
            if (!contextStack.Peek().DefineMethod(met).Get(out var fail)) {
                if (fail == CannotDefine.AlreadyExisting)
                    $"The main method is already defined in {contextStack.Peek().Name}".Report(met.Position);
                else
                    $"The main method cannot be defined in {contextStack.Peek().Name}".Report(met.Position);
            }
            // check uniqueness of main in 4th pass.
            // here check, if all parameters are primitive 

            foreach (var arg in met.Arguments) {
                if (arg.Type.IsByRef())
                    "Arguments of the main-function cannot be passed by reference".Report(arg.Position);
                const string errNonPrim = "Arguments of the main-function must be integers, floating-point numbers, strings of arrays/spans of them";
                if (!arg.Type.IsPrimitive()) {
                    if (arg.Type.TryCast<AggregateType>(out var agg)) {
                        if (!agg.ItemType.IsPrimitive())
                            errNonPrim.Report(arg.Position);
                        else if (agg.ItemType.TryCast<PrimitiveType>(out var prim) && prim.PrimitiveName == PrimitiveName.Handle)
                            "Native handles cannot be passed to the main-function".Report(arg.Position);
                    }
                    else
                        errNonPrim.Report(arg.Position);
                }
                else if (arg.Type.TryCast<PrimitiveType>(out var prim) && prim.PrimitiveName == PrimitiveName.Handle)
                    "Native handles cannot be passed to the main-function".Report(arg.Position);
            }
            if (!met.ReturnType.IsVoid() && !met.ReturnType.IsPrimitive(PrimitiveName.Int) && !met.ReturnType.IsPrimitive(PrimitiveName.UInt))
                "The return-type of the main-function must be void, int or uint".Report(met.Position);
        }

        public void Visit() {

            foreach (var tp in AllTypes) {
                VisitTypeDef(tp.Item2, tp.Item1);
            }
            foreach (var ttp in AllTypeTemplates) {
                VisitTypeTemplateDef(ttp.Item2, ttp.Item1);
            }
        }
        protected IType NextTypeContext(IEnumerable<IContext> ctxs) {
            foreach (var ctx in ctxs) {
                if (ctx is ITypeContext tc) {
                    return tc.Type;
                }
                else if (ctx is ITypeTemplateContext ttc) {
                    return ttc.TypeTemplate.BuildType();
                }
                else if (ctx is SimpleMethodContext mcx) {
                    if (mcx.Method != null) {
                        if (mcx.Method.NestedIn != null) {

                            var ret = NextTypeContext(new[] { mcx.Method.NestedIn });
                            if (ret != null)
                                return ret;
                        }
                    }
                    else if (mcx.MethodTemplate != null) {
                        if (mcx.MethodTemplate.NestedIn != null) {
                            var ret = NextTypeContext(new[] { mcx.MethodTemplate.NestedIn });
                            if (ret != null)
                                return ret;
                        }
                    }
                }
                else if (ctx is HierarchialContext hc) {
                    var ret = NextTypeContext(hc.Underlying);
                    if (ret != null)
                        return ret;
                }
            }
            return null;
        }

        protected virtual void VisitNamespace([NotNull] FBlangParser.NamespaceContext context, string fileName) {
            namespaceQualifier = new EquatableLList<string>(context.Ident()?.GetText() ?? "", namespaceQualifier);
            contextStack.Push(Module.NamespaceContext(namespaceQualifier));

            foreach (var glob in context.global()) {
                VisitGlobal(glob, fileName);
            }

            contextStack.Pop();
            namespaceQualifier = namespaceQualifier.Next;
        }

        protected virtual IDeclaredMethod VisitGlobalMethod([NotNull] FBlangParser.GlobalMethodContext context, string fileName) {
            var met = VisitMethodDef(context.methodDef(), fileName, true);

            if (met is BasicMethod m) {

                if (m.IsStatic()) {
                    "The 'static' - modifier is invalid for global methods".Report(context.methodDef().Static().Position(fileName));
                }
                else {
                    m.Specifiers |= Method.Specifier.Static;
                }
                if (m.Visibility == Visibility.Private)
                    m.Visibility = Visibility.Internal;
                else if (m.Visibility == Visibility.Protected)
                    m.Visibility = Visibility.Public;
            }
            else if (met is BasicMethodTemplate tm) {
                if (tm.Specifiers.HasFlag(Method.Specifier.Static)) {
                    "The 'static' - modifier is invalid for global methods".Report(context.methodDef().Static().Position(fileName));
                }
                else {
                    tm.Specifiers |= Method.Specifier.Static;
                }
                if (tm.Visibility == Visibility.Private)
                    tm.Visibility = Visibility.Internal;
                else if (tm.Visibility == Visibility.Protected)
                    tm.Visibility = Visibility.Public;
            }

            return met;
        }
        private void VisitGlobalMacro(FBlangParser.GlobalMacroContext context, string fileName) {
            MacroFunction macro = VisitMacroDef(context.macroDef(), fileName, true);
            if (macro.Visibility == Visibility.Private || macro.Visibility == Visibility.Protected) {
                Console.Error.WriteLine($"Warning: {context.Position(fileName)}: The macro {macro.Name} is defined {macro.Visibility} in global context. The visibility is automatically changed to internal.");
                macro.Visibility = Visibility.Internal;
            }
        }

        private MacroFunction VisitMacroDef(FBlangParser.MacroDefContext context, string fileName, bool globalCtx) {
            var vis = context.visibility().GetVisibility(globalCtx ? Visibility.Internal : Visibility.Private);
            var name = context.macroName.Text;
            VisitMacroArgs(context.macroArgs(), out var requiredArgs, out var optionalArgs, out var blockArg, fileName);

            var ret = new MacroFunction(context.Position(fileName), name, vis, contextStack.Peek(), requiredArgs, optionalArgs, blockArg);
            var ctx = contextStack.Peek() is ITypeContext tcx ? tcx.InstanceContext : contextStack.Peek();
            if (!ctx.DefineMacro(ret).Get(out var fail)) {
                if (fail == CannotDefine.AlreadyExisting)
                    $"{contextStack.Peek().Name} already defines a macro {ret.Name}".Report(ret.Position);
                else
                    $"{contextStack.Peek().Name} cannot define a macro {ret.Name}".Report(ret.Position);
            }
            allMacros.Add((ret, context));
            return ret;
        }
        private void VisitMacroArgs(FBlangParser.MacroArgsContext context, out IReadOnlyDictionary<string, ExpressionParameter> required, out ExpressionParameterPack optional, out StatementParameter block, string fileName) {
            if (context is null) {
                required = Map.Empty<string, ExpressionParameter>();
                optional = null;
                block = null;
            }
            else {
                var req = new Dictionary<string, ExpressionParameter>(context.macroArg().Length);
                bool hasVarArg = false,
                    hasBlock = false;
                optional = null;
                block = null;
                foreach (var argCtx in context.macroArg()) {
                    if (argCtx.Dots() != null) {
                        if (hasVarArg) {
                            "A macro definition can have at most one variadic argument".Report(argCtx.Position(fileName));
                        }
                        else if (hasBlock) {
                            "An expression-parameter-pack cannot be defined behind a statement parameter".Report(argCtx.Position(fileName));
                        }
                        hasVarArg = true;
                        if (argCtx.macroExpressionArg() != null) {
                            optional = new ExpressionParameterPack(argCtx.Position(fileName), argCtx.macroExpressionArg().GetText());
                            if (!req.TryAdd(optional.Name, null)) {
                                $"The parameter {optional.Name} is already defined for this macro".Report(argCtx.Position(fileName));
                            }
                        }
                        else {
                            // statement parameter
                            "A statement-parameter cannot be variadic".Report(argCtx.Position(fileName));
                        }
                    }
                    else {
                        if (argCtx.macroExpressionArg() != null) {
                            if (hasBlock) {
                                "An expression-parameter cannot be defined behind a statement-parameter".Report(argCtx.Position(fileName));
                            }
                            else if (hasVarArg) {
                                "An expression-parameter cannot be defined behind an expression-parameter-pack".Report(argCtx.Position(fileName));
                            }
                            var name = argCtx.macroExpressionArg().GetText();
                            if (!req.TryAdd(name, new ExpressionParameter(argCtx.Position(fileName), name))) {
                                $"The parameter {name} is already defined for this macro".Report(argCtx.Position(fileName));
                            }
                        }
                        else {
                            //statement parameter
                            if (hasBlock) {
                                "A macro definition can have at most one statement-parameter".Report(argCtx.Position(fileName));
                            }
                            hasBlock = true;
                            block = new StatementParameter(argCtx.Position(fileName), argCtx.macroStatementArg().Ident().GetText());
                            if (!req.TryAdd(block.Name, null)) {
                                $"The parameter {block.Name} is already defined for this macro".Report(argCtx.Position(fileName));
                            }
                        }
                    }
                }
                if (optional != null)
                    req.Remove(optional.Name);
                if (block != null)
                    req.Remove(block.Name);
                required = req;
            }
        }
        protected override bool VisitTypeDef([NotNull] FBlangParser.TypeDefContext context, IType tp) {
            contextStack.Push(tp.Context);
            if (context.classDef() != null)
                DefineClassBody(context.classDef().classBody(), tp.Position.FileName);
            else if (context.voidDef() != null)
                DefineClassBody(context.voidDef().classBody(), tp.Position.FileName);
            else if (context.intfDef() != null) {
                DefineInterface(context.intfDef().intfBody(), tp.Position.FileName);
            }
            else if (context.enumDef() != null) {
                //TODO enum
                throw new NotImplementedException();
            }
            contextStack.Pop();
            return true;
        }

        private void DefineInterface([NotNull] FBlangParser.IntfBodyContext context, string fileName) {
            foreach (var metSig in context.methodSig()) {
                VisitMethodSig(metSig, fileName);
            }
        }
        private Method.Signature VisitMethodSigAsSig(FBlangParser.MethodSigContext metSig, string fileName) {
            var name = VisitMethodName(metSig.methodName(), out _);
            IReadOnlyList<IGenericParameter> genArgs = null;
            if (!(metSig.genericDef() is null)) {
                genArgs = VisitGenericDef(metSig.genericDef(), fileName);
                //headerCtx needed for correctly recognizing the argument-types of the method
                SimpleMethodContext headerCtx;
                if (contextStack.Peek() is SimpleTypeContext stc) {
                    headerCtx = new SimpleMethodContext(Module, Context.DefiningRules.All, stc, SimpleMethodContext.VisibleMembers.Instance);
                }
                else {
                    headerCtx = new SimpleMethodContext(Module, new[] { null, contextStack.Peek() }, SimpleMethodContext.VisibleMembers.Static);
                }
                foreach (var genTp in genArgs.OfType<GenericTypeParameter>()) {
                    if (!headerCtx.DefineType(genTp).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} already defines a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                        else
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} cannot define a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                    }
                }
                contextStack.Push(headerCtx);
            }

            IType retTy;
            if (metSig.localTypeIdent() != null) {
                retTy = VisitLocalTypeIdent(metSig.localTypeIdent(), fileName);
            }
            else {
                retTy = PrimitiveType.Void;
                if (metSig.Async() != null)
                    retTy = retTy.AsAwaitable();
            }
            IReadOnlyList<IType> args;
            if (metSig.argTypeList() != null) {
                var _args = new List<IType>(metSig.argTypeList().argTypeIdent().Length);
                //int ind = 1;
                foreach (var argTy in metSig.argTypeList().argTypeIdent()) {
                    //_args.Add(new BasicVariable(argTy.Position(fileName), VisitArgTypeIdent(argTy, fileName), Variable.Specifier.LocalVariable, $"arg{ind}", null));
                    _args.Add(VisitArgTypeIdent(argTy, fileName));
                }
                args = _args;
            }
            else
                args = List.Empty<IType>();
            if (metSig.genericDef() is null) {
                return new Method.Signature(name, retTy, args, null);
            }
            else {
                contextStack.Pop();
                return new Method.Signature(name, retTy, args, genArgs);
            }

        }
        private void VisitMethodSig(FBlangParser.MethodSigContext metSig, string fileName) {
            var name = VisitMethodName(metSig.methodName(), out var isOperatorOverload);
            var vis = Visibility.Public;
            var specs = Method.Specifier.Abstract;
            if (isOperatorOverload)
                specs |= Method.Specifier.OperatorOverload;

            IReadOnlyList<IGenericParameter> genArgs = null;
            if (!(metSig.genericDef() is null)) {
                genArgs = VisitGenericDef(metSig.genericDef(), fileName);
                SimpleMethodContext headerCtx;
                if (contextStack.Peek() is SimpleTypeContext stc) {
                    headerCtx = new SimpleMethodContext(Module, Context.DefiningRules.All, stc, SimpleMethodContext.VisibleMembers.Instance);
                }
                else {
                    headerCtx = new SimpleMethodContext(Module, new[] { null, contextStack.Peek() }, SimpleMethodContext.VisibleMembers.Static);
                }

                foreach (var genTp in genArgs.OfType<GenericTypeParameter>()) {
                    if (!headerCtx.DefineType(genTp).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} already defines a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                        else
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} cannot define a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                    }
                }
                contextStack.Push(headerCtx);
            }
            IType retTy;
            if (metSig.localTypeIdent() != null) {
                retTy = VisitLocalTypeIdent(metSig.localTypeIdent(), fileName);
            }
            else {
                retTy = PrimitiveType.Void;
                if (metSig.Async() != null)
                    retTy = retTy.AsAwaitable();
            }
            if (retTy.IsAwaitable())
                specs |= Method.Specifier.Async;

            IReadOnlyList<IVariable> args;
            if (metSig.argTypeList() != null) {

                var _args = new List<IVariable>(metSig.argTypeList().argTypeIdent().Length);
                int ind = 1;
                foreach (var argTy in metSig.argTypeList().argTypeIdent()) {
                    _args.Add(new BasicVariable(argTy.Position(fileName), VisitArgTypeIdent(argTy, fileName), Variable.Specifier.LocalVariable, $"arg{ind++}", null));
                }
                args = _args;
            }
            else
                args = List.Empty<IVariable>();
            if (metSig.genericDef() is null) {
                var met = new BasicMethod(metSig.Position(fileName), name, vis, retTy, args) { Specifiers = specs };
                met.NestedIn = contextStack.Peek();
                if (contextStack.Peek() is ITypeContext tcx) {
                    if (!tcx.DefineMethod(met).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The method-signature {met.Signature} is already defined in {tcx}".Report(metSig.Position(fileName));
                        else
                            $"The method-signature {met.Signature} cannot be defined in {tcx}".Report(metSig.Position(fileName));
                    }
                }
                else {
                    //FATAL error
                    "A method-signature an only be defined inside an interface".Report(metSig.Position(fileName));
                }
            }
            else {
                var met = new BasicMethodTemplate(metSig.Position(fileName), name, vis, retTy, args, genArgs) { Specifiers = specs };
                // consume the method-context necessary for storing the generic parameters
                contextStack.Pop();
                met.NestedIn = contextStack.Peek();
                if (contextStack.Peek() is ITypeContext tcx) {
                    if (!tcx.DefineMethodTemplate(met).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The method-signature {met.Signature} is already defined in {tcx}".Report(metSig.Position(fileName));
                        else
                            $"The method-signature {met.Signature} cannot be defined in {tcx}".Report(metSig.Position(fileName));
                    }
                }
                else {
                    //FATAL error
                    "A method-signature an only be defined inside an interface".Report(metSig.Position(fileName));
                }
            }

        }

        protected override bool VisitTypeTemplateDef([NotNull] FBlangParser.TypeDefContext context, ITypeTemplate<IType> tp) {
            contextStack.Push(tp.Context);
            if (context.classDef() != null)
                DefineClassBody(context.classDef().classBody(), tp.Position.FileName);
            else if (context.voidDef() != null)
                DefineClassBody(context.voidDef().classBody(), tp.Position.FileName);
            else if (context.intfDef() != null) {
                DefineInterface(context.intfDef().intfBody(), tp.Position.FileName);
            }
            else if (context.enumDef() != null) {
                //TODO generic enum
                throw new NotImplementedException();
            }
            contextStack.Pop();
            return true;
        }

        protected virtual void DefineClassBody([NotNull] FBlangParser.ClassBodyContext context, string fileName) {
            foreach (var memb in context.memberDef()) {
                if (memb.methodDef() != null) {
                    VisitMethodDef(memb.methodDef(), fileName, false);
                }
                else if (memb.fieldDef() != null) {
                    VisitFieldDef(memb.fieldDef(), fileName);
                }
                else if (memb.macroDef() != null) {
                    VisitMacroDef(memb.macroDef(), fileName, false);
                }
                // nested types already handled (read in first pass)
            }
        }
        protected virtual void VisitFieldDef([NotNull] FBlangParser.FieldDefContext context, string fileName) {
            var vis = context.visibility().GetVisibility();
            if (context.includingFieldDef() != null) {
                VisitIncludingFieldDef(context.includingFieldDef(), vis, fileName);
            }
            else {
                var names = context.names().Ident();
                var varTp = VisitVarType(context.varType(), out var specs, fileName);

                //we only define fields in types
                if (contextStack.Peek() is ITypeContext stc) {
                    bool isStatic = specs.HasFlag(Variable.Specifier.Static);
                    var ctx = isStatic ? stc.StaticContext : stc.InstanceContext;
                    //TODO deconstruction-initialization
                    foreach (var name in names) {
                        var ret = new BasicVariable(name.Position(fileName), varTp, specs, name.GetText(), stc.Type, vis) {
                            Parent = ctx
                        };
                        allFields.Add((ret, new FieldInfo(context.expr())));
                        if (!ctx.DefineVariable(ret).Get(out var fail)) {
                            if (fail == CannotDefine.AlreadyExisting)
                                $"The {(isStatic ? "static" : "instance")} field '{ret.Signature}' is already defined".Report(name.Position(fileName));
                            else
                                $"The {(isStatic ? "static" : "instance")} field '{ret.Signature}' cannot be defined".Report(name.Position(fileName));
                        }
                    }
                }
                else {
                    // wird nicht auftreten
                    $"Defining fields is not allowed in {contextStack.Peek()}".Report(context.Position(fileName));
                }
            }
        }

        protected virtual void VisitIncludingFieldDef(FBlangParser.IncludingFieldDefContext context, Visibility vis, string fileName) {
            var name = context.Ident().GetText();
            var varTp = VisitVarType(context.varType(), out var specs, fileName);
            //we only define fields in types
            if (contextStack.Peek() is ITypeContext stc) {
                bool isStatic = specs.HasFlag(Variable.Specifier.Static);
                if (isStatic)
                    "An including-field cannot be static".Report(context.varType().Static().Position(fileName));
                var ctx = isStatic ? stc.StaticContext : stc.InstanceContext;
                var ret = new BasicVariable(context.Ident().Position(fileName), varTp, specs, name, stc.Type, vis) {
                    Parent = ctx
                };
                IType includingInterface;
                if (context.As() != null) {
                    includingInterface = TypeFromSig(VisitTypeQualifier(context.typeQualifier(), fileName), context.typeQualifier().Position(fileName));
                    /*if (!includingInterface.IsError() && !varTp.IsSubTypeOf(includingInterface)) {
                        $"The ascription-type {includingInterface.Signature} is not a supertype of the field-type {varTp.Signature}".Report();
                    }*/
                }
                else
                    includingInterface = varTp;
                ISet<Method.Signature> notInclude = null;
                if (context.Except() != null) {
                    notInclude = context.methodSig().Select(x => VisitMethodSigAsSig(x, fileName)).ToHashSet();
                }

                allFields.Add((ret, new FieldInfo(context.expr(), true, includingInterface, notInclude)));
                if (!ctx.DefineVariable(ret).Get(out var fail)) {
                    if (fail == CannotDefine.AlreadyExisting)
                        $"The {(isStatic ? "static" : "instance")} field '{ret.Signature}' is already defined".Report(context.Ident().Position(fileName));
                    else
                        $"The {(isStatic ? "static" : "instance")} field '{ret.Signature}' cannot be defined".Report(context.Ident().Position(fileName));
                }
            }
            else {
                // wird nicht auftreten
                $"Defining fields is not allowed in {contextStack.Peek()}".Report(context.Position(fileName));
            }
        }

        protected virtual bool TryVisitLiteral(FBlangParser.LiteralContext lit, out IExpression ret, IType expectedReturnType, ErrorBuffer err, string fileName) {
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
                ret = new DefaultValueExpression(lit.Position(fileName), expectedReturnType ?? Type.Top);
            }

            else {
                //Ident
                var vr = contextStack.Peek().VariableByName(lit.Ident().GetText());
                if (vr != null) {
                    if (vr.IsStatic() || !vr.IsStatic() && vr.IsLocalVariable()) {
                        ret = new VariableAccessExpression(lit.Position(fileName), null, vr);
                    }
                    else {
                        ret = _err.Report($"Cannot access variable {vr.Signature} from {contextStack.Peek().Name}", lit.Position(fileName), Expression.Error);
                    }
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
        protected virtual IType VisitVarType([NotNull] FBlangParser.VarTypeContext context, out Variable.Specifier specs, string fileName) {
            specs = Variable.Specifier.None;
            if (context.Final() != null)
                specs |= Variable.Specifier.Final;
            else if (context.Volatile() != null)
                specs |= Variable.Specifier.Volatile;

            if (context.Static() != null)
                specs |= Variable.Specifier.Static;



            var ret = VisitTypeIdent(context.typeIdent(), fileName);

            return ret;
        }
        protected virtual IDeclaredMethod VisitMethodDef([NotNull] FBlangParser.MethodDefContext context, string fileName, bool globalCtx) {
            var name = VisitMethodName(context.methodName(), out var isOperatorOverload);
            var vis = context.visibility().GetVisibility(globalCtx ? Visibility.Internal : Visibility.Private);
            var specs = Method.Specifier.None;

            //TODO do static analysis for determining Method::HasUniqueThis
            specs |= Method.Specifier.UniqueThis;


            if (context.Abstract() != null)
                specs |= Method.Specifier.Abstract;
            else if (context.Virtual() != null)
                specs |= Method.Specifier.Virtual;
            else if (context.Static() != null)
                specs |= Method.Specifier.Static;


            if (name == "ctor") {
                specs |= Method.Specifier.Constructor;
            }
            else if (name == "dtor") {
                specs |= Method.Specifier.Destructor;
                if (context.Static() != null) {
                    "A destructor must not be static".Report(context.Position(fileName));
                }
            }

            if (context.Init() != null) {
                specs |= FBMethodSpecifier.InitCtor;
                if (specs.HasFlag(Method.Specifier.Static))
                    "The 'init' keyword cannot be applied to static methods".Report(context.Init().Position(fileName));
            }
            else if (context.Copy() != null) {
                if (!specs.HasFlag(Method.Specifier.Constructor))
                    "The 'copy' keyword can only be applied to copy-constructors".Report(context.Copy().Position(fileName));
                else if (specs.HasFlag(Method.Specifier.Static))
                    "The 'copy' keyword cannot be applied to static constructors".Report(context.Init().Position(fileName));
                specs |= FBMethodSpecifier.CopyCtor;
            }

            var smcVis = specs.HasFlag(Method.Specifier.Static) ? SimpleMethodContext.VisibleMembers.Static : SimpleMethodContext.VisibleMembers.Both;

            if (isOperatorOverload) {
                //DOLATER check if operator-overload is allowed here
                specs |= Method.Specifier.OperatorOverload;
            }

            IReadOnlyList<IGenericParameter> genArgs = null;
            if (context.genericDef() != null) {
                genArgs = VisitGenericDef(context.genericDef(), fileName);
                SimpleMethodContext headerCtx;
                if (contextStack.Peek() is SimpleTypeContext stc) {
                    headerCtx = new SimpleMethodContext(Module, Context.DefiningRules.All, stc, smcVis);
                }
                else {
                    headerCtx = new SimpleMethodContext(Module, new[] { null, contextStack.Peek() }, SimpleMethodContext.VisibleMembers.Static);
                }

                foreach (var genTp in genArgs.OfType<GenericTypeParameter>()) {
                    if (!headerCtx.DefineType(genTp).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} already defines a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                        else
                            $"The generic method {new MethodTemplate.Signature(name, genArgs)} cannot define a generic typeparameter '{genTp.Signature.Name}'".Report(genTp.Position);
                    }
                }
                contextStack.Push(headerCtx);
            }


            IType retTp;
            if (context.localTypeIdent() != null) {
                retTp = VisitLocalTypeIdent(context.localTypeIdent(), fileName);
                if (retTp.IsAwaitable()) {
                    specs |= Method.Specifier.Async;
                }
                else if (ParentIsActor() && vis > Visibility.Private && !specs.HasFlag(Method.Specifier.Static)) {
                    "All non-private member-functions of an actor must be async".Report(context.Position(fileName));
                }
                if (specs.HasFlag(Method.Specifier.Constructor)) {
                    "Constructors must not have a return-type".Report(context.methodName().Position(fileName));
                }
            }
            else {
                retTp = PrimitiveType.Void;
                if (context.Async() != null) {
                    retTp = retTp.AsAwaitable();
                    specs |= Method.Specifier.Async;
                }
                else if (ParentIsActor() && vis > Visibility.Private && !specs.HasFlag(Method.Specifier.Static)) {
                    "All non-private member-functions of an actor must be async".Report(context.Position(fileName));
                }
                else if (isOperatorOverload) {
                    // For converters it is ok to not specify the type twice
                    var opName = name.Split(" ").Last();
                    if (opName == OverloadableOperator.String.OperatorName()) {
                        retTp = PrimitiveType.String;
                    }
                    else if (opName == OverloadableOperator.Bool.OperatorName()) {
                        retTp = PrimitiveType.Bool;
                    }
                }
            }

            if (retTp.IsAwaitable()) //The method is async if the returntype is async
                specs |= Method.Specifier.Async;
            if (ParentIsActor()) {
                specs |= Method.Specifier.Coroutine;
            }

            IReadOnlyList<IVariable> args = context.argList() != null
                ? VisitArgList(context.argList(), fileName)
                : List.Empty<IVariable>();

            if (specs.HasFlag(FBMethodSpecifier.InitCtor) && !args.Any()) {
                "The init-modifier has no effect on parameterless methods".Warn(context.Init().Position(fileName), WarningLevel.Low);
            }
            else if (specs.HasFlag(FBMethodSpecifier.CopyCtor)) {
                var tp = NextTypeContext(contextStack);
                if (args.Count != 1 || args[0].Type.IsValueType() || args[0].Type.UnWrapAll() != tp)
                    "A copy-constructor must have exact one parameter: a reference to an object of the enclosing type".Report(context.Copy().Position(fileName));

            }

            foreach (BasicVariable vr in args) {
                vr.VariableSpecifiers |= Variable.Specifier.LocalVariable;
            }
            if (context.genericDef() is null) {

                var met = new BasicMethod(context.Position(fileName), name, vis, retTp, args) { Specifiers = specs };
                allMethods.Add((met, new MethodInfo(context)));
                if (contextStack.Peek() is SimpleTypeContext stc) {
                    bool isStatic = met.IsStatic();

                    met.Context = new SimpleMethodContext(stc.Module,
                        Context.DefiningRules.Variables,
                        stc,
                        smcVis,
                        args
                    ) {
                        Method = met
                    };
                    met.NestedIn = stc;

                    var ctx = isStatic ? stc.StaticContext : stc.InstanceContext;
                    if (!ctx.DefineMethod(met).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The {(isStatic ? "static" : "instance")} method '{met.Signature}' is already defined in {stc}".Report(met.Position);
                        else
                            $"The {(isStatic ? "static" : "instance")} method '{met.Signature}' cannot be defined in {stc}".Report(met.Position);
                    }
                }
                else {//global method
                    met.Context = new SimpleMethodContext(Module,
                        new[] { null, contextStack.Peek() },
                        SimpleMethodContext.VisibleMembers.Static,
                        args
                    ) {
                        Method = met
                    };
                    met.NestedIn = contextStack.Peek();
                    if (!contextStack.Peek().DefineMethod(met).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The global method '{met.Signature}' is already defined in {contextStack.Peek().Name}".Report(met.Position);
                        else
                            $"the global method '{met.Signature}' cannot be defined in {contextStack.Peek().Name}".Report(met.Position);
                    }
                }
                return met;
            }
            else {
                // method-template
                var tm = new BasicMethodTemplate(context.Position(fileName), name, vis, retTp, args, genArgs) { Specifiers = specs };
                allMethodTemplates.Add((tm, context));
                var smc = contextStack.Pop() as SimpleMethodContext;
                smc.SetArguments(args);
                smc.MethodTemplate = tm;
                tm.Context = smc;
                if (contextStack.Peek() is SimpleTypeContext stc) {
                    bool isStatic = tm.Specifiers.HasFlag(Method.Specifier.Static);
                    var ctx = isStatic ? stc.StaticContext : stc.InstanceContext;
                    if (!ctx.DefineMethodTemplate(tm).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The {(isStatic ? "static" : "instance")} method '{tm.Signature}' is already defined in {stc}".Report(tm.Position);
                        else
                            $"The {(isStatic ? "static" : "instance")} method '{tm.Signature}' cannot be defined in {stc}".Report(tm.Position);
                    }

                }
                else {
                    if (!contextStack.Peek().DefineMethodTemplate(tm).Get(out var fail)) {
                        if (fail == CannotDefine.AlreadyExisting)
                            $"The global method '{tm.Signature}' is already defined in {contextStack.Peek()}".Report(tm.Position);
                        else
                            $"The global method '{tm.Signature}' cannot be defined in {contextStack.Peek()}".Report(tm.Position);
                    }
                }
                tm.NestedIn = contextStack.Peek();
                return tm;
            }
        }
        protected virtual IReadOnlyList<IGenericParameter> VisitGenericDef([NotNull] FBlangParser.GenericDefContext context, string fileName) {
            var genArgs = context.genericFormalArglist().genericFormalArgument();
            var ret = new List<IGenericParameter>(genArgs.Length);
            foreach (var arg in genArgs) {
                ret.Add(VisitGenericFormalArgument(arg, fileName));
            }
            return ret;
        }
        protected virtual IGenericParameter VisitGenericFormalArgument([NotNull] FBlangParser.GenericFormalArgumentContext context, string fileName) {
            if (context.genericLiteralParameter() != null) {
                return VisitGenericLiteralParameter(context.genericLiteralParameter(), fileName);
            }
            else if (context.genericTypeParameter() != null) {
                return VisitGenericTypeParameter(context.genericTypeParameter(), fileName);
            }
            //wird nicht auftreten
            return null;
        }
        protected virtual GenericLiteralParameter VisitGenericLiteralParameter([NotNull] FBlangParser.GenericLiteralParameterContext context, string fileName) {
            if (!Enum.TryParse(context.primitiveName().GetText(), out PrimitiveName prim)) {
                //sollte nicht vorkommen
                prim = "The type of a generic literalparameter must be primitive".Report(context.Position(fileName), PrimitiveName.Int);
            }
            string name = context.Ident().GetText();
            return new GenericLiteralParameter(context.Position(fileName), name, PrimitiveType.FromName(prim), context.Unsigned() != null);
        }
        protected virtual GenericTypeParameter VisitGenericTypeParameter([NotNull] FBlangParser.GenericTypeParameterContext context, string fileName) {
            var name = context.Ident().GetText();
            IHierarchialType superTp = null;
            var implementingInterfaces = new List<IType>();
            if (context.typeQualifier() != null) {
                var sup = TypeFromSig(VisitTypeQualifier(context.typeQualifier(), fileName), context.typeQualifier().Position(fileName));
                if (sup is IHierarchialType htp)
                    superTp = htp;
                else if (!sup.IsError())
                    $"The type {sup.Signature} cannot be a supertype-constraint of a generic typeparameter".Report(context.typeQualifier().Position(fileName));
            }
            if (context.intfList() != null) {
                foreach (var qual in context.intfList().typeQualifier()) {
                    var tp = TypeFromSig(VisitTypeQualifier(qual, fileName), qual.Position(fileName));
                    if (tp.IsInterface())
                        implementingInterfaces.Add(tp);
                    else if (!tp.IsError())
                        $"The non-interface type {tp.Signature} cannot be an interface-constraint of a generic typeparameter".Report(qual.Position(fileName));
                }
            }
            return new GenericTypeParameter(context.Position(fileName), contextStack.Peek(), name, superTp, implementingInterfaces.ToArray());
        }
        protected string VisitMethodName([NotNull]FBlangParser.MethodNameContext context, out bool isOperatorOverload) {
            if (context.Ident() != null) {
                isOperatorOverload = false;
                return context.Ident().GetText();
            }
            else {
                isOperatorOverload = true;
                return $"operator {context.overloadableOperator().GetText().Replace(" ", "").Replace("\t", "")}";
            }
        }
        private IReadOnlyList<IVariable> VisitCmdLineParamList([NotNull] FBlangParser.CmdLineParamListContext context, string fileName) {
            var args = new List<IVariable>();
            foreach (var cmdParams in context.cmdLineParams()) {
                VisitCmdLineParams(cmdParams, args, fileName);
            }
            return args;
        }
        private void VisitCmdLineParams([NotNull] FBlangParser.CmdLineParamsContext context, IList<IVariable> args, string fileName) {
            var argTp = VisitTypeIdent(context.typeIdent(), fileName);
            foreach (var param in context.cmdLineParam()) {
                VisitCmdLineParam(param, args, argTp, fileName);
            }
        }
        private void VisitCmdLineParam([NotNull] FBlangParser.CmdLineParamContext context, IList<IVariable> args, IType tp, string fileName) {
            var name = context.Ident().GetText();
            string desc = "";
            IExpression dflt = null;
            if (context.StringLit() != null) {
                var litValue = context.StringLit().GetText();
                desc = Literal.SolveStringEscapes(context.StringLit().Position(fileName), litValue.AsSpan(1, litValue.Length - 2)).Value;
            }
            if (context.literal() != null) {
                TryVisitLiteral(context.literal(), out dflt, tp, null, fileName);
            }
            args.Add(new CmdLineArgument(context.Ident().Position(fileName), tp, name, dflt, desc));
        }
        protected virtual IReadOnlyList<IVariable> VisitArgList([NotNull] FBlangParser.ArgListContext context, string fileName) {
            var args = new List<IVariable>();
            foreach (var tparam in context.typedParam()) {
                VisitTypedParam(tparam, args, fileName);
            }
            return args;
        }
        protected virtual void VisitTypedParam([NotNull] FBlangParser.TypedParamContext context, IList<IVariable> args, string fileName) {
            var specs = Variable.Specifier.None;
            if (context.Final() != null)
                specs |= Variable.Specifier.Final;
            else if (context.Volatile() != null)
                specs |= Variable.Specifier.Volatile;


            var argTp = VisitArgTypeIdent(context.argTypeIdent(), fileName);

            if (context.Dollar() != null)
                argTp = argTp.AsValueType();
            foreach (var id in context.Ident()) {
                args.Add(new BasicVariable(id.Position(fileName), argTp, specs, id.GetText(), null));
            }
        }
        public virtual IType VisitArgTypeIdent([NotNull] FBlangParser.ArgTypeIdentContext context, string fileName) {
            var ret = VisitLocalTypeIdent(context.localTypeIdent(), fileName);
            
            if (context.Dots() != null) {
                ret = ret.AsVarArg();
            }
            return ret;
        }
        public virtual bool TryVisitArgTypeIdent([NotNull] FBlangParser.ArgTypeIdentContext context, out IType ret, ErrorBuffer err, string fileName) {
            if (!TryVisitLocalTypeIdent(context.localTypeIdent(), out ret, err, fileName)) {
                ret = Type.Error;
                return false;
            }
            
            if (context.Dots() != null) {
                ret = ret.AsVarArg();
            }
            return true;
        }
        public virtual IType VisitLocalTypeIdent([NotNull]FBlangParser.LocalTypeIdentContext context, string fileName) {
            var ret = VisitTypeIdent(context.typeIdent(), fileName);
            if (context.Pointer() != null) {
                ret = ret.AsSpan();
            }
            if (context.Amp() != null) {
                ret = ret.AsByRef();
            }
            return ret;
        }
        public virtual bool TryVisitLocalTypeIdent([NotNull]FBlangParser.LocalTypeIdentContext context, out IType ret, ErrorBuffer err, string fileName) {
            if (!TryVisitTypeIdent(context.typeIdent(), out ret, err, fileName)) {
                ret = Type.Error;
                return false;
            }
            if (context.Pointer() != null) {
                ret = ret.AsSpan();
            }
            if (context.Amp() != null) {
                ret = ret.AsByRef();
            }
            return true;
        }
    }
}
