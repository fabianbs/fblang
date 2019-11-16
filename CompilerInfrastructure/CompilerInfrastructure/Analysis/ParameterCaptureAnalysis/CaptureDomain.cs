using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    public class CaptureDomain : IDataFlowDomain<IVariable> {
        public bool IsInDomain(IVariable fact) {
            return fact != null;
        }
    }
}
