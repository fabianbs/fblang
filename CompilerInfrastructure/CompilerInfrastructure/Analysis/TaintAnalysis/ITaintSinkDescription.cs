using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintSinkDescription<D> {
        bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx);
        
        bool IsSinkFact(D vr);
        bool IsSinkReturn(D retFact);
    }
}
