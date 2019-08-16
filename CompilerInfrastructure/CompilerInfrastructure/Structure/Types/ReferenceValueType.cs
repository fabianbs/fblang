using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class ReferenceValueType : ModifierType {
        readonly bool isValueType;
        public ReferenceValueType(IType underlying, bool isValueTy)
            : base(underlying, isValueTy ? underlying.TypeSpecifiers | Type.Specifier.ValueType : underlying.TypeSpecifiers & ~Type.Specifier.ValueType, customSuffix: isValueTy ? "$" : "&") {
            isValueType = isValueTy;
        }


        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(UnderlyingType.Replace(genericActualParameter, curr, parent), isValueType);
        }

        static readonly LazyDictionary<(IType, bool), IType> cache
            = new LazyDictionary<(IType, bool), IType>(x => {
                if (x.Item2) {
                    if (x.Item1.IsValueType())
                        return x.Item1;
                    if (x.Item1 is ReferenceValueType rvt && rvt.UnderlyingType.IsValueType())
                        return rvt.UnderlyingType;
                    return new ReferenceValueType(x.Item1, true);
                }
                else {
                    if (!x.Item1.IsValueType())
                        return x.Item1;
                    if (x.Item1 is ReferenceValueType rvt && !rvt.UnderlyingType.IsValueType())
                        return rvt.UnderlyingType;
                    return new ReferenceValueType(x.Item1, false);
                }
            });
        public static IType Get(IType underlying, bool isValueType) {
            return cache[(underlying, isValueType)];
        }

        public override void PrintPrefix(TextWriter tw) {
            if (isValueType)
                UnderlyingType.PrintTo(tw);
            else
                UnderlyingType.PrintPrefix(tw);
        }
        public override void PrintValue(TextWriter tw) {
            if (!isValueType) {
                tw.Write("ref ");
                UnderlyingType.PrintValue(tw);
            }
        }
        public override void PrintSuffix(TextWriter tw) {
            if (isValueType)
                tw.Write("$");
            else
                UnderlyingType.PrintSuffix(tw);
        }
    }
}
