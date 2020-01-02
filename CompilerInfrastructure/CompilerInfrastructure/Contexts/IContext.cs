/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Contexts.DataFlowAnalysis;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Summaries;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure {
    using Structure.Types;
    using DefResult = BooleanResult<CannotDefine>;

    public interface IContext : IPositional, IReplaceableStructureElement<IContext>/*,ISummarizable*/ {

        IReadOnlyDictionary<Type.Signature, IType> Types {
            get;
        }
        IReadOnlyDictionary<TypeTemplate.Signature, ITypeTemplate<IType>> TypeTemplates {
            get;
        }
        IReadOnlyDictionary<Method.Signature, IMethod> Methods {
            get;
        }
        IReadOnlyDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>> MethodTemplates {
            get;
        }
        IReadOnlyDictionary<Variable.Signature, IVariable> Variables {
            get;
        }
        IReadOnlyDictionary<StringSignature, MacroFunction> Macros {
            get;
        }

        IReadOnlyDictionary<StringSignature, object> Identifiers {
            get;
        }

        //DataFlowSet DataFlowSet { get; }

        IEnumerable<IMethod> MethodsByName(string name);
        IEnumerable<IMethodTemplate<IMethod>> MethodTemplatesByName(string name);
        IEnumerable<IType> TypesByName(string name);
        IEnumerable<ITypeTemplate<IType>> TypeTemplatesByName(string name);
        IEnumerable<IVariable> VariablesByName(string name);
        IEnumerable<MacroFunction> MacrosByName(string name);
        IEnumerable<object> IdentifiersByName(string name);
        IContext LocalContext {
            get;
        }
        bool CanDefineTypes {
            get;
        }
        bool CanDefineVariables {
            get;
        }
        bool CanDefineMethods {
            get;
        }
        Context.DefiningRules DefiningRules {
            get;
        }
        DefResult DefineType(IType tp);
        DefResult DefineTypeTemplate(ITypeTemplate<IType> tp);
        DefResult DefineMethod(IMethod met);
        DefResult DefineMethodTemplate(IMethodTemplate<IMethod> met);
        DefResult DefineMacro(MacroFunction m);
        DefResult DefineIdentifier(string sig, object o);
        DefResult DefineVariable(IVariable vr);
        //void DefineDataFlowFact(IDataFlowFact fact, IDataFlowValue val);
        Module Module {
            get;
        }
        string Name {
            get;
        }
    }
    public interface IWrapperContext : IContext {
        IContext Underlying {
            get;
        }
    }
    /// <summary>
    /// Provides nested classes and static methods for <see cref="IContext"/>
    /// </summary>
    public abstract class Context {
        static readonly LazyDictionary<Module, IContext> immu = new LazyDictionary<Module, IContext>(x => new BasicContext(x, DefiningRules.None, true));
        [Flags]
        public enum DefiningRules {
            None = 0,
            Types = 1,
            Variables = 2,
            Methods = 4,
            All = ~0
        }
        public static IContext Combine(params IContext[] ctxs) {
            return new HierarchialContext(ctxs?.FirstOrDefault()?.Module, ctxs ?? Array.Empty<IContext>(), DefiningRules.None, false);
        }
        public static IContext Constrained(IContext ctx, Visibility vis) {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));
            switch (ctx) {
                case ConstrainedContext cctx: {
                    if (cctx.Constraint >= vis) {
                        return cctx;
                    }
                    else {
                        return new ConstrainedContext(cctx.underlying, vis);
                    }
                }
                case SimpleTypeTemplateContext sttc: {
                    return Constrained(sttc, vis);
                }
                case ITypeContext stc: {
                    return Constrained(stc, vis);
                }
                default: {
                    return new ConstrainedContext(ctx, vis);
                }
            }
        }
        public static SimpleTypeContext Constrained(ITypeContext ctx, Visibility vis) {
            if (ctx is SimpleTypeTemplateContext sttc) {
                return Constrained(sttc, vis);
            }
            return new SimpleTypeContext(
                ctx.Module,
                DefiningRules.None,
                Constrained(ctx.InstanceContext, vis),
                Constrained(ctx.StaticContext, vis), ctx.Position) {
                Type = ctx.Type
            };
        }
        public static SimpleTypeTemplateContext Constrained(SimpleTypeTemplateContext ctx, Visibility vis) {
            return new SimpleTypeTemplateContext(
                ctx.Module,
                DefiningRules.None,
                Constrained(ctx.InstanceContext, vis),
                Constrained(ctx.StaticContext, vis), ctx.Position) {
                TypeTemplate = ctx.TypeTemplate
            };
        }
        public static IContext Immutable {
            get;
        } = new BasicContext(new Module(), DefiningRules.None, true);
        public static IContext GetImmutable(Module mod) {
            return immu[mod];
        }
    }
    /// <summary>
    /// Provides Extension-methods for <see cref="IContext"/>
    /// </summary>
    public static class ContextHelper {
        public static IVariable VariableByName(this IContext ctx, string name) {
            return ctx.VariablesByName(name).FirstOrDefault();
        }

        public static IVariable TryGetVariableByName(this IContext ctx, string name, Position pos) {

            var vrs = ctx.VariablesByName(name);
            return vrs.IsSingeltonOrEmpty(out var val) switch
            {
                true => val,
                false => $"The actual variable is ambiguous for the name '{name}'. Possible variables are: {string.Join(", ", vrs)}".Report(pos, val),
                null => null
            };
        }
        public static MacroFunction MacroByName(this IContext ctx, string name) {
            return ctx.MacrosByName(name).FirstOrDefault();
        }
        public static object IdentifierByName(this IContext ctx, string name) {
            return ctx.IdentifiersByName(name).FirstOrDefault();
        }
        public static T IdentifierByName<T>(this IContext ctx, string name) {
            return ctx.IdentifiersByName(name).OfType<T>().FirstOrDefault();
        }
        public static IContext NewScope(this IContext ctx, Context.DefiningRules defining) {
            return new HierarchialContext(ctx.Module, new[] { ctx }, defining, false);
        }
        public static IContext NewScope(this IContext ctx, bool inheritDefRulesOrNone) {
            return new HierarchialContext(ctx.Module, new[] { ctx }, inheritDefRulesOrNone ? ctx.DefiningRules : Context.DefiningRules.None, false);
        }
        public static ITypeContext NextTypeContext(this IContext ctx) {
            if (ctx is ITypeContext stctx)
                return stctx;
            else if (ctx is HierarchialContext hctx) {
                if (ctx is SimpleMethodContext smc && smc.DefinedInType != null) {
                    return smc.DefinedInType.Context;
                }
                return hctx.Underlying.Select(x => NextTypeContext(x)).FirstOrDefault(x => x != null);
            }

            return null;
        }
        public static IContext FirstWhere(this IContext ctx, Predicate<IContext> cond) {
            if (cond is null || ctx is null)
                return ctx;
            while (ctx != null) {
                if (cond(ctx))
                    return ctx;
                switch (ctx) {
                    case IStructTypeContext stc: {
                        ctx = stc.StaticContext;
                        break;
                    }
                    case IWrapperContext wc: {
                        ctx = wc.Underlying;
                        break;
                    }
                    case HierarchialContext hc: {
                        foreach (var sub in hc) {
                            // assume no circular dependencies
                            var ret = sub.FirstWhere(cond);
                            if (ret != null)
                                return ret;
                        }
                        ctx = null;
                        break;
                    }
                    default: {
                        ctx = null;
                        break;
                    }
                }
            }
            return null;
        }
        public static IEnumerable<IDeclaredMethod> DeclaredMethodsByName(this IContext ctx, string name) {
            return (ctx.MethodsByName(name) as IEnumerable<IDeclaredMethod>).Concat(ctx.MethodTemplatesByName(name));
        }
    }
}
