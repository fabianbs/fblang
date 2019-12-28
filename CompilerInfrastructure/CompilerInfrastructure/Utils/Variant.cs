using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public abstract class Variant {
        public struct VariantMatch<U> {
            public readonly struct VariantCase {
                readonly Variant vr;
                readonly bool hasValue;
                readonly U value;
                private VariantCase(Variant _vr, bool _hasValue, U _value) {
                    vr = _vr;
                    hasValue = _hasValue;
                    value = _value;
                }
                internal static VariantCase Create<T>(Func<T, U> fn, Variant vr) {
                    bool hasValue;
                    U value;
                    if (vr.TryCast(out T val)) {
                        hasValue = true;
                        value = fn(val);
                    }
                    else {
                        hasValue = false;
                        value = default;
                    }
                    return new VariantCase(vr, hasValue, value);
                }
                public VariantCase Case<V>(Func<V, U> fn) {
                    if (hasValue)
                        return this;
                    else
                        return Create(fn, vr);
                }
                public U Else(U dflt = default) {
                    if (hasValue)
                        return value;
                    return dflt;
                }
            }

            readonly Variant vr;
            internal VariantMatch(Variant _vr) {
                vr = _vr;
            }
            public VariantCase Case<T>(Func<T, U> fn) {
                return VariantCase.Create(fn ?? throw new ArgumentNullException(nameof(fn)), vr);
            }

        }
        public abstract bool TryCast<T>(out T value);
        public VariantMatch<U> Match<U>() {
            return new VariantMatch<U>(this);
        }
    }
}
