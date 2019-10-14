using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintSource {
        readonly IVariable srcVar;
        readonly IDeclaredMethod srcMet;
        public TaintSource(IVariable _srcVar) {
            srcVar = _srcVar;
            srcMet = null;
        }
        public TaintSource(IDeclaredMethod _srcMet) {
            srcVar = null;
            srcMet = _srcMet;
        }
        public bool IsSourceMethod => srcMet != null;
        public bool IsSourceVariable => srcVar != null;
        public IVariable SourceVariable => srcVar;
        public IDeclaredMethod SourceMethod => srcMet;

        public bool TryGetSourceVariable(out IVariable vr) {
            if (IsSourceVariable) {
                vr = srcVar;
                return true;
            }
            vr = default;
            return false;
        }
        public bool TryGetSourceMethod(out IDeclaredMethod met) {
            if (IsSourceMethod) {
                met = srcMet;
                return true;
            }
            met = default;
            return false;
        }
    }
}
