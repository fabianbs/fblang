/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    public static class FBMethodSpecifier {
        public static readonly Method.Specifier InitCtor=(Method.Specifier)((ulong)Method.Specifier.LAST<<1);
        public static readonly Method.Specifier CopyCtor= (Method.Specifier) ((ulong) Method.Specifier.LAST<<2)|Method.Specifier.Constructor;
        public static readonly Method.Specifier AutoIncluded = (Method.Specifier)((ulong)Method.Specifier.LAST<<3);
        public static bool IsInitCtor(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(InitCtor);
        }
        public static bool IsCopyCtor(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(CopyCtor);
        }
        public static bool IsAutoIncluded(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(AutoIncluded);
        }
    }
}
