/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface INameMangler {
        string MangleTypeName(IType ty);
        string MangleFunctionName(IMethod met);
    }
    public interface INameDemangler {
        Type.Signature DemangleTypeName(string mname);
        Method.Signature DemangleFunctionName(string mname);
    }
    /// <summary>
    /// This mangler just returns the FullName() result
    /// </summary>
    public class NullMangler : INameMangler {

        public string MangleFunctionName(IMethod met) => met.FullName();
        public string MangleTypeName(IType ty) => ty.FullName();

        private NullMangler() { }
        public static NullMangler Instance { get; } = new NullMangler();
    }
    /// <summary>
    /// This name-mangling scheme is oriented at the Itanium C++ ABI, but does not implement it exactly.
    /// One reason is, that for function-resolution also the return-type may be relevant.
    /// Also the mangled names should be a bit more readable
    /// </summary>
    public class DefaultNameMangler : INameMangler {
        Dictionary<IMethod, string> cache = new Dictionary<IMethod, string>();
        public string MangleFunctionName(IMethod met) {
            if (cache.TryGetValue(met, out var ret))
                return ret;
            var sig = met.Signature;
            // do not mangle the main-function
            if (sig.Name == "main")
                return "main";
            // do not mangle already mangled names
            if (sig.TryGetInternalName(out var intnl))
                return intnl;
            var sb = new StringBuilder();
            sb.Append("_Z");
            var fullname = met.FullName();
            sb.Append(fullname.Length);
            sb.Append(fullname);
            if (sig.GenericActualArguments.Any()) {
                sb.Append("<");

                sb.AppendJoin(",", sig.GenericActualArguments.Select(x => {
                    if (x is ILiteral lit)
                        return lit.ToString();
                    else if (x is IType ty)
                        return MangleTypeName(ty);
                    else
                        //will not happen
                        return null;
                }));

                sb.Append(">");
            }
            sb.Append(":");
            if (met.Arguments.Any()) {
                sb.AppendJoin(",", sig.ArgTypeSignatures.Select(x => MangleTypeName(x)));
            }
            else {
                sb.Append("v");
            }
            sb.Append("->");
            sb.Append(MangleTypeName(met.ReturnType));

            ret = sb.ToString();
            cache.TryAdd(met, ret);
            return ret;
        }

        public string MangleTypeName(IType ty) {
            if (ty.TryCast<PrimitiveType>(out var prim)) {
                switch (prim.PrimitiveName) {
                    case PrimitiveName.Void:
                        return "v";
                    case PrimitiveName.Null:
                        return "Dn";
                    case PrimitiveName.Bool:
                        return "b";
                    case PrimitiveName.Byte:
                        return "h";
                    case PrimitiveName.Char:
                        return "c";
                    case PrimitiveName.Short:
                        return "s";
                    case PrimitiveName.UShort:
                        return "t";
                    case PrimitiveName.Int:
                        return "i";
                    case PrimitiveName.UInt:
                        return "j";
                    case PrimitiveName.Handle:
                        return "P";
                    case PrimitiveName.SizeT:
                        return "z";
                    case PrimitiveName.Long:
                        return "x";
                    case PrimitiveName.ULong:
                        return "y";
                    case PrimitiveName.BigLong:
                        return "n";
                    case PrimitiveName.UBigLong:
                        return "o";
                    case PrimitiveName.Float:
                        return "f";
                    case PrimitiveName.Double:
                        return "d";
                    case PrimitiveName.String:
                        return "s";
                }
            }

            return ty.UnWrapAll().FullName() + (ty.IsByRef() ? "&" : "");
        }
    }
}
