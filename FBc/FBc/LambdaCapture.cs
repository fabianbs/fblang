using CompilerInfrastructure;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    public class LambdaCapture : BasicVariable {
        [Serializable]
        enum SpecialCaptures {
            None, This, Base
        }
        static readonly LazyDictionary<IType, LambdaCapture> thisCapture
            = new LazyDictionary<IType, LambdaCapture>(x => new LambdaCapture(new BasicVariable(x.Position, x, Variable.Specifier.None, "this", x), SpecialCaptures.This));
        static readonly LazyDictionary<IType, LambdaCapture> baseCapture
            = new LazyDictionary<IType, LambdaCapture>(x => x is IHierarchialType hty
                                                            ? new LambdaCapture(new BasicVariable(x.Position, hty.SuperType, Variable.Specifier.None, "super", x), SpecialCaptures.Base)
                                                            : null);
        readonly SpecialCaptures specialCap;

        public LambdaCapture(IVariable original)
            : base(original.Position,
                  original.Type,
                  original.VariableSpecifiers,
                  original.Signature.Name,
                  original.DefinedInType,
                  original.Visibility) {
            Original = original;
        }
        private LambdaCapture(IVariable original, SpecialCaptures scap) : this(original) {
            specialCap = scap;
        }

        public IVariable Original {
            get;
        }
        protected override IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var ret = this;
            var otherType = Type.Replace(genericActualParameter, curr, parent);
            if (Type != otherType) {
                ret = new LambdaCapture(Original.Replace(genericActualParameter, curr, parent));
            }
            return ret;
        }

        public bool IsThisCapture => specialCap == SpecialCaptures.This;
        public bool IsBaseCapture => specialCap == SpecialCaptures.Base;

        public static LambdaCapture This(IType ty) {
            return thisCapture[ty];
        }
        public static LambdaCapture Base(IType ty) {
            return baseCapture[ty];
        }

    }
}
