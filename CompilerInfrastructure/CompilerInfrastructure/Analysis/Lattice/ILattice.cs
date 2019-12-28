using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.Lattice {
    public interface ILattice<D> {
        D Bottom { get; }
        bool IsTop(D d);
        bool SqSubsetEqual(D d1, D d2);
        D Join(D d1, D d2);
    }
}
