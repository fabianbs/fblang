using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompilerInfrastructure {
    [Serializable]
    public class ByRefType : ModifierType {
        [Serializable]
        class ByRefSignature : Type.Signature {
            readonly Type.Signature underlying;
            public ByRefSignature(Type.Signature underlying) : base(underlying?.Name ?? "", underlying?.GenericActualArguments) {
                this.underlying = underlying ?? throw new System.ArgumentNullException(nameof(underlying));
            }
            public override string Name => base.Name + "&";
        }
        static readonly LazyDictionary<IType, ByRefType> byrefTps = new LazyDictionary<IType, ByRefType>(x => new ByRefType(x));

        public ByRefType(IType underlying)
            : base(underlying ?? "Unknown ByRef-Type".ReportTypeError(),
                  (underlying?.TypeSpecifiers ?? Type.Specifier.None) | Type.Specifier.ByRef,
                  customSuffix: "&") {
        }


        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var nwElem = UnderlyingType.Replace(genericActualParameter, curr, parent);
            return byrefTps[nwElem];
        }
        public static ByRefType Get(IType it) {
            return byrefTps[it];
        }
        public override string ToString() => this.PrintString();
        public override void PrintPrefix(TextWriter tw) {
            UnderlyingType.PrintTo(tw);
        }
        public override void PrintValue(TextWriter tw) { }
        public override void PrintSuffix(TextWriter tw) {
            tw.Write("&");
        }
    }
}