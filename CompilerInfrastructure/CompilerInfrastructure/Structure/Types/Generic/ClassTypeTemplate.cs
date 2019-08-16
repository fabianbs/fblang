using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types.Generic {
    [Serializable]
    public class ClassTypeTemplate : ITypeTemplate<ClassType>, IHierarchialTypeTemplate<ClassType> {
        readonly Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), ClassTypeTemplate> genericCache
            = new Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), ClassTypeTemplate>();
        readonly Dictionary<EquatableCollection<ITypeOrLiteral>, ClassType> builtTypes = new Dictionary<EquatableCollection<ITypeOrLiteral>, ClassType>();
        public ClassTypeTemplate(Position pos, IContext parentCtx, string name, IReadOnlyList<IGenericParameter> genArgs) {
            Position = pos;
            Parent = parentCtx;
            Signature = new TypeTemplate.Signature(name, genArgs);
            HeaderContext = //parentCtx is SimpleTypeContext stc
                            //? stc.NewScope(Visibility.Private)
                            // : new SimpleTypeContext(parentCtx.Module, CompilerInfrastructure.Context.DefiningRules.All, new BasicContext(parentCtx.Module, CompilerInfrastructure.Context.DefiningRules.All), parentCtx);
               SimpleTypeTemplateContext.NewScope(parentCtx, pos: pos, defRules: CompilerInfrastructure.Context.DefiningRules.Types);
            HeaderContext.TypeTemplate = this;
            foreach (var genTp in genArgs.OfType<GenericTypeParameter>()) {
                HeaderContext.DefineType(genTp);
            }

            Context = SimpleTypeTemplateContext.NewScope(HeaderContext, pos: pos, defRules: CompilerInfrastructure.Context.DefiningRules.All);
            Context.TypeTemplate = this;

        }
        public ClassTypeTemplate(Position pos, IContext parentCtx, string name, params IGenericParameter[] genArgs)
            : this(pos, parentCtx, name, (IReadOnlyList<IGenericParameter>) genArgs) {

        }
        public TypeTemplate.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public Position Position {
            get;
        }
        public IContext Parent {
            get;
        }
        Type.Specifier typeSpecifiers=Type.Specifier.None;
        public Type.Specifier TypeSpecifiers {
            get => typeSpecifiers; set {
                if (typeSpecifiers != value) {
                    typeSpecifiers = value;
                    foreach (var kvp in builtTypes) {
                        kvp.Value.TypeSpecifiers = value;
                    }
                }
            }
        }
        public ITypeTemplateContext HeaderContext {
            get;
        }
        ITypeTemplateContext context;
        public ITypeTemplateContext Context {
            get => context; set {
                if (context != value) {
                    context = value;
                    foreach (var kvp in builtTypes) {
                        var gen = FromList(Signature.GenericFormalparameter, kvp.Key);
                        kvp.Value.Context = (ITypeContext) value?.Replace(gen, value, value);
                        // the replacing does not change the type, since it was already built using this generic args
                        kvp.Value.Context.Type = kvp.Value;
                    }
                }
            }
        }
        ClassType superType=null;
        public ClassType SuperType {
            get => superType; set {
                if (value != superType) {
                    superType = value;
                    foreach (var kvp in builtTypes) {
                        var gen = FromList(Signature.GenericFormalparameter, kvp.Key);
                        kvp.Value.Superclass = (ClassType) ReplaceWithParent(value, gen, Context);
                    }
                }
            }
        }
        IHierarchialType IHierarchialTypeTemplate<ClassType>.SuperType => SuperType;
        public IList<IType> ImplementingInterfaces {
            get;
        } = new List<IType>();
        public Visibility Visibility {
            get; set;
        } = Visibility.Internal;


        public bool AddInterface(IType intf) {
            if (intf.IsInterface()) {
                ImplementingInterfaces.Add(intf);
                return true;
            }
            else
                return false;
        }
        internal ClassType BuildTypeImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> dic, IReadOnlyList<ITypeOrLiteral> genericActualParameters) {

            var ret = new ClassType(Position, Signature.Name, Visibility, genericActualParameters, Parent) {
                TypeSpecifiers = TypeSpecifiers,
                Superclass = ReplaceWithParent(SuperType, dic, Context) as ClassType,
                Parent = Parent.Replace(dic, Context, Context)
            };
            ret.Signature.BaseGenericType = this;
            foreach (var x in ImplementingInterfaces) {
                ret.AddInterface(x.Replace(dic, Context, Context));
            }
            ret.Context = (ITypeContext) Context?.Replace(dic, Context, Context);
            ret.Context.Type = ret;
            ret.genericCache.TryAdd(dic, ret);
            return ret;
            // leads t ostack-overflow exception, since ClassType.Replace calls BuildType
            //return (ClassType)BuildType().Replace(dic, Context, Context);
        }
        public ClassType BuildType(GenericParameterMap<IGenericParameter, ITypeOrLiteral> dic, IReadOnlyList<ITypeOrLiteral> genericActualParameters) {
            var genArgs = EquatableCollection.FromIList(genericActualParameters);
            if (builtTypes.TryGetValue(genArgs, out var ret)) {
                return ret;
            }
            ret = BuildTypeImpl(dic, genericActualParameters);
            if (!builtTypes.TryAdd(genArgs, ret)) {
                return builtTypes[genArgs];
            }
            return ret;
        }
        public ClassType BuildType(IReadOnlyList<ITypeOrLiteral> genericActualParameters) {
            var genArgs = EquatableCollection.FromIList(genericActualParameters);
            if (builtTypes.TryGetValue(genArgs, out var ret)) {
                return ret;
            }
            var dic = FromList(Signature.GenericFormalparameter, genericActualParameters);
            ret = BuildTypeImpl(dic, genericActualParameters);
            if (!builtTypes.TryAdd(genArgs, ret)) {
                return builtTypes[genArgs];
            }
            return ret;
        }
        internal static GenericParameterMap<IGenericParameter, ITypeOrLiteral> FromList(IReadOnlyList<IGenericParameter> genericFormalArguments, IReadOnlyList<ITypeOrLiteral> genericActualArguments) {
            var dic = new GenericParameterMap<IGenericParameter, ITypeOrLiteral>();
            int count = Math.Min(genericFormalArguments.Count, genericActualArguments?.Count ?? 0);
            for (int i = 0; i < count; ++i) {
                dic.Add(genericFormalArguments[i], genericActualArguments[i]);
            }
            return dic;
        }
        IType ReplaceWithParent(IType tp, GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr) {
            IContext par;
            if (tp is ClassType ct && ct.Parent != null) {
                if (ct.Parent is SimpleTypeContext stc && stc.Type != null) {
                    par = ReplaceWithParent(stc.Type, genericActualParameter, curr).Context;
                }
                else {
                    //DOLATER is null right here?
                    par = ct.Parent.Replace(genericActualParameter, curr, null);
                }
            }
            else
                par = null;
            return tp?.Replace(genericActualParameter, curr, par);
        }
        public ClassType BuildType() {
            var genArgs = EquatableCollection.FromIList<ITypeOrLiteral>(Signature.GenericFormalparameter);
            if (builtTypes.TryGetValue(genArgs, out var ret)) {
                return ret;
            }
            ret = new ClassType(Position, Signature.Name, Visibility, Signature.GenericFormalparameter, Parent) {
                TypeSpecifiers = TypeSpecifiers,
                Superclass = SuperType,
                Parent = Parent
            };
            ret.Signature.BaseGenericType = this;
            foreach (var x in ImplementingInterfaces) {
                ret.AddInterface(x);
            }
            var dic = new Dictionary<IGenericParameter, ITypeOrLiteral>();
            foreach (var gen in Signature.GenericFormalparameter) {
                dic.Add(gen, gen);
            }
            ret.Context = (ITypeContext) Context?.Replace(dic, Context, Context);
            ret.Context.Type = ret;
            ret.genericCache.TryAdd(dic, ret);
            if (!builtTypes.TryAdd(genArgs, ret)) {
                return builtTypes[genArgs];
            }
            return ret;
        }
        public IRefEnumerator<IASTNode> GetEnumerator() => throw new NotImplementedException();
        public ITypeTemplate<ClassType> Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                ret = new ClassTypeTemplate(Position, parent, Signature.Name, Signature.GenericFormalparameter);

                genericCache.TryAdd((genericActualParameter, curr), ret);
                ret.genericCache.TryAdd((genericActualParameter, curr), ret);
                foreach (var intf in ImplementingInterfaces)
                    ret.AddInterface(intf.Replace(genericActualParameter, curr, parent));
                ret.TypeSpecifiers = TypeSpecifiers;
                ret.Context = (ITypeTemplateContext) Context.Replace(genericActualParameter, curr, parent);
                ret.Context.TypeTemplate = ret;
                ret.SuperType = SuperType?.Replace(genericActualParameter, curr, parent) as ClassType;
                ret.Visibility = Visibility;

                (ret.ImplementingInterfaces as List<IType>).AddRange(ImplementingInterfaces.Select(x => x.Replace(genericActualParameter, curr, parent)));

            }
            return ret;
        }
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/

        public override string ToString() => Signature.ToString();
    }
}
