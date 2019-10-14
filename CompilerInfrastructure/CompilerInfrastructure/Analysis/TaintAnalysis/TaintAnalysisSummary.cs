using System.Collections.Generic;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintAnalysisSummary {
        internal TaintAnalysisSummary(ISet<IVariable> _mfp, MultiMap<IStatement, (IVariable, TaintSource)> _leaks, ISet<IVariable> _returnFacts) {
            MaximalFixedPoint = _mfp;
            Leaks = _leaks;
            ReturnFacts = _returnFacts;
        }
        public ISet<IVariable> MaximalFixedPoint { get; }
        public MultiMap<IStatement, (IVariable, TaintSource)> Leaks { get; }
        public ISet<IVariable> ReturnFacts { get; }
    }
}
