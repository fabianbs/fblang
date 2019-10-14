﻿using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintSourceDescription {
        bool IsSourceMethod(IDeclaredMethod met, out ISet<int> parameterTaintIdx, out bool taintsReturnValue);
        bool IsSourceVariable(IVariable vr);
    }
}
