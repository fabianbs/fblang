namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    using System;
    using System.Collections.Generic;
    using Expressions;
    using Instructions;
    using TaintAnalysis;
    using Utils;

    public class StackRefCaptureSourceSinkDescription : ITaintSourceDescription<StackCaptureFact>,
                                                        ITaintSinkDescription<StackCaptureFact> {
        readonly StackRefCaptureDomain dom;

        public StackRefCaptureSourceSinkDescription(StackRefCaptureDomain _dom) {
            dom = _dom ?? throw new ArgumentNullException(nameof(_dom));
        }

        public bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx) {
            parameterLeakIdx = Set.Empty<int>();
            return false;
        }

        public bool IsSinkFact(StackCaptureFact vr) {
            return vr.Variable.DefinedInType != null;
        }

        public bool IsSinkReturn(StackCaptureFact retFact) {
            return retFact.FirstClass;
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

        public bool IsSourceExpression(IExpression expr, out ISet<StackCaptureFact> facts) {
            if (expr is ReferenceExpression rx)
                return dom.ContainsFacts(rx, out facts);
            facts = Set.Empty<StackCaptureFact>();
            return false;
        }
    }
}
