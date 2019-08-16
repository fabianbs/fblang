using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class Deconstruction : StatementImpl {
        readonly IExpression[] content;

        public Deconstruction(Position pos, IEnumerable<IExpression> dest, IExpression range, bool checkConsistency = false) : base(pos) {
            content =
                new[] { range ?? Expression.Error }
                .Concat(dest != null && dest.Any() ? dest : Enumerable.Empty<IExpression>())
                .ToArray();

            if (checkConsistency) {
                CheckConsistency(pos, dest, range);
            }
        }
        public static bool CheckConsistency(Position pos, IEnumerable<IExpression> dest, IExpression range, ErrorBuffer err = null, IMethod met=null) {
            if (range is null) {
                err.Report("The range which is deconstructed must not be null", pos);
                return false;
            }
            if (dest is null || !dest.Any()) {
                err.Report("For a deconstruction statement there must be at least one destination", pos);
                return false;
            }
            else if ((dest = dest.Where(x => !x.IsLValue(met) && !x.IsUnpack() && !x.IsError())).Any()) {
                bool onlyOne = dest?.ElementAtOrDefault(1) is null;
                err.Report($"The expression{(onlyOne ? "" : "s")} {string.Join(", ", dest)} {(onlyOne ? "is" : "are")} not lvalue and cannot be the destination of a deconstruction", pos);
                return false;
            }
            return true;
        }
        private Deconstruction(Position pos, IExpression[] cont) : base(pos) {
            content = cont;
        }
        public IExpression Range => content[0];
        public Span<IExpression> Destination => new Span<IExpression>(content, 1, content.Length - 1);

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new Deconstruction(Position, content.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray());
        }

        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public override IEnumerable<IExpression> GetExpressions() => content;
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Range.TryReplaceMacroParameters(args, out var nwRange)) {
                changed = true;
                if (nwRange.Length != 1) {
                    "A deconstruction can only have one range to destruct".Report(Range.Position.Concat(args.Position));
                    nwRange = new[] { Range };
                }
                else if (!(nwRange[0].ReturnType.UnWrap() is AggregateType)) {
                    "The expression-parameters must be substituted such that the destructed expression is a destructible range".Report(Range.Position.Concat(args.Position));
                }
            }
            else
                nwRange = new[] { Range };
            var nwDest = content.Skip(1).SelectMany(x => {
                if (x.TryReplaceMacroParameters(args, out var nwD)) {
                    changed = true;
                    return nwD;
                }
                return new[] { x };
            });

            if (changed) {
                stmt = new Deconstruction(Position.Concat(args.Position), nwDest, nwRange.First(), true);
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
