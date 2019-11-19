using System;
using System.Collections.Generic;
using System.Linq;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintAnalysisSummary<D> {
        private readonly TaintAnalysisState<D> finalState;

        internal TaintAnalysisSummary(ISet<D> _mfp, TaintAnalysisState<D> finalState) {
            MaximalFixpoint = _mfp;
            this.finalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        }
        internal TaintAnalysisSummary(IEnumerable<TaintAnalysisSummary<D>> parts) {
            MaximalFixpoint = parts.SelectMany(x=>x.MaximalFixpoint).ToHashSet();

            //TODO
            throw new NotImplementedException();
        }
        public ISet<D> MaximalFixpoint { get; }
        public MultiMap<ISourceElement, D> Leaks => finalState.leaks;
        public MultiMap<D, TaintSource<D>> Sources => finalState.factToSrc;
        public ISet<D> ReturnFacts => finalState.returnFacts;
    }
}
