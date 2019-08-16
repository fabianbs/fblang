using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class VarArgType : SpanType {
        static readonly LazyDictionary<IType, VarArgType> vaTp = new LazyDictionary<IType, VarArgType>(x => new VarArgType(x));
        private protected VarArgType(IType underlying) : base(underlying, "...") {
        }
        public override Type.Specifier TypeSpecifiers => Type.Specifier.VarArg;

        public static new VarArgType Get(IType argsTp) {
            return argsTp is VarArgType vaty ? vaty : vaTp[argsTp];
        }
        public override bool IsSubTypeOf(IType other) => IsSubTypeOf(other, out _);
        public override bool IsSubTypeOf(IType other, out int difference) => ItemType.IsSubTypeOf(other, out difference);
        public override string ToString() => this.PrintString();
        public override void PrintSuffix(TextWriter tw) =>tw.Write("...");
    }
}
