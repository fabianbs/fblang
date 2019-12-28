using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintSourceDescription<D> {
        bool IsSourceMethod(IDeclaredMethod met, out ISet<int> parameterTaintIdx, out bool taintsReturnValue);
        //bool IsSourceVariable(IVariable vr);
        bool IsSourceStatement(IStatement stmt, out ISet<D> facts);
        bool IsSourceExpression(IExpression expr, out ISet<D> facts);
    }
}
