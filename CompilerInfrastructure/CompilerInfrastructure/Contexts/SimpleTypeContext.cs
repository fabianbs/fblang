using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using static CompilerInfrastructure.Context.DefiningRules;
namespace CompilerInfrastructure {
    [Serializable]
    public class SimpleTypeContext : HierarchialContext, ITypeContext {
        public SimpleTypeContext(Module mod, Context.DefiningRules defRules = Context.DefiningRules.All, Position pos = default)//DOLATER: Not sure whether or nor to serialize non-public members
            : this(mod, defRules, new BasicContext(mod, Context.DefiningRules.All, false, pos), new BasicContext(mod, Context.DefiningRules.All, false, pos)) {

        }
        public SimpleTypeContext(Module mod, Context.DefiningRules defRules, IContext instanceCtx, IContext staticCtx, Position pos = default)
            : base(mod, new[] { instanceCtx, staticCtx }, true, false) {
            InstanceContext = instanceCtx;
            StaticContext = staticCtx;
            Position = pos;
        }
        public IContext InstanceContext {
            get;
        }
        public IContext StaticContext {
            get;
        }

        public virtual IType Type {
            get;
            set;
        }
        static readonly LazyDictionary<Module, SimpleTypeContext> immu = new LazyDictionary<Module, SimpleTypeContext>(mod =>
            new SimpleTypeContext(mod, Context.DefiningRules.None, new BasicContext(mod, Context.DefiningRules.None, true), new BasicContext(mod, Context.DefiningRules.None, true))
        );
        public static SimpleTypeContext GetImmutable(Module mod) {
            return immu[mod];
        }
        /// <summary>
        /// Creates a new Context for a Subclass or a nested class
        /// </summary>
        /// <param name="minParentVis">The minimum Visibility of the parent members to be accessible in the new context;
        /// usually <see cref="Visibility.Private"/> for nested classes and <see cref="Visibility.Protected"/> for Subclasses</param>
        /// <returns></returns>
        public virtual SimpleTypeContext NewScope(Context.DefiningRules defRules = Context.DefiningRules.All, Position pos = default, IType type = null) {

            return new SimpleTypeContext(Module, DefiningRules,
                new HierarchialContext(Module, new[] { InstanceContext }, defRules, type is null ? false : !type.MayRelyOnGenericParameters()),
                new HierarchialContext(Module, new[] { StaticContext }, defRules, type is null ? false : !type.MayRelyOnGenericParameters())) {
                Type = type
            };
        }
        public static SimpleTypeContext NewScope(IContext ctx, Context.DefiningRules defRules = Context.DefiningRules.All, Position pos = default, IType type = null) {
            if (ctx is SimpleTypeContext stc)
                return stc.NewScope(defRules, pos, type);
            return new SimpleTypeContext(ctx.Module, defRules,
                new BasicContext(ctx.Module, defRules, type is null ? false : !type.MayRelyOnGenericParameters(), pos),
                new HierarchialContext(ctx.Module, new[] { ctx }, defRules, type is null ? false : !type.MayRelyOnGenericParameters())) {
                Type = type
            };
        }
        public static SimpleTypeContext Merge(Module mod, IEnumerable<ITypeContext> ctxs, Context.DefiningRules defRules) {
            return new SimpleTypeContext(mod, defRules,
                ctxs.Any() || defRules != Context.DefiningRules.None ? new HierarchialContext(mod, ctxs.Select(x => x.InstanceContext), defRules, false) : Context.GetImmutable(mod),
                ctxs.Any() || defRules != Context.DefiningRules.None ? new HierarchialContext(mod, ctxs.Select(x => x.StaticContext), defRules, false) : Context.GetImmutable(mod)) {
                Type = ctxs.FirstOrDefault()?.Type
            };
        }
        public override IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                var nwInst = InstanceContext.Replace(genericActualParameter, curr, parent);
                var nwStat = StaticContext.Replace(genericActualParameter, curr, parent);
                ret = new SimpleTypeContext(Module, DefiningRules,
                    nwInst,
                    nwStat
                );
                genericCache.TryAdd((genericActualParameter, curr), ret);
            }
            return (SimpleTypeContext) ret;
        }
        public override string Name {
            get {
                return Type != null ? Type.Signature.ToString() : base.Name;
            }
        }
    }
    public class SimpleTypeTemplateContext : SimpleTypeContext, ITypeTemplateContext {

        public SimpleTypeTemplateContext(Module mod, Context.DefiningRules defRules = (Context.DefiningRules) (-1), Position pos = default)
            : base(mod, defRules, pos) {
        }

        public SimpleTypeTemplateContext(Module mod, Context.DefiningRules defRules, IContext instanceCtx, IContext staticCtx, Position pos = default)
            : base(mod, defRules, instanceCtx, staticCtx, pos) {
        }

        public override IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                var nwInst = InstanceContext.Replace(genericActualParameter, curr, parent);
                var nwStat = StaticContext.Replace(genericActualParameter, curr, parent);
                // replacing gives a typectx instead of a typetemplatectx (since we have replaced now)
                ret = new SimpleTypeTemplateContext(Module, DefiningRules,
                    nwInst,
                    nwStat
                ) { TypeTemplate = TypeTemplate };
                genericCache.TryAdd((genericActualParameter, curr), ret);
            }
            return (SimpleTypeTemplateContext) ret;
        }
        public ITypeTemplate<IType> TypeTemplate {
            get; set;
        }

        public override IType Type {
            get => base.Type ?? TypeTemplate?.BuildType();
            set => base.Type = value;
        }
        public override string Name {
            get {
                return TypeTemplate != null ? TypeTemplate.Signature.ToString() : base.Name;
            }
        }
        public virtual SimpleTypeTemplateContext NewScope(Context.DefiningRules defRules = Context.DefiningRules.All, Position pos = default, ITypeTemplate<IType> ttp = null) {

            return new SimpleTypeTemplateContext(Module, DefiningRules,
                new HierarchialContext(Module, new[] { InstanceContext }, defRules, false),
                new HierarchialContext(Module, new[] { StaticContext }, defRules, false)) {
                TypeTemplate = ttp
            };
        }
        public static SimpleTypeTemplateContext NewScope(IContext ctx, Context.DefiningRules defRules = Context.DefiningRules.All, Position pos = default, ITypeTemplate<IType> ttp = null) {
            if (ctx is SimpleTypeTemplateContext sttc)
                return sttc.NewScope(defRules, pos, ttp);
            else if (ctx is SimpleTypeContext stc)
                return new SimpleTypeTemplateContext(stc.Module, stc.DefiningRules,
                    new HierarchialContext(stc.Module, new[] { stc.InstanceContext }, defRules, false),
                    new HierarchialContext(stc.Module, new[] { stc.StaticContext }, defRules, false)) {
                    TypeTemplate = ttp
                };
            return new SimpleTypeTemplateContext(ctx.Module, defRules,
                new BasicContext(ctx.Module, defRules, false, pos),
                new HierarchialContext(ctx.Module, new[] { ctx }, defRules, false)) {
                TypeTemplate = ttp
            };
        }
        public static SimpleTypeTemplateContext Merge(Module mod, IEnumerable<IStructTypeContext> ctxs, Context.DefiningRules defRules) {
            return new SimpleTypeTemplateContext(mod, defRules,
                new HierarchialContext(mod, ctxs.Select(x => x.InstanceContext), defRules, false),
                new HierarchialContext(mod, ctxs.Select(x => x.StaticContext), defRules, false)) {
                TypeTemplate = ctxs.OfType<SimpleTypeTemplateContext>().FirstOrDefault()?.TypeTemplate
            };
        }
    }
}

