using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis {
    public interface IDataFlowDomain<D> {
        bool IsInDomain(D fact);
        bool IsPropagatingAssignment(BinOp ass) { return true; }
    }
}
