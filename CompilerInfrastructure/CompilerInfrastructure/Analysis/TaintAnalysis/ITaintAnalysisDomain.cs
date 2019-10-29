using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintAnalysisDomain : IDataFlowDomain<IVariable> {
    }
}
