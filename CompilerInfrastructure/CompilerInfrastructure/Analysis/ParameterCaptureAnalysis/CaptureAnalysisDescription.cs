using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    public class CaptureAnalysisDescription : TaintAnalysis.TaintAnalysisDescription<IVariable> {
        public CaptureAnalysisDescription(InterMonoAnalysis<TaintAnalysisSummary<IVariable>,IVariable> analysis)
            : base(CaptureSourceSinkDescription.Instance, CaptureSourceSinkDescription.Instance, new CaptureDomain(analysis)) {
        }

        public override ISet<IVariable> SummaryFlow(ISet<IVariable> In, IDeclaredMethod met) {
            var ret = MonoSet.Empty<IVariable>();
            if (!met.IsStatic() && !met.HasUniqueThis() && met.NestedIn is ITypeContext tcx && tcx.Type != null) {
                ret += Variable.This(tcx.Type);
            }
            ret.AddRange(met.Arguments.Where(x => !x.IsNoCapture()));
            return ret;
        }
        public override ISet<IVariable> InitialSeeds(IDeclaredMethod met) => MonoSet.Of(met.Arguments);
    }
}
