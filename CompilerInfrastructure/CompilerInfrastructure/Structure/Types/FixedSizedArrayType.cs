/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class FixedSizedArrayType : ArrayType {
        static readonly LazyDictionary<(ILiteral, IType), FixedSizedArrayType> fsaTp = new LazyDictionary<(ILiteral, IType), FixedSizedArrayType>(delegate ((ILiteral len, IType elem) x) {
            return new FixedSizedArrayType(x.elem, x.len);
        });
        private protected FixedSizedArrayType(IType underlying, uint length) : this(underlying, Literal.UInt(length)) {

        }
        private protected FixedSizedArrayType(IType underlying, UIntLiteral length) : base(underlying, $"[{length}]") {
            ArrayLength = length;
        }
        private protected FixedSizedArrayType(IType underlying, ILiteral length) : base(underlying, $"[{length}]") {
            if (length is null || !length.ReturnType.IsSubTypeOf(PrimitiveType.UInt)) {
                var msg = "The length of a fixed-sized array must be a literal of an unsigned 32bit-integer";
                if (length is null)
                    msg.Report();
                else
                    msg.Report(length.Position);
            }
            ArrayLength = length;
        }
        public ILiteral ArrayLength {
            get;
        }
        public override Type.Specifier TypeSpecifiers => Type.Specifier.FixedSizedArray;
        public static FixedSizedArrayType Get(IType elem, ILiteral len) {
            if (len is UIntLiteral ulit)
                len = Literal.GetOrSetUInt(ulit);
            return fsaTp[(len, elem)];
        }
        public static FixedSizedArrayType Get(IType elem, uint len) {
            return fsaTp[(Literal.UInt(len), elem)];
        }
        public override string ToString() => this.PrintString();
        public override void PrintSuffix(TextWriter tw) => tw.Write("[" + ArrayLength.ToString() + "]$");
    }
}
