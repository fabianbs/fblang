using System;
using System.Collections.Generic;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintAnalysisSummary {
        private readonly TaintAnalysisState finalState;

        internal TaintAnalysisSummary(ISet<IVariable> _mfp, TaintAnalysisState finalState) {
            MaximalFixedPoint = _mfp;
            this.finalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        }
        public ISet<IVariable> MaximalFixedPoint { get; }
        public MultiMap<IStatement, IVariable> Leaks => finalState.leaks;
        public MultiMap<IVariable, TaintSource> Sources => finalState.factToSrc;
        public ISet<IVariable> ReturnFacts => finalState.returnFacts;
    }
}
