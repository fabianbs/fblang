using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using Imms;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public class TaintAnalysisDescription : MonoAnalysisDescription<TaintAnalysisSummary, IVariable> {
        readonly ITaintSourceDescription sources;
        readonly ITaintSinkDescription sinks;
        readonly MultiMap<IVariable, TaintSource> factToSrc = new MultiMap<IVariable, TaintSource>();
        readonly MultiMap<IStatement, (IVariable, TaintSource)> leaks = new MultiMap<IStatement, (IVariable, TaintSource)>();
        public TaintAnalysisDescription(ITaintSourceDescription sources, ITaintSinkDescription sinks) {
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
            this.sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
        }
        public void TaintPropagation(ISet<IVariable> from, IVariable to) {
            var src = from.SelectMany(x => factToSrc[x]);
            factToSrc.Add(to, src);
        }
        public void TaintSource(TaintSource src, IEnumerable<IVariable> facts) {
            foreach (var fact in facts) {
                factToSrc.Add(fact, src);
            }
        }
        public void TaintLeak(IStatement stmt, IEnumerable<IVariable> leakedFacts) {
            foreach (var lfact in leakedFacts) {
                TaintLeak(stmt, lfact);
            }
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void TaintLeak(IStatement stmt, IVariable lfact) {
            foreach (var src in factToSrc[lfact]) {
                leaks.Add(stmt, (lfact, src));
            }
        }
        public override ISet<IVariable> NormalFlow(ISet<IVariable> In, IStatement stmt) {
            //TODO
            throw new NotImplementedException();
        }

        public virtual bool IsVariableValue(IExpression expr, out ISet<IVariable> vars) {
            //TODO
            throw new NotImplementedException();
        }

        public virtual ISet<IVariable> CallFlow(ISet<IVariable> In, CallExpression call, IStatement parent) {
            bool needAnalysis = true;
            var Out = In.ToImmSet();
            if (sinks.IsSinkMethod(call.Callee, out var paramLeaks)) {
                var leaks = paramLeaks
                    .Select(x => call.Arguments[x])
                    .SelectMany(x => IsVariableValue(x, out var vars) ? vars : ImmSet.Empty<IVariable>())
                    .Where(x => In.Contains(x));

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
                var mapFormalToActual = call.MapFormalToActualParameters();
                // DOLATER: more efficient solution...
                foreach (var (fact, src) in summary.Leaks.Values.SelectMany(x => x).Where(x => x.Item2.TryGetSourceVariable(out var vr) && vr.IsLocalVariable())) {
                    if (mapFormalToActual.TryGetValue(fact, out var actuals)
                        && actuals.SelectMany(x => IsVariableValue(x, out var vars) ? vars : ImmSet.Empty<IVariable>()).Intersect(In).Any()) {

                        TaintLeak(parent, fact);
                    }
                }
            }

            return Out;
        }
        public override ISet<IVariable> NormalFlow(ISet<IVariable> In, IExpression expr, IStatement parent) {
            if (expr is CallExpression call) {
                return CallFlow(In, call, parent);
            }
            //TODO rest
            throw new NotImplementedException();
        }
    }
}
