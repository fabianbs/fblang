/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CompilerInfrastructure;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    using CompilerInfrastructure.Contexts;
    using CompilerInfrastructure.Structure.Types.Generic;
    using CompilerInfrastructure.Utils;
    using System.Linq;
    using static Type.Specifier;
    /// <summary>
    /// Read type-declarations
    /// </summary>
    /// <seealso cref="FBc.FBlangBaseVisitor{object}" />
    class ASTFirstPass : FBlangBaseVisitor<object> {
        readonly string fileName;
        readonly Stack<IContext> contextStack = new Stack<IContext>();
        EquatableLList<string> namespaceQualifier = null;
        readonly List<(IType, FBlangParser.TypeDefContext)> allTypes = new List<(IType, FBlangParser.TypeDefContext)>();
        readonly List<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> allTypeTemplates = new List<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)>();
        readonly List<(IContext, FBlangParser.ExtensionDefContext)> allExtensions = new List<(IContext, FBlangParser.ExtensionDefContext)>();
        public ASTFirstPass(string fileName)
            : this(new FBModule(filename: fileName), fileName) {

        }
        public ASTFirstPass(FBModule mod, string fileName) {
            contextStack.Push(Module = mod);
            this.fileName = fileName;
        }
        public FBModule Module {
            get;
        }
        public IReadOnlyList<(IType, FBlangParser.TypeDefContext)> AllTypes => allTypes;
        public IReadOnlyList<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> AllTypeTemplates => allTypeTemplates;
        public IReadOnlyList<(IContext, FBlangParser.ExtensionDefContext)> AllExtensions => allExtensions;
        IType ParentType() {
            if (contextStack.TryPeek(out IContext res) && res is SimpleTypeContext stc) {
                return stc.Type;
            }
            return null;
        }

        public override object VisitNamespace([NotNull] FBlangParser.NamespaceContext context) {
            var ctx = new HierarchialContext(Module, new[] { contextStack.Peek() }, Context.DefiningRules.All, true);
            contextStack.Push(ctx);
            namespaceQualifier = new EquatableLList<string>(context.Ident()?.GetText() ?? "", namespaceQualifier);
            Module.DefineNamespace(namespaceQualifier, ctx);
            var ret = base.VisitNamespace(context);
            contextStack.Pop();
            namespaceQualifier = namespaceQualifier.Next;
            return ret;
        }

        public override object VisitTypeDef([NotNull] FBlangParser.TypeDefContext context) {
            var classDef = context.classDef();
            var voidDef = context.voidDef();
            var intfDef = context.intfDef();
            var enumDef = context.enumDef();
            var extDef = context.extensionDef();

            if (classDef != null)
                return VisitClassDef(classDef);
            else if (voidDef != null)
                return VisitVoidDef(voidDef);
            else if (intfDef != null)
                return VisitIntfDef(intfDef);
            else if (enumDef != null)
                return VisitEnumDef(enumDef);
            else {
                //extensiondef: 2nd pass
            }
            return base.VisitTypeDef(context);
        }
        public override object VisitClassDef([NotNull] FBlangParser.ClassDefContext context) {
            string name = context.Ident().GetText();
            var dfltVis = contextStack.Peek() is FBModule ? Visibility.Internal : Visibility.Private;
            Visibility vis = context.visibility()?.GetVisibility() ?? dfltVis;

            Type.Specifier spec = None;

            if (context.Abstract() != null)
                spec |= Abstract;
            if (context.Final() != null)
                spec |= NoInheritance;
            if (context.Actor() != null)
                spec |= Actor;

            SimpleTypeContext ctx;
            if (context.genericDef() is null) {
                ctx = SimpleTypeContext.NewScope(contextStack.Peek(), pos: context.Position(fileName));
                ClassType nwTp;
                contextStack.Peek().DefineType(nwTp = new ClassType(context.Position(fileName), name, vis, contextStack.Peek()));
                nwTp.Context = ctx;
                ctx.Type = nwTp;
                nwTp.TypeSpecifiers = spec;
                allTypes.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
            }
            else {
                ctx = SimpleTypeTemplateContext.NewScope(contextStack.Peek(), pos: context.Position(fileName));
                ClassTypeTemplate nwTp;
                IGenericParameter[] genArgs;
                using (contextStack.PushFrame(ctx)) {
                    genArgs = context.genericDef().genericFormalArglist().genericFormalArgument().Select(x => {
                        return (IGenericParameter) VisitGenericFormalArgument(x);
                    }).ToArray();
                }
                contextStack.Peek().DefineTypeTemplate(nwTp = new ClassTypeTemplate(context.Position(fileName), contextStack.Peek(), name, genArgs));
                nwTp.TypeSpecifiers = spec;
                (ctx as ITypeTemplateContext).TypeTemplate = nwTp;
                allTypeTemplates.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
                ctx.Type = nwTp.BuildType();
                foreach (var genArg in genArgs) {
                    if (genArg is GenericTypeParameter genTp)
                        ctx.DefineType(genTp);
                    else if (genArg is GenericLiteralParameter genLit)
                        ctx.DefineVariable(new BasicVariable(genLit.Position, genLit.ReturnType, Variable.Specifier.Final, genLit.Name, ctx.Type));
                }

            }

            //Class, Superclass, Actor, SuperActor in 2nd pass

            contextStack.Push(ctx.StaticContext);
            var ret = base.VisitClassDef(context);
            contextStack.Pop();
            return ret;
        }

        public override object VisitVoidDef([NotNull] FBlangParser.VoidDefContext context) {
            string name = context.Ident().GetText();
            var dfltVis = contextStack.Peek() is FBModule ? Visibility.Internal : Visibility.Private;
            Visibility vis = context.visibility().GetVisibility(dfltVis);
            SimpleTypeContext ctx;
            if (context.genericDef() is null) {
                ctx = SimpleTypeContext.NewScope(contextStack.Peek(), pos: context.Position(fileName));
                ClassType nwTp;
                contextStack.Peek().DefineType(nwTp = new ClassType(context.Position(fileName), name, vis, contextStack.Peek()));
                nwTp.Context = ctx;
                ctx.Type = nwTp;

                nwTp.TypeSpecifiers = NoInheritance;
                allTypes.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
            }
            else {
                ctx = SimpleTypeTemplateContext.NewScope(contextStack.Peek(), pos: context.Position(fileName));
                ClassTypeTemplate nwTp;
                IGenericParameter[] genArgs;
                using (contextStack.PushFrame(ctx)) {
                    genArgs = context.genericDef().genericFormalArglist().genericFormalArgument().Select(x => {
                        return (IGenericParameter) VisitGenericFormalArgument(x);
                    }).ToArray();
                }
                contextStack.Peek().DefineTypeTemplate(nwTp = new ClassTypeTemplate(context.Position(fileName), contextStack.Peek(), name, genArgs));
                (ctx as ITypeTemplateContext).TypeTemplate = nwTp;
                ctx.Type = nwTp.BuildType();
                foreach (var genArg in genArgs) {
                    if (genArg is GenericTypeParameter genTp)
                        ctx.DefineType(genTp);
                    else if (genArg is GenericLiteralParameter genLit)
                        ctx.DefineVariable(new BasicVariable(genLit.Position, genLit.ReturnType, Variable.Specifier.Final, genLit.Name, ctx.Type));
                }


                nwTp.TypeSpecifiers = NoInheritance;
                allTypeTemplates.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));


            }
            //nested classes are defined static (java nested classes would be instance)
            contextStack.Push(ctx.StaticContext);
            var ret = base.VisitVoidDef(context);
            contextStack.Pop();
            return ret;
        }

        public override object VisitEnumDef([NotNull] FBlangParser.EnumDefContext context) {
            string name = context.Ident().GetText();
            var dfltVis = contextStack.Peek() is FBModule ? Visibility.Internal : Visibility.Private;
            Visibility vis = context.visibility().GetVisibility(dfltVis);

            EnumType nwTp;
            contextStack.Peek().DefineType(nwTp = new EnumType(context.Position(fileName), name, ParentType(), vis, contextStack.Peek()));
            allTypes.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
            return base.VisitEnumDef(context);
        }

        public override object VisitIntfDef([NotNull] FBlangParser.IntfDefContext context) {
            string name = context.Ident().GetText();
            var dfltVis = contextStack.Peek() is FBModule ? Visibility.Internal : Visibility.Private;
            Visibility vis = context.visibility().GetVisibility(dfltVis);
            SimpleTypeContext ctx;
            if (context.genericDef() is null) {
                ctx = SimpleTypeContext.NewScope(contextStack.Peek(), pos: context.Position(fileName), defRules: Context.DefiningRules.Methods);
                ClassType nwTp;
                contextStack.Peek().DefineType(nwTp = new ClassType(context.Position(fileName), name, vis, contextStack.Peek()) {
                    Context = ctx,
                    TypeSpecifiers = Interface
                });
                ctx.Type = nwTp;
                allTypes.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
            }
            else {
                ctx = SimpleTypeTemplateContext.NewScope(contextStack.Peek(), pos: context.Position(fileName), defRules: Context.DefiningRules.Methods | Context.DefiningRules.Types);
                ClassTypeTemplate nwTp;
                IGenericParameter[] genArgs;
                using (contextStack.PushFrame(ctx)) {
                    genArgs = context.genericDef().genericFormalArglist().genericFormalArgument().Select(x => {
                        return (IGenericParameter) VisitGenericFormalArgument(x);
                    }).ToArray();
                }
                contextStack.Peek().DefineTypeTemplate(nwTp = new ClassTypeTemplate(context.Position(fileName), contextStack.Peek(), name, genArgs) {
                    TypeSpecifiers = Interface,
                    Context = (SimpleTypeTemplateContext) ctx
                });
                foreach (var genArg in genArgs) {
                    if (genArg is GenericTypeParameter genTp)
                        ctx.DefineType(genTp);
                    else if (genArg is GenericLiteralParameter genLit)
                        "Interfaces cannot define generic literalparameter".Report(genLit.Position);
                }
                allTypeTemplates.Add((nwTp, (FBlangParser.TypeDefContext) context.Parent));
                (ctx as SimpleTypeTemplateContext).TypeTemplate = nwTp;
            }
            // no nested types in interfaces
            return ctx;
        }
        public override object VisitGenericFormalArgument([NotNull] FBlangParser.GenericFormalArgumentContext context) {
            // generate and return IGenericParameter ret
            IGenericParameter ret;
            if (context.genericLiteralParameter() != null) {
                // generic literalparameter
                if (!System.Enum.TryParse(context.genericLiteralParameter().primitiveName().GetText(), out PrimitiveName prim)) {
                    //sollte nicht vorkommen
                    prim = "The type of a generic literalparameter must be primitive".Report(context.Position(fileName), PrimitiveName.Int);
                }
                string name = context.genericLiteralParameter().Ident().GetText();
                ret = new GenericLiteralParameter(context.Position(fileName), name, PrimitiveType.FromName(prim), context.genericLiteralParameter().Unsigned() != null);
            }
            else {
                // generic typeparameter
                var ctx = context.genericTypeParameter();//assert not null
                var name = ctx.Ident().GetText();
                ret = new GenericTypeParameter(ctx.Position(fileName), contextStack.Peek(), name);
            }

            return ret;
        }
        public override object VisitExtensionDef([NotNull] FBlangParser.ExtensionDefContext context) {
            SimpleTypeContext ctx = context.genericDef() is null
                                  ? SimpleTypeContext.NewScope(contextStack.Peek(), pos: context.Position(fileName))
                                  : SimpleTypeTemplateContext.NewScope(contextStack.Peek(), pos: context.Position(fileName));
            contextStack.Push(ctx);
            allExtensions.Add((ctx, context));
            var ret = base.VisitExtensionDef(context);
            contextStack.Pop();
            return ret;
        }
    }
}
