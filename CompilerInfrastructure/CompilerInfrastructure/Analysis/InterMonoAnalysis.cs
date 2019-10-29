using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Analysis.CFG;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Utils;
using Imms;

namespace CompilerInfrastructure.Analysis {
    public abstract class InterMonoAnalysis<S, D> : IDataFlowAnalysis<S, ISet<D>, IDeclaredMethod> {
        readonly RecursiveLazyDictionary<IDeclaredMethod, S> summaries;
        readonly MonoAnalysisDescription<S, D> analysis;
        readonly ControlFlowGraph cfgBuilder ;
        public InterMonoAnalysis(MonoAnalysisDescription<S, D> _analysis, S dflt, ISemantics sem) {
            summaries = new RecursiveLazyDictionary<IDeclaredMethod, S>(MaximalFixpoint, dflt);
            analysis = _analysis ?? throw new ArgumentNullException(nameof(_analysis));
            analysis.Analysis = this;
            cfgBuilder = new ControlFlowGraph(sem);
        }


        ISet<D> FinalFacts(ISet<D> seeds, ICFGNode root, IEnumerable<ICFGNode> entries) {
            var solution = new Dictionary<ICFGNode, ISet<D>>();
            Vector<ICFGNode> finalNodes = default;
            root.ForEach(solution, (x, sol) => {
                if (solution.TryAdd(x, analysis.Bottom)) {
                    if (x.IsExitNode)
                        finalNodes.Add(x);
                    return true;
                }
                return false;
            });
            if (finalNodes.Count == 0) // no return
                return seeds;
            var worklist = new Queue<(ICFGNode, ICFGNode)>();

            void AddToWorkList(ICFGNode src, ICFGNode dest) {
                worklist.Enqueue((src, dest));
                foreach (var succ in dest.Next) {
                    worklist.Enqueue((dest, succ));
                }
            }

            #region initialize

            foreach (var entry in entries) {
                foreach (var succ in entry.Next) {
                    worklist.Enqueue((entry, succ));
                    solution[entry] = seeds;
                }
            }
            #endregion
            #region MFP computation
            while (worklist.TryDequeue(out var edge)) {
                var (src, dest) = edge;
                var In = solution[src];
                ISet<D> Out;
                if (src is StatementCFGNode stmt)
                    Out = analysis.NormalFlow(In, stmt.Statement);
                else if (src is ExpressionCFGNode expr)
                    Out = analysis.NormalFlow(In, expr.Expression);
                else
                    Out = In;

                var destSol = solution[dest];

                if (!analysis.SqSubsetEqual(Out, destSol)) {
                    solution[dest] = analysis.Join(destSol, Out);
                    AddToWorkList(src, dest);
                }
            }
            #endregion
            var finalResult = analysis.Bottom;
            foreach(var exitNode in finalNodes) {
                finalResult = analysis.Join(finalResult, solution[exitNode]);
            }
            return finalResult;
        }
        S MaximalFixpoint(IDeclaredMethod met) {
            analysis.Initialize();
            try {
                var facts = analysis.InitialSeeds();
                if (met.Body.HasValue) {
                    var root = cfgBuilder.Create(met);
                    facts = FinalFacts(facts, root, new[] { root });
                }
                else
                    facts = analysis.SummaryFlow(facts, met);

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
