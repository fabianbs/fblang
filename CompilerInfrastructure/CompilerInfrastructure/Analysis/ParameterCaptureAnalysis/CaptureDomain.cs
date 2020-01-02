using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    using Structure;

    public class CaptureDomain : TaintAnalysisVariableDomain {
        public CaptureDomain(InterMonoAnalysis<TaintAnalysisSummary<IVariable>, IVariable> analysis) 
            : base(analysis) {
        }

        public override bool IsInDomain(IVariable fact) {
            return fact != null;
        }
    }
}
