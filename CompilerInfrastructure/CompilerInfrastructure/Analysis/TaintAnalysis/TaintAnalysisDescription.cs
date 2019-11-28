using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Analysis.Lattice;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using Imms;
using CompilerInfrastructure.Analysis;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public abstract class TaintAnalysisDescription<D> : MonoAnalysisDescription<TaintAnalysisSummary<D>, D> {

        readonly ITaintSourceDescription<D> sources;
        readonly ITaintSinkDescription<D> sinks;
        readonly Stack<TaintAnalysisState<D>> state = new Stack<TaintAnalysisState<D>>();
        public TaintAnalysisDescription(ITaintSourceDescription<D> sources, ITaintSinkDescription<D> sinks, ITaintAnalysisDomain<D> dom)
            : base(dom) {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
        }
        public new ITaintAnalysisDomain<D> Domain => (ITaintAnalysisDomain<D>) base.Domain;
        public void TaintPropagation(ISet<D> from, D to) {
            if (Domain.IsInDomain(to)) {
                var src = from.SelectMany(x => state.Peek().factToSrc[x]);
                state.Peek().factToSrc.Add(to, src);
            }
        }
        public void TaintPropagation(ISet<D> from, IEnumerable<D> tos) {
            var src = from.SelectMany(x => state.Peek().factToSrc[x]).ToArray();// avoid recomputation
            foreach (var to in tos.Where(x => Domain.IsInDomain(x))) {
                state.Peek().factToSrc.Add(to, src);
            }
        }
        public void TaintSource(TaintSource<D> src, IEnumerable<D> facts) {
            foreach (var fact in facts) {
                state.Peek().factToSrc.Add(fact, src);
            }
        }
        public void TaintLeak(ISourceElement stmt, IEnumerable<D> leakedFacts) {
            foreach (var lfact in leakedFacts) {
                TaintLeak(stmt, lfact);
            }
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TaintLeak(ISourceElement stmt, D lfact) {
            state.Peek().leaks.Add(stmt, lfact);
        }


       
        protected bool IsTainted(D vr, ISet<D> In) {
            return In.Contains(vr)/* || sources.IsSourceVariable(vr)*/;
        }
        protected bool IsAnyTainted(ISet<D> vars, ISet<D> In) {
            return !In.ToMonoSet().IsDisjointWith(vars);
        }

        public virtual ISet<D> CallFlow(ISet<D> In, CallExpression call) {
            bool needAnalysis = true;
            var Out = In.ToMonoSet();
            if (sinks.IsSinkMethod(call.Callee, out var paramLeaks)) {
                var leaks = paramLeaks
                    .Select(x => call.Arguments[x])
                    .SelectMany(x => Domain.ContainsFacts(x, out var vars) ? vars : Set.Empty<D>())
                    .Where(x => IsTainted(x, In));

                TaintLeak(call, leaks);

                needAnalysis = false;
            }
            if (sources.IsSourceMethod(call.Callee, out var paramTaints, out _)) {
                var nwTaints = paramTaints
                    .Select(x => call.Arguments[x])
                    .SelectMany(x => Domain.ContainsFacts(x, out var vars) ? vars : Set.Empty<D>());
                Out += nwTaints;
                TaintSource(new TaintSource<D>(call.Callee), nwTaints);
                needAnalysis = false;
            }

            if (needAnalysis) {
                var summary = Analysis.Query(call.Callee);
                var mapActualsToFormal = call.MapActualToFormalParameters();

                foreach (var actual in mapActualsToFormal) {
                    if (Domain.ContainsFacts(actual.Key, out var vars) && vars.Any(x => IsTainted(x, In))) {
                        if (summary.MaximalFixpoint.Contains(Domain.FactFromVariable(actual.Value))) {
                            TaintLeak(call, vars.Where(x => IsTainted(x, In)));
                        }
                    }
                }
            }

            return Out;
        }
        public override ISet<D> NormalFlow(ISet<D> In, IExpression expr) {
            if (sources.IsSourceExpression(expr, out var facts)) {
                return In.ToMonoSet() + facts;
            }
            switch (expr) {
                case CallExpression call:
                    return CallFlow(In, call);
                case BinOp bo: {
                    In = NormalFlow(In, bo.Left);
                    In = NormalFlow(In, bo.Right);
                    var Out = In.ToMonoSet();
                    if (bo.Operator.IsAssignment() && Domain.IsPropagatingAssignment(bo)) {
                        if (Domain.ContainsFacts(bo.Right, out var rhsFacts)) {
                            if (IsAnyTainted(rhsFacts, Out)) {
                                if (Domain.ContainsFacts(bo.Left, out var leftFacts)) {
                                    if (leftFacts.Any(x => sinks.IsSinkFact(x)))
                                        TaintLeak(expr, rhsFacts);
                                    TaintPropagation(Out.Intersect(rhsFacts), leftFacts);
                                }
                            }
                            else {
                                // strong update
                                if (Domain.ContainsFacts(bo.Left, out var leftFacts)) {
                                    Out -= leftFacts;
                                }
                            }
                        }
                    }
                    return Out;
                }
                default: {
                    foreach (var ex in expr.GetExpressions()) {
                        In = NormalFlow(In, ex);
                    }
                    return In;
                }
            }
        }
        public override ISet<D> NormalFlow(ISet<D> In, IStatement stmt) {
            if (sources.IsSourceStatement(stmt, out var facts)) {
                TaintSource(new TaintAnalysis.TaintSource<D>(stmt), facts);
                return In.ToMonoSet().Union(facts);
            }
            else if (stmt is ControlReturnStatement retStmt && retStmt.HasReturnValue) {
                if (Domain.ContainsFacts(retStmt.ReturnValue, out var vars)) {
                    state.Peek().returnFacts.UnionWith(In.ToMonoSet().Intersect(vars));
                    TaintLeak(stmt, vars.Where(x => sinks.IsSinkReturn(x)));
                }
            }
            return In;
        }
        public override void Initialize(IDeclaredMethod met) {
            var nwState = new TaintAnalysisState<D>();
            foreach (var seed in InitialSeeds(met)) {
                nwState.factToSrc.Add(seed, new TaintSource<D>(seed));
            }
            state.Push(nwState);
            base.Initialize(met);
        }

        public override void Finish() {
            base.Finish();
            state.Pop();
        }

        public override TaintAnalysisSummary<D> ComputeSummary(IDeclaredMethod met, ISet<D> mfp) {
            return new TaintAnalysisSummary<D>(mfp, state.Peek());
        }

    }
}
