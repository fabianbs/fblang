using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    class TaintAnalysisState {
        internal readonly MultiMap<IVariable, TaintSource> factToSrc = new MultiMap<IVariable, TaintSource>();
        internal readonly MultiMap<IStatement, IVariable> leaks = new MultiMap<IStatement, IVariable>();
        internal readonly ISet<IVariable> returnFacts = new HashSet<IVariable>();
    }
}
