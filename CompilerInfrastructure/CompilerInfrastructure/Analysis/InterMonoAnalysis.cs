using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using Imms;

namespace CompilerInfrastructure.Analysis {
    public abstract class InterMonoAnalysis<S, D> : IDataFlowAnalysis<S, ISet<D>, IDeclaredMethod> {
        readonly RecursiveLazyDictionary<IDeclaredMethod, S> summaries;
        readonly MonoAnalysisDescription<S, D> analysis;
        public InterMonoAnalysis(MonoAnalysisDescription<S, D> _analysis, S dflt) {
            summaries = new RecursiveLazyDictionary<IDeclaredMethod, S>(MaximalFixedPoint, dflt);
            analysis = _analysis ?? throw new ArgumentNullException(nameof(_analysis));
            analysis.Analysis = this;
        }


        protected virtual ISet<D> Flow(ISet<D> In, IStatement stmt, out bool terminated) {
            switch (stmt) {
                case null:
                case NopStatement _:
                    terminated = false;
                    return In;
                case IfStatement ifStmt: {
                    In = analysis.NormalFlow(In, ifStmt.Condition, ifStmt);
                    bool elseTerm = false;
                    var OutThen = Flow(In, ifStmt.ThenStatement, out bool thenTerm);
                    var OutElse = ifStmt.ElseStatement != null ? Flow(In, ifStmt.ElseStatement, out elseTerm) : In;
                    terminated = elseTerm && thenTerm;
                    return analysis.Merge(OutThen, OutElse);
                }
                case ForeachLoop feLoop: {
                    In = analysis.NormalFlow(In, feLoop.Range, feLoop);
                    terminated = false;
                    bool repeat;
                    do {
                        var Out = Flow(In, feLoop.Body, out var term);
                        if (term)
                            return Out;
                        repeat = analysis.SqSubsetEqual(Out, In);
                        In = Out;
                    } while (repeat);
                    return In;
                }
                case ForLoop forLoop: {
                    In = analysis.NormalFlow(In, forLoop.Initialization);
                    In = analysis.NormalFlow(In, forLoop.Condition, forLoop);
                    terminated = false;
                    bool repeat;
                    do {
                        var Out = Flow(In, forLoop.Body,out var term);
                        if (term)
                            return Out;
                        Out = analysis.NormalFlow(Out, forLoop.Condition, forLoop);
                        repeat = analysis.SqSubsetEqual(Out, In);
                        In = Out;
                    } while (repeat);
                    return In;
                }
                case DoWhileLoop doWileLoop: {
                    bool repeat;
                    do {
                        var Out = Flow(In, doWileLoop.Body, out terminated);
                        if (terminated)
                            return Out;
                        Out = analysis.NormalFlow(Out, doWileLoop.Condition, doWileLoop);
                        repeat = analysis.SqSubsetEqual(Out, In);
                        In = Out;
                    } while (repeat);
                    return In;
                }
                case WhileLoop whileLoop: {
                    In = analysis.NormalFlow(In, whileLoop.Condition, whileLoop);
                    terminated = false;
                    bool repeat;
                    do {
                        var Out = Flow(In, whileLoop.Body, out bool term);
                        if (term)
                            return Out;
                        Out = analysis.NormalFlow(Out, whileLoop.Condition, whileLoop);
                        repeat = analysis.SqSubsetEqual(Out, In);
                        In = Out;
                    } while (repeat);

                    return In;
                }
                case BlockStatement blockStmt: {
                    terminated = false;
                    foreach (var sub in blockStmt.Statements) {
                        In = Flow(In, sub, out terminated);
                        if (terminated)
                            break;
                    }
                    return In;
                }
                case ExpressionStmt exprStmt:
                    terminated = false;
                    return analysis.NormalFlow(In, exprStmt.Expression, exprStmt);
                default:
                    terminated = stmt is ITerminatorStatement; // approximation at 'continue' stmts
                    return analysis.NormalFlow(In, stmt);
            }
        }
        S MaximalFixedPoint(IDeclaredMethod met) {
            analysis.Initialize();
            try {
                var facts = analysis.InitialSeeds();
                if (met.Body.HasValue)
                    facts = Flow(facts, met.Body.Instruction, out _);

                return analysis.ComputeSummary(met, facts);
            }
            finally {
                analysis.Finish();
            }
        }
        public S Query(IDeclaredMethod node) {
            return summaries[node];
        }
    }
}
