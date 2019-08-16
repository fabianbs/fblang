using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public abstract class ModifierType : TypeImpl, IWrapperType {
        readonly Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IType> genericCache
            = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IType>();
        protected ModifierType(IType underlying, Type.Specifier specs, string customPrefix = "", string customSuffix = "") {
            UnderlyingType = underlying ?? throw new ArgumentNullException(nameof(underlying));
            Signature = new Type.Signature(customPrefix + underlying.Signature.Name + customSuffix, underlying.Signature.GenericActualArguments) { BaseGenericType = underlying.Signature.BaseGenericType };
            TypeSpecifiers = specs;
        }
        public IType UnderlyingType {
            get;
        }

        public override Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;

        public override ITypeContext Context => UnderlyingType.Context;

        public override Type.Specifier TypeSpecifiers {
            get;
        }
        public override IContext DefinedIn => UnderlyingType.DefinedIn;

        public override Visibility Visibility => UnderlyingType.Visibility;

        public override Position Position => UnderlyingType.Position;

        public override IReadOnlyCollection<IType> ImplementingInterfaces => UnderlyingType.ImplementingInterfaces;

        public override IType NestedIn => UnderlyingType.NestedIn;

        IType IWrapperType.ItemType {
            get => UnderlyingType;
        }

        public override bool IsNestedIn(IType other) => UnderlyingType.IsNestedIn(other) || other is ModifierType mt && IsNestedIn(mt.UnderlyingType);
        public override bool IsSubTypeOf(IType other) => UnderlyingType.IsSubTypeOf(other) || other is ModifierType mt && IsSubTypeOf(mt.UnderlyingType);
        public override bool IsSubTypeOf(IType other, out int difference)//TODO capability inheritance
            => UnderlyingType.IsSubTypeOf(other, out difference) || other is ModifierType mt && IsSubTypeOf(mt.UnderlyingType, out difference);
        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                ret = ReplaceImpl(genericActualParameter, curr, parent);
                if (!genericCache.TryAdd(genericActualParameter, ret))
                    ret = genericCache[genericActualParameter];
                if (ret is ModifierType mt) {
                    mt.genericCache.TryAdd(genericActualParameter, ret);
                }
            }
            return ret;
        }
        protected abstract IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
    }
}
