using CompilerInfrastructure.Expressions;
using Imms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public interface ITaintAnalysisDomain<D> : IDataFlowDomain<D> {
        bool ContainsFacts(IExpression expr, out ISet<D> facts);
        D FactFromVariable(IVariable vr);
    }
    public abstract class TaintAnalysisVariableDomain : ITaintAnalysisDomain<IVariable> {
        InterMonoAnalysis<TaintAnalysisSummary<IVariable>, IVariable> Analysis;
        public TaintAnalysisVariableDomain(InterMonoAnalysis<TaintAnalysisSummary<IVariable>, IVariable> analysis) {
            Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        }
        public bool ContainsFacts(IExpression expr, out ISet<IVariable> vars) {
            switch (expr) {
                case VariableAccessExpression vrx:
                    vars = MonoSet.Of(vrx.Variable);
                    return true;
                case BinOp ass when ass.Operator.IsAssignment():
                    return ContainsFacts(ass.Right, out vars);
                case CallExpression call: {
                    var  _vars = MonoSet.Empty<IVariable>();
                    // No CallGraphAnalysis (would require whole-program CG)
                    // explicit param-to-return flow
                    var summary = Analysis.Query(call.Callee);
                    var returnedVariableFacts = summary.ReturnFacts
                        .SelectMany(x => summary.Sources[x])
                        .Where(x => x.IsSourceVariable)
                        .Select(x => x.SourceFact)
                        .ToMonoSet();
                    foreach (var arg in call.MapActualToFormalParameters()) {
                        if (returnedVariableFacts.Contains(arg.Value) && ContainsFacts(arg.Key, out var argVars)) {
                            _vars += argVars;
                        }
                    }
                    vars = _vars;
                    return !_vars.IsEmpty;
                }
                default:
                    vars = MonoSet.Empty<IVariable>();
                    return false;
            }
        }

        public IVariable FactFromVariable(IVariable vr) => vr;
        public abstract bool IsInDomain(IVariable fact);
    }
}
