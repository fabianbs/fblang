using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class FunctionalComparer<T> : IComparer<T> {
        readonly Comparison<T> compareTo;

        public FunctionalComparer(Comparison<T> cmp) {
            compareTo = cmp ?? throw new ArgumentNullException(nameof(cmp));
        }

        public int Compare(T x, T y) => compareTo(x, y);
        
        public static implicit operator FunctionalComparer<T>(Comparison<T> cmp) {
            return new FunctionalComparer<T>(cmp);
        }
    }
    public class FunctionalEquiComparer<T> : IEqualityComparer<T> {
        readonly Func<T, T, bool> eq;
        readonly Func<T, int> hc;
        public FunctionalEquiComparer(Func<T, T, bool> _eq, Func<T, int> _hc) {
            eq = _eq ?? throw new ArgumentNullException(nameof(_eq));
            hc = _hc ?? throw new ArgumentNullException(nameof(_hc));
        }

        public bool Equals(T x, T y) => eq(x, y);
        public int GetHashCode(T obj) => hc(obj);
    }
}
