using System;
using System.Collections.Generic;
using System.Linq;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    using System.Collections.Concurrent;
    using System.Text;

    public readonly struct TaintAnalysisSummary<D> {
        private readonly TaintAnalysisState<D> finalState;

        internal TaintAnalysisSummary(ISet<D> _mfp, TaintAnalysisState<D> finalState) {
            MaximalFixpoint = _mfp;
            this.finalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
        }
        internal TaintAnalysisSummary(IEnumerable<TaintAnalysisSummary<D>> parts) {
            MaximalFixpoint = parts.SelectMany(x => x.MaximalFixpoint).ToHashSet();

            //TODO
            throw new NotImplementedException();
        }
        public ISet<D> MaximalFixpoint { get; }
        public MultiMap<ISourceElement, D> Leaks => finalState.leaks;
        public MultiMap<D, TaintSource<D>> Sources => finalState.factToSrc;
        public ISet<D> ReturnFacts => finalState.returnFacts;

        /// <inheritdoc />
        public override string ToString() {
            var sb =  new StringBuilder();
            var sep = "," + Environment.NewLine + "\t\t";
            var sepp = "," + Environment.NewLine + "\t\t\t";
            sb.AppendLine("Analysis Results: {");
            // final facts
            sb.Append("\tFinal dataflow facts: {");
            if (MaximalFixpoint.Any()) {
                sb.AppendLine();
                sb.Append("\t\t");
                sb.AppendJoin(sep, MaximalFixpoint);
                sb.AppendLine();
            }
            
            sb.AppendLine("\t},");
            // leaks
            sb.Append("\tLeaks: {");
            if (Leaks.Any()) {
                sb.AppendLine();
                sb.Append("\t\t");
                bool frst = true;
                foreach (var (src, facts) in Leaks) {
                    if (frst)
                        frst = false;
                    else
                        sb.AppendLine("\t\t},");
                    sb.AppendLine(src.Position.ToString().CapitalizeTrim() + ": {");
                    sb.Append("\t\t\t");
                    sb.AppendJoin(sepp, facts);
                    sb.AppendLine();
                }

                sb.AppendLine("\t\t}");
                sb.AppendLine(ReturnFacts.Any() ? "\t}," : "\t}");
            }

            // resulting facts
            if (ReturnFacts.Any()) {
                sb.AppendLine("\tReturned dataflow facts: {");
                sb.Append("\t\t");
                sb.AppendJoin(sep, ReturnFacts);
                sb.AppendLine();
                sb.AppendLine("\t}");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
