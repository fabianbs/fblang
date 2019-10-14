using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis {
    /// <summary>
    /// Defines acommon interface for a interprocedural dataflow-analysis
    /// </summary>
    /// <typeparam name="S">The type of procedure-summaries</typeparam>
    /// <typeparam name="D">The type of dataflow-facts</typeparam>
    /// <typeparam name="N">The type of Nodes under analysis</typeparam>
    public interface IDataFlowAnalysis<S, D, N> {
        S Query(N node);
    }
}
