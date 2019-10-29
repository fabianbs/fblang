using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using Imms;

namespace CompilerInfrastructure.Analysis.Lattice {
   
    public class DefaultSubsetLattice<D> : ILattice<ISet<D>> {
        public DefaultSubsetLattice(int bound) {
            Bound = bound >= 0 ? bound : throw new ArgumentOutOfRangeException(nameof(bound));
        }
        public DefaultSubsetLattice() {
            Bound = int.MaxValue;
        }
        public int Bound { get; }
        public ISet<D> Bottom { get; } = ImmSet.Empty<D>();

        public bool IsTop(ISet<D> d) => !(d is null) && d.Count >= Bound;
        public ISet<D> Join(ISet<D> d1, ISet<D> d2) {
            if (d1 is null)
                return d2;
            if (d2 is null)
                return d1;
            return d1.ToImmSet().Union(d2);
        }

        public bool SqSubsetEqual(ISet<D> d1, ISet<D> d2) {
            if (d1 is null || d1.Count == 0)
                return true;
            if (d2 is null || d2.Count == 0)
                return false;
            return d1.IsSubsetOf(d2);
        }
        public static ILattice<ISet<D>> Instance { get; } = new DefaultSubsetLattice<D>();
    }
}
