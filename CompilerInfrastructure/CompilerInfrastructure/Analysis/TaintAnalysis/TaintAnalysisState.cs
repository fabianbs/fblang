using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    class TaintAnalysisState<D> {
        internal readonly MultiMap<D, TaintSource<D>> factToSrc = new MultiMap<D, TaintSource<D>>();
        internal readonly MultiMap<ISourceElement, D> leaks = new MultiMap<ISourceElement, D>();
        internal readonly ISet<D> returnFacts = new HashSet<D>();
    }
}
