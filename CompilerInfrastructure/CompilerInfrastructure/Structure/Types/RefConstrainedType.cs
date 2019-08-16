using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public enum ReferenceCapability {
        @ref,
        trn,
        iso,
        box,
        val,
        tag
    }
    public static class ReferenceCapabilityHelper {
        public static Type.Specifier AsTypeSpecifier(this ReferenceCapability cap) {
            switch (cap) {
                case ReferenceCapability.trn:
                    return Type.Specifier.Transition;
                case ReferenceCapability.iso:
                    return Type.Specifier.Unique;
                case ReferenceCapability.box:
                    return Type.Specifier.Constant;
                case ReferenceCapability.val:
                    return Type.Specifier.Immutable;
                case ReferenceCapability.tag:
                    return Type.Specifier.Tag;
                default:
                    return Type.Specifier.None;
            }
        }
        public static bool CanWrite(this ReferenceCapability cap) {
            switch (cap) {
                case ReferenceCapability.@ref:
                case ReferenceCapability.trn:
                case ReferenceCapability.iso:
                    return true;
                default:
                    return false;
            }
        }
        public static bool CanRead(this ReferenceCapability cap) {
            return cap != ReferenceCapability.tag;
        }
    }
    [Serializable]
    public class RefConstrainedType : ModifierType {
        static readonly LazyDictionary<(IType, ReferenceCapability), RefConstrainedType> cache
            = new LazyDictionary<(IType, ReferenceCapability), RefConstrainedType>(x => new RefConstrainedType(x.Item1, x.Item2));
        readonly ReferenceCapability refCap;

        static IType getUnderlying(IType elem) {
            if (elem is RefConstrainedType rct) {
                return rct.UnderlyingType;
            }
            else {
                return elem ?? "The type which is constrained by a reference capability must not be null".ReportTypeError();
            }
        }
        public RefConstrainedType(IType elem, ReferenceCapability refCap)
            : base(getUnderlying(elem), elem.TypeSpecifiers | refCap.AsTypeSpecifier(), refCap + " ") {
            this.refCap = refCap;

        }
        public ReferenceCapability ReferenceCapability => refCap;

        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(UnderlyingType.Replace(genericActualParameter, curr, parent), refCap);
        }
        public static RefConstrainedType Get(IType tp, ReferenceCapability refCap) {
            return cache[(tp, refCap)];
        }

        public override void PrintPrefix(TextWriter tw) {
            UnderlyingType.PrintPrefix(tw);
        }
        public override void PrintValue(TextWriter tw) {
            tw.Write(refCap.AsTypeSpecifier().ToString().ToLower());
            UnderlyingType.PrintValue(tw);
        }
        public override void PrintSuffix(TextWriter tw) {
            UnderlyingType.PrintSuffix(tw);
        }
        public override bool IsSubTypeOf(IType other) => IsSubTypeOf(other, out _);
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (base.IsSubTypeOf(other, out difference))
                return true;
            // for immutable types, the type-parameters are covariant
            if (refCap == ReferenceCapability.val) {
                //immutable arrays are covariant
                {
                    if (UnderlyingType.TryCast<ArrayType>(out var arr) && other.IsImmutable() && other.TryCast<ArrayType>(out var that) && !that.ItemType.IsPrimitive()) {
                        return arr.ItemType.IsSubTypeOf(that.ItemType, out difference);
                    }
                }
                // immutable slices are covariant
                {
                    if (UnderlyingType.TryCast<SpanType>(out var arr) && other.IsImmutable() && other.TryCast<SpanType>(out var that) && !that.ItemType.IsPrimitive()) {
                        return arr.ItemType.IsSubTypeOf(that.ItemType, out difference);
                    }
                }
                // immutable type-template-instantiations are covariant
                {
                    if (UnderlyingType.TryCast<ClassType>(out var ctp) && other.IsImmutable() && other.TryCast<ClassType>(out var that)) {
                        if (ctp.Signature.BaseGenericType == that.Signature.BaseGenericType) {
                            for (int i = 0; i < ctp.Signature.GenericActualArguments.Count; ++i) {
                                if (ctp.Signature.GenericActualArguments[i] is ILiteral genLit) {
                                    // literal-parameters are invariant
                                    if (that.Signature.GenericActualArguments[i] is ILiteral thatLit) {
                                        if (!thatLit.ValueEquals(genLit))
                                            return false;
                                    }
                                    else
                                        return false;
                                }
                                else if (ctp.Signature.GenericActualArguments[i] is IType genTp) {
                                    // type-parameters are now covariant
                                    if (that.Signature.GenericActualArguments[i] is IType thatTp) {
                                        if (genTp.IsSubTypeOf(thatTp, out var diff))
                                            difference += diff * diff;
                                        else
                                            return false;
                                    }
                                    else
                                        return false;
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
