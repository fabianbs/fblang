using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    public class StackRefCaptureDomain : ITaintAnalysisDomain<StackCaptureFact> {
        readonly InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> analysis;
        public StackRefCaptureDomain(InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> analysis) {
            this.analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        }

        public bool ContainsFacts(IExpression expr, out ISet<StackCaptureFact> vars) {
            while (true) {
                switch (expr) {
                    case VariableAccessExpression vrx:
                        vars = MonoSet.Of(FactFromVariable(vrx.Variable));
                        return true;
                    case BinOp ass when ass.Operator.IsAssignment():
                        expr = ass.Right;
                        continue;
                    case CallExpression call: {
                        var _vars = MonoSet.Empty<StackCaptureFact>();
                        // No CallGraphAnalysis (would require whole-program CG)
                        // explicit param-to-return flow
                        var summary = analysis.Query(call.Callee);
                        var returnedVariableFacts = summary.ReturnFacts.SelectMany(x => summary.Sources[x])
                                                           .Where(x => x.IsSourceVariable)
                                                           .Select(x => x.SourceFact)
                                                           .ToMonoSet();
                        foreach (var (actual, formal) in call.MapActualToFormalParameters()) {
                            if (returnedVariableFacts.Contains(FactFromVariable(formal)) && ContainsFacts(actual, out var argVars)) {
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
        }

        public StackCaptureFact FactFromVariable(IVariable vr) {
            return vr.Type.IsByRef() ? new StackCaptureFact(vr, false) : new StackCaptureFact(vr, true);
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
