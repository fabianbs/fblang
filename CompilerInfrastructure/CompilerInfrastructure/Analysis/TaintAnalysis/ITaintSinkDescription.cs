using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintSinkDescription {
        bool IsSinkMethod(IDeclaredMethod met, out ISet<int> parameterLeakIdx);
        
        bool IsSinkVariable(IVariable vr);
    }
}
