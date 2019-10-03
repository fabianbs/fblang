using CompilerInfrastructure;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Imms;
using CompilerInfrastructure.Expressions;

namespace FBc.Analysis {
    public abstract class InterMonoAnalysis<D> : IDataFlowAnalysis<IDeclaredMethod, ISet<D>> {
        readonly RecursiveLazyDictionary<(IDeclaredMethod, ISet<D>), ISet<D>> cache;
        readonly AnalysisDirection dir;

        public InterMonoAnalysis(AnalysisDirection _dir, IEqualityComparer<(IDeclaredMethod, ISet<D>)> metComp = null) {
            cache = new RecursiveLazyDictionary<(IDeclaredMethod, ISet<D>), ISet<D>>(Analyze, Set.Empty<D>(), metComp);
            dir = _dir;
        }
        protected abstract ImmSet<D> NormalFlow(IExpression expr, ImmSet<D> In);
        protected abstract ImmSet<D> NormalFlow(IStatement stmt, ImmSet<D> In);
        protected abstract ImmSet<D> CallFlow(CallExpression call, ImmSet<D> In, Func<(IDeclaredMethod, ISet<D>), ISet<D>> FF);
        protected virtual ImmSet<D> Meet(ImmSet<D> frst, ImmSet<D> scnd) {
            return frst.Union(scnd);
        }
        ImmSet<D> ForwardFlow(IStatement stmt, ImmSet<D> In, Func<(IDeclaredMethod, ISet<D>), ISet<D>> FF) {
            ImmSet<D> LoopFlow(IExpression cond, IStatement body) {
                var _In = In;
                bool repeat;
                do {
                    var Out = cond != null ? NormalFlow(cond, _In) :_In;
                    Out = Flow(body, Out, FF);

                    repeat = !Out.SetEquals(_In);
                    _In = Out;
                } while (repeat);
                return _In;
            }
            switch (stmt) {
                case IfStatement ifStmt:
                    In = NormalFlow(ifStmt.Condition, In);
                    var thenCase = Flow(ifStmt.ThenStatement, In, FF);
                    var elseCase = ifStmt.ElseStatement != null ? Flow(ifStmt.ElseStatement, In, FF) : In;
                    return Meet(thenCase, elseCase);
                case ForLoop forLoop:
                    In = Flow(forLoop.Initialization, In, FF);
                    return LoopFlow(forLoop.Condition, forLoop.Body);
                case ForeachLoop foreachLoop:
                    In = NormalFlow(foreachLoop.Range, In);
                    return LoopFlow(null, foreachLoop.Body);
                case WhileLoop whileLoop when whileLoop.IsHeadControlled:
                    return LoopFlow(whileLoop.Condition, whileLoop.Body);
                case WhileLoop doWhileLoop:
                    In = Flow(doWhileLoop.Body, In, FF);
                    return LoopFlow(doWhileLoop.Condition, doWhileLoop.Body);
                case BlockStatement blockStmt:
                    foreach (var subStmt in blockStmt.Statements) {
                        In = Flow(subStmt, In, FF);
                    }
                    return In;
                case ExpressionStmt estmt when estmt.Expression is CallExpression call:
                    return CallFlow(call, In, FF);
                case ExpressionStmt estmt:
                    return NormalFlow(estmt.Expression, In);
                default:
                    return NormalFlow(stmt, In);
            }
        }
        ImmSet<D> BackwardFlow(IStatement stmt, ImmSet<D> In, Func<(IDeclaredMethod, ISet<D>), ISet<D>> FF) {
            //TODO
            return In;
        }
        ImmSet<D> Flow(IStatement stmt, ImmSet<D> In, Func<(IDeclaredMethod, ISet<D>), ISet<D>> FF) {
            return dir switch
            {
                AnalysisDirection.Forward => ForwardFlow(stmt, In, FF),
                AnalysisDirection.Backward => BackwardFlow(stmt, In, FF),
                _ => throw new ArgumentException(),
            };
        }
        ISet<D> Analyze((IDeclaredMethod, ISet<D>) node, Func<(IDeclaredMethod, ISet<D>), ISet<D>> FF) {
            var (met, seeds) = node;
            if (met is null || !met.Body.HasValue)
                return seeds;
            var facts = seeds.ToImmSet();

            return Flow(met.Body.Instruction, facts, FF);
        }
        public ISet<D> Query(IDeclaredMethod node, ISet<D> initialSeeds) {
            return cache[(node, initialSeeds ?? Set.Empty<D>())];
        }
    }

}
