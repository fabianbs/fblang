using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis {
    public interface IAnalysisDomain<D> {
        bool IsInDomain(D fact);
    }
}
