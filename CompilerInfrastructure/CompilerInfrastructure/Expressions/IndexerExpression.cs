/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class IndexerExpression : ExpressionImpl, ICompileTimeEvaluable {
        readonly IExpression[] content;
        public IndexerExpression(Position pos, IType retTy, IExpression parent, ICollection<IExpression> args) : base(pos) {

            if (args is null || args.Count == 0) {
                "An array-indexer must have at least one index".Report(pos);
            }
            content = new IExpression[1 + (args?.Count ?? 0)];
            content[0] = parent ?? "A not-existing object cannot be indexed".Report(pos, Expression.Error);
            args?.CopyTo(content, 1);
            if (retTy is null) {
                if (ParentExpression.ReturnType.UnWrap() is AggregateType agg) {
                    ReturnType = agg.ItemType;
                }
                else
                    ReturnType = "The type of the array-index expression cannot be inferred and must be specified explicitly".ReportTypeError(pos);
            }
            else
                ReturnType = retTy;
        }
        public IExpression ParentExpression => content[0];
        public Span<IExpression> Indices {
            get => new Span<IExpression>(content, 1, content.Length - 1);
        }
        public override bool IsLValue(IMethod met) {

            return !ParentExpression.ReturnType.IsConstant();
        }

        public override IType ReturnType {
            get;
        }
        bool? compileTimeEvaluable=null;
        public bool IsCompileTimeEvaluable {
            get {
                if (compileTimeEvaluable is null) {// Currently focus on evaluating 1-dimensional arrays
                    compileTimeEvaluable = content.Length == 2 && content.All(x => x.IsCompileTimeEvaluable());
                }
                return compileTimeEvaluable.Value;
            }
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        public override IEnumerable<IExpression> GetExpressions() => content;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new IndexerExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                ParentExpression.Replace(genericActualParameter, curr, parent),
                content.Skip(1).Select(x => x.Replace(genericActualParameter, curr, parent)).AsCollection(content.Length - 1)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            if (ParentExpression.TryReplaceMacroParameters(args, out var nwPar)) {
                changed = true;
                if (nwPar.Length != 1) {
                    "An indexer cannot index a variable number of objects".Report(Position.Concat(args.Position));
                    nwPar = content;
                }
            }
            else
                nwPar = content;

            var nwIdx = content.Skip(1).SelectMany(x => {
                if (x.TryReplaceMacroParameters(args, out var nwInd)) {
                    changed = true;
                    return nwInd;
                }
                return new[] { x };
            }).ToArray();

            if (changed) {
                expr = new[] { args.Semantics.CreateIndexer(Position.Concat(args.Position), ReturnType.IsTop() ? null : ReturnType, nwPar[0].ReturnType, nwPar[0], nwIdx) };
                return true;
            }
            expr = new[] { this };
            return false;
        }

        public ILiteral Evaluate(ref EvaluationContext context) {
            if (Indices.Length != 1)// do not evaluate multi-dimensional array-accesses right now (maybe in future)
                return null;
            if (!ParentExpression.TryEvaluate(ref context, out var arr) || !(arr is ArrayLiteral arrLit))
                return null;
            if (!Indices[0].TryEvaluate(ref context, out var idx))
                return null;
            return Literal.WithIntegerType(idx, x => arrLit[Position, x], x => arrLit[Position, x], x => arrLit[Position, x]);
        }
    }
}
