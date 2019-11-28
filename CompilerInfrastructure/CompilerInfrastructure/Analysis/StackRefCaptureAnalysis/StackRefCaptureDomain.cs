using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Expressions;
using Imms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    public class StackRefCaptureDomain : ITaintAnalysisDomain<StackCaptureFact> {
        InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> Analysis;
        public StackRefCaptureDomain(InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> analysis) {
            Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        }
        public bool ContainsFacts(IExpression expr, out ISet<StackCaptureFact> vars) {
            switch (expr) {
                case VariableAccessExpression vrx:
                    vars = MonoSet.Of(FactFromVariable(vrx.Variable));
                    return true;
                case BinOp ass when ass.Operator.IsAssignment():
                    return ContainsFacts(ass.Right, out vars);
                case CallExpression call: {
                    var  _vars = MonoSet.Empty<StackCaptureFact>();
                    // No CallGraphAnalysis (would require whole-program CG)
                    // explicit param-to-return flow
                    var summary = Analysis.Query(call.Callee);
                    var returnedVariableFacts = summary.ReturnFacts
                        .SelectMany(x => summary.Sources[x])
                        .Where(x => x.IsSourceVariable)
                        .Select(x => x.SourceFact)
                        .ToMonoSet();
                    foreach (var arg in call.MapActualToFormalParameters()) {
                        if (returnedVariableFacts.Contains(FactFromVariable(arg.Value)) && ContainsFacts(arg.Key, out var argVars)) {
                            _vars += argVars;
                        }
                    }
                    vars = _vars;
                    return !_vars.IsEmpty;
                }
                default:
                    vars = MonoSet.Empty<StackCaptureFact>();
                    return false;
            }
        }
        public StackCaptureFact FactFromVariable(IVariable vr) {
            if (vr.Type.IsByRef())
                return new StackCaptureFact(vr, false);
            return new StackCaptureFact(vr, true);
        }

        public bool IsInDomain(IVariable fact) {
            return fact != null && fact.Type.IsRef();
        }

        public bool IsInDomain(StackCaptureFact fact) {
            return IsInDomain(fact.Variable);
        }
        public bool IsPropagatingAssignment(BinOp ass) {
            return ass.IsRefReassignment;
        }
    }
}
