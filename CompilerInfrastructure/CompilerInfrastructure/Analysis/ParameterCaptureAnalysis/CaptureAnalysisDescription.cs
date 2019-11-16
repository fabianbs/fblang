using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    public class CaptureAnalysisDescription : TaintAnalysis.TaintAnalysisDescription<CaptureDomain> {
        public CaptureAnalysisDescription()
            : base(CaptureSourceSinkDescription.Instance, CaptureSourceSinkDescription.Instance) {
        }

        public override ISet<IVariable> SummaryFlow(ISet<IVariable> In, IDeclaredMethod met) {
            var ret = Imms.ImmSet.Empty<IVariable>();
            if (!met.IsStatic() && !met.HasUniqueThis() && met.NestedIn is ITypeContext tcx && tcx.Type != null) {
                ret += Variable.This(tcx.Type);
            }
            ret.AddRange(met.Arguments.Where(x => !x.IsFinal() || !x.Type.IsUnique()));
            return ret;
        }
        public override ISet<IVariable> InitialSeeds(IDeclaredMethod met) => Imms.ImmSet.Of(met.Arguments);
    }
}
