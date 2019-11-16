using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    public class CaptureSourceSinkDescription : ITaintSourceDescription, ITaintSinkDescription {
        public static CaptureSourceSinkDescription Instance { get; } = new CaptureSourceSinkDescription();
        private CaptureSourceSinkDescription() { }

        public bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx) {
            parameterLeakIdx = Set.Empty<int>();
            return false;
        }
        public bool IsSinkVariable(IVariable vr) {
            return !vr.IsLocalVariable() || vr.Type.IsByMutableRef();
        }
        public bool IsSourceMethod(IDeclaredMethod met, out ISet<int> parameterTaintIdx, out bool taintsReturnValue) {
            taintsReturnValue = false;
            parameterTaintIdx = Set.Empty<int>();
            return false;
        }
        public bool IsSourceStatement(IStatement stmt, out ISet<IVariable> facts) {
            if (stmt is Declaration decl) {
                facts = Imms.ImmSet.Of(decl.Variables);
                return true;
            }
            facts = Set.Empty<IVariable>();
            return false;
        }
    }
}
