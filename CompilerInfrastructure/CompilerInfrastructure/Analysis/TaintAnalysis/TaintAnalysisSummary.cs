using System;
using System.Collections.Generic;
using System.Linq;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintAnalysisSummary {
        private readonly TaintAnalysisState finalState;

        internal TaintAnalysisSummary(ISet<IVariable> _mfp, TaintAnalysisState finalState) {
            MaximalFixedPoint = _mfp;
            this.finalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        }
        internal TaintAnalysisSummary(IEnumerable<TaintAnalysisSummary> parts) {
            MaximalFixedPoint = parts.SelectMany(x=>x.MaximalFixedPoint).ToHashSet();

            //TODO
            throw new NotImplementedException();
        }
        public ISet<IVariable> MaximalFixedPoint { get; }
        public MultiMap<ISourceElement, IVariable> Leaks => finalState.leaks;
        public MultiMap<IVariable, TaintSource> Sources => finalState.factToSrc;
        public ISet<IVariable> ReturnFacts => finalState.returnFacts;
    }
}
