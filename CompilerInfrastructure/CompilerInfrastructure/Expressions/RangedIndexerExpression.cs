using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class RangedIndexerExpression : ExpressionImpl {
        readonly IExpression[] content;
        readonly bool offsToEnd;

        public RangedIndexerExpression(Position pos, IType retTy, IExpression parent, IExpression offset, IExpression count = null) : base(pos) {
            content = new[] { parent, offset, count ?? Literal.Null };
            offsToEnd = count is null;
            if (retTy is null) {
                if (parent.ReturnType.UnWrap() is AggregateType agg) {
                    if (agg.IsVarArg())
                        retTy = agg;
                    else
                        retTy = agg.ItemType.AsSpan();
                }
                else if (parent.ReturnType.IsError())
                    retTy = Type.Error;
                else
                    retTy = $"The type for the ranged-indexer expression on an object of the type {parent.ReturnType.Signature} cannot be inferred and therefor must be specified explicitly".Report(pos, Type.Error);
            }
            ReturnType = retTy;
        }
        public IExpression Parent => content[0];
        public IExpression Offset => content[1];
        public IExpression Count => content[2];
        /// <summary>
        /// True, iff the Count is a unused value, which will be replaced by the number of remaining elements after Offset
        /// </summary>
        public bool IsOffsetToEnd => offsToEnd;
        public override IType ReturnType {
            get;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        public override IEnumerable<IExpression> GetExpressions() => content;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new RangedIndexerExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Parent.Replace(genericActualParameter, curr, parent),
                Offset.Replace(genericActualParameter, curr, parent),
                Count.Replace(genericActualParameter, curr, parent)
            );
        }
        static readonly string[] CONTENT_NAMES = { "range", "offset", "length" };
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;

            var nwContent = content.Select((x, i) => {
                if (x.TryReplaceMacroParameters(args, out var nwExpr)) {
                    if (nwExpr.Length != 1) {
                        $"A ranged indexer-expression can only have one {CONTENT_NAMES[i]}-expression".Report(x.Position.Concat(args.Position));
                    }
                    changed = true;
                    return nwExpr.FirstOrDefault();
                }
                return x;
            }).ToArray();
            if (changed) {
                expr = new[] { new RangedIndexerExpression(Position.Concat(args.Position),
                    null,
                    nwContent[0],
                    nwContent[1],
                    nwContent[2]
                ) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
