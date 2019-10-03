using System;
using System.Collections.Generic;
using System.Text;

namespace FBc.Analysis {
    /// <summary>
    /// Defines a dataflow-analysis
    /// </summary>
    /// <typeparam name="N">The type of nodes under analysis</typeparam>
    /// <typeparam name="D">The type of the dataflow-facts</typeparam>
    public interface IDataFlowAnalysis<N, D> {
        /// <summary>
        /// Queries the analysis-results for the given node
        /// </summary>
        D Query(N node, D initialSeeds);
    }
}
