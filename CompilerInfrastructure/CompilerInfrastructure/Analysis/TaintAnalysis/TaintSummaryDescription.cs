using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    /// <summary>
    /// Provides a default- or specialized summary for functions, which have no body, or are imported
    /// </summary>
    public interface TaintSummaryDescription {
        TaintAnalysisSummary GetSummary(IDeclaredMethod met);
    }
}
