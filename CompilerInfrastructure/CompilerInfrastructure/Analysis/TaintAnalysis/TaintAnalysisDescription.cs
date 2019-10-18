using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using Imms;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public class TaintAnalysisDescription<Dom> : MonoAnalysisDescription<TaintAnalysisSummary, IVariable> where Dom : IAnalysisDomain<IVariable>, new() {
        static readonly Dom dom = new Dom();
        readonly ITaintSourceDescription sources;
        readonly ITaintSinkDescription sinks;
        readonly Stack<TaintAnalysisState> state = new Stack<TaintAnalysisState>();
        public TaintAnalysisDescription(ITaintSourceDescription sources, ITaintSinkDescription sinks)
            : base(dom) {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
        }

        public void TaintPropagation(ISet<IVariable> from, IVariable to) {
            if (dom.IsInDomain(to)) {
                var src = from.SelectMany(x => state.Peek().factToSrc[x]);
                state.Peek().factToSrc.Add(to, src);
            }
        }
        public void TaintPropagation(ISet<IVariable> from, IEnumerable<IVariable> tos) {
            var src = from.SelectMany(x => state.Peek().factToSrc[x]).ToArray();// avoid recomputation
            foreach (var to in tos.Where(x => dom.IsInDomain(x))) {
                state.Peek().factToSrc.Add(to, src);
            }
        }
        public void TaintSource(TaintSource src, IEnumerable<IVariable> facts) {
            foreach (var fact in facts) {
                state.Peek().factToSrc.Add(fact, src);
            }
        }
        public void TaintLeak(IStatement stmt, IEnumerable<IVariable> leakedFacts) {
            foreach (var lfact in leakedFacts) {
                TaintLeak(stmt, lfact);
            }
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TaintLeak(IStatement stmt, IVariable lfact) {
            state.Peek().leaks.Add(stmt, lfact);
        }


        public virtual bool IsVariableValue(IExpression expr, out ISet<IVariable> vars) {
            switch (expr) {
                case VariableAccessExpression vrx:
                    vars = ImmSet.Of(vrx.Variable);
                    return true;
                case BinOp ass when ass.Operator.IsAssignment():
                    return IsVariableValue(ass.Right, out vars);
                case CallExpression call: {
                    var  _vars = ImmSet.Empty<IVariable>();
                    //TODO CallGraphAnalysis
                    //TODO explicit param-to-return flow
                    var summary = Analysis.Query(call.Callee);
                    var returnedVariableFacts = summary.ReturnFacts.SelectMany(x => summary.Sources[x]).Where(x => x.IsSourceVariable).Select(x => x.SourceVariable).ToImmSet();
                    foreach (var arg in call.MapActualToFormalParameters()) {
                        if (returnedVariableFacts.Contains(arg.Value) && IsVariableValue(arg.Key, out var argVars)) {
                            _vars += argVars;
                        }
                    }
                    vars = _vars;
                    return !_vars.IsEmpty;
                }
                default:
                    vars = ImmSet.Empty<IVariable>();
                    return false;
            }
        }
        protected bool IsTainted(IVariable vr, ISet<IVariable> In) {
            return In.Contains(vr)/* || sources.IsSourceVariable(vr)*/;
        }
        protected bool IsAnyTainted(ISet<IVariable> vars, ISet<IVariable> In) {
            return !In.ToImmSet().IsDisjointWith(vars);
        }

        public virtual ISet<IVariable> CallFlow(ISet<IVariable> In, CallExpression call, IStatement parent) {
            bool needAnalysis = true;
            var Out = In.ToImmSet();
            //TODO CallGraphAnalysis
            if (sinks.IsSinkMethod(call.Callee, out var paramLeaks)) {
                var leaks = paramLeaks
                    .Select(x => call.Arguments[x])
                    .SelectMany(x => IsVariableValue(x, out var vars) ? vars : ImmSet.Empty<IVariable>())
                    .Where(x => IsTainted(x, In));

                TaintLeak(parent, leaks);

                needAnalysis = false;
            }
            if (sources.IsSourceMethod(call.Callee, out var paramTaints, out _)) {
                var nwTaints = paramTaints
                    .Select(x=>call.Arguments[x])
                    .SelectMany(x => IsVariableValue(x, out var vars) ? vars : ImmSet.Empty<IVariable>());
                Out = Out.Union(nwTaints);
                TaintSource(new TaintSource(call.Callee), nwTaints);
                needAnalysis = false;
            }

            if (needAnalysis) {
                var summary = Analysis.Query(call.Callee);
                var mapActualsToFormal = call.MapActualToFormalParameters();

                foreach (var actual in mapActualsToFormal) {
                    if (IsVariableValue(actual.Key, out var vars) && vars.Any(x => IsTainted(x, In))) {
                        if (summary.MaximalFixedPoint.Contains(actual.Value)) {
                            TaintLeak(parent, vars.Where(x => IsTainted(x, In)));
                        }
                    }
                }
            }

            return Out;
        }
        public override ISet<IVariable> NormalFlow(ISet<IVariable> In, IExpression expr, IStatement parent) {
            switch (expr) {
                case CallExpression call:
                    return CallFlow(In, call, parent);
                case BinOp bo: {
                    In = NormalFlow(In, bo.Left, parent);
                    In = NormalFlow(In, bo.Right, parent);
                    var Out = In.ToImmSet();
                    if (bo.Operator.IsAssignment()) {
                        if (IsVariableValue(bo.Right, out var rhsFacts)) {
                            if (IsAnyTainted(rhsFacts, Out)) {
                                if (IsVariableValue(bo.Left, out var leftFacts)) {
                                    TaintPropagation(Out.Intersect(rhsFacts), leftFacts);
                                }
                            }
                            else {
                                // strong update
                                if (IsVariableValue(bo.Left, out var leftFacts)) {
                                    Out -= leftFacts;
                                }
                            }
                        }
                    }
                    return Out;
                }
                default: {
                    foreach (var ex in expr.GetExpressions()) {
                        In = NormalFlow(In, ex, parent);
                    }
                    return In;
                }
            }
        }
        public override ISet<IVariable> NormalFlow(ISet<IVariable> In, IStatement stmt) {
            if (sources.IsSourceStatement(stmt, out var facts)) {
                TaintSource(new TaintAnalysis.TaintSource(stmt), facts);
                return In.ToImmSet().Union(facts);
            }
            else if (stmt is ControlReturnStatement retStmt && retStmt.HasReturnValue) {
                if (IsVariableValue(retStmt.ReturnValue, out var vars))
                    state.Peek().returnFacts.UnionWith(In.ToImmSet().Intersect(vars));
            }
            return In;
        }
        public override void Initialize() {
            state.Push(new TaintAnalysisState());
            base.Initialize();
        }

        public override void Finish() {
            base.Finish();
            state.Pop();
        }

        public override TaintAnalysisSummary ComputeSummary(IDeclaredMethod met, ISet<IVariable> mfp) {
            return new TaintAnalysisSummary(mfp, state.Peek());
        }
    }
}
