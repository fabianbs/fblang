using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Contexts;

namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    public class StackRefCaptureAnalysisDescription : TaintAnalysis.TaintAnalysisDescription<StackCaptureFact> {

        private StackRefCaptureAnalysisDescription(StackRefCaptureDomain dom, StackRefCaptureSourceSinkDescription sourceSink)
            : base(sourceSink, sourceSink, dom) {
        }
        private StackRefCaptureAnalysisDescription(StackRefCaptureDomain dom)
            : this(dom, new StackRefCaptureSourceSinkDescription(dom)) {
        }
        public StackRefCaptureAnalysisDescription(InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> analysis)
            : this(new StackRefCaptureDomain(analysis)) {
        }

        public override ISet<StackCaptureFact> SummaryFlow(ISet<StackCaptureFact> In, IDeclaredMethod met) {
            var ret = MonoSet.Empty<StackCaptureFact>();
            ret.AddRange(met.Arguments.Where(x => x.Type.IsRef() && (!x.IsFinal() || !x.Type.IsUnique())).Select(Domain.FactFromVariable));
            return ret;
        }
    }
}
