using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.CallGraph {
    public interface ICallGraphAnalysis {
        ISet<IDeclaredMethod> GetAllCallees(CallExpression call);
    }
}
