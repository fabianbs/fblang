using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    public class StackRefCaptureSourceSinkDescription : TaintAnalysis.ITaintSourceDescription<StackCaptureFact>, TaintAnalysis.ITaintSinkDescription<StackCaptureFact> {
        StackRefCaptureDomain dom;
        public StackRefCaptureSourceSinkDescription(StackRefCaptureDomain _dom) {
            dom = _dom ?? throw new ArgumentNullException(nameof(_dom));
        }

        public bool IsSourceMethod(IDeclaredMethod met, out ISet<int> parameterTaintIdx, out bool taintsReturnValue) {
            parameterTaintIdx = Set.Empty<int>();
            taintsReturnValue = false;
            return false;
        }
        public bool IsSourceStatement(IStatement stmt, out ISet<StackCaptureFact> facts) {
            facts = Set.Empty<StackCaptureFact>();
            return false;
        }
        public bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx) {
            parameterLeakIdx = Set.Empty<int>();
            return false;
        }
        public bool IsSinkFact(StackCaptureFact vr) => vr.Variable.DefinedInType != null;// TODO: Assigning to value-typed fields of local variables is ok (will then taint the whole struct)
        public bool IsSinkReturn(StackCaptureFact retFact) => retFact.FirstClass;
        public bool IsSourceExpression(IExpression expr, out ISet<StackCaptureFact> facts) {
            if (expr is ReferenceExpression rx) {
                return dom.ContainsFacts(rx, out facts);
            }
            facts = Set.Empty<StackCaptureFact>();
            return false;
        }
    }
}
