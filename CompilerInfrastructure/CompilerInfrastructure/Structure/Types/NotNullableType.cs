/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class NotNullableType : ModifierType {
        static readonly LazyDictionary<IType, IType> cache = new LazyDictionary<IType, IType>(x => x.IsNotNullable() ? x : new NotNullableType(x));
        public NotNullableType(IType underlying) : base(underlying, underlying.TypeSpecifiers | Type.Specifier.NotNullable, "", "!") {
        }
        public override void PrintPrefix(TextWriter tw) {
            UnderlyingType.PrintTo(tw);
        }
        public override void PrintValue(TextWriter tw) {

        }
        public override void PrintSuffix(TextWriter tw) {
            tw.Write("!");
        }
        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(UnderlyingType.Replace(genericActualParameter, curr, parent));
        }
        public static IType Get(IType tp) {
            return cache[tp];
        }
    }
}
