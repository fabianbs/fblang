using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.ParameterCaptureAnalysis {
    public class CaptureSourceSinkDescription : ITaintSourceDescription<IVariable>, ITaintSinkDescription<IVariable> {
        public static CaptureSourceSinkDescription Instance { get; } = new CaptureSourceSinkDescription();
        private CaptureSourceSinkDescription() { }

        public bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx) {
            parameterLeakIdx = Set.Empty<int>();
            return false;
        }
        public bool IsSinkFact(IVariable vr) {
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

        public bool IsSinkReturn(IVariable retFact) => false;
        public bool IsSourceExpression(IExpression expr, out ISet<IVariable> facts) {
            facts = Set.Empty<IVariable>();
            return false;
        }
    }
}
