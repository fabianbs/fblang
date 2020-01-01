/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    using CompilerInfrastructure.Structure.Types;

    [Serializable]
    class ContiguousRangeExpression : ExpressionImpl {
        readonly IExpression[] exprs;
        public ContiguousRangeExpression(Position pos, IType retTy, IExpression from, IExpression to) : base(pos) {
            exprs = new[] { from ?? "The start of a range-expression must be specified explicitly".Report(pos, Expression.Error), to ?? "The exclusive end of a range-expression must be specified explicitly".Report(pos, Expression.Error) };
            if (retTy is null && !from.ReturnType.IsTop() && !to.ReturnType.IsTop()) {
                var itemTy = Type.MostSpecialCommonSuperType(From.ReturnType, To.ReturnType);
                if (itemTy.IsError())
                    $"The range-expression with the types {From.ReturnType} to {To.ReturnType} cannot build a valid contiguous range".Report(pos);
                else if (!itemTy.IsIntegerType() && (!itemTy.OverloadsOperator(OverloadableOperator.Increment, out _) || !itemTy.OverloadsOperator(OverloadableOperator.LessThan, out _))) {
                    $"The type {itemTy} is invalid for contiguous range-elements, since it must be incrementable and less-than comparable. Try using integer-types or overload the operator++ and operator<.".Report(pos);
                }
                retTy = IterableType.Get(itemTy);
            }
            ReturnType = from.ReturnType.IsTop() || to.ReturnType.IsTop() ? Type.Top : retTy.AsNotNullable();
        }
        public override IType ReturnType {
            get;
        }
        public IExpression From => exprs[0];
        public IExpression To => exprs[1];

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(exprs);
        public override IEnumerable<IExpression> GetExpressions() => exprs;
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ContiguousRangeExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                From.Replace(genericActualParameter, curr, parent),
                To.Replace(genericActualParameter, curr, parent)
            );
        }
        static readonly string[] EXPR_NAMES = { "from", "to" };
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            var nwExprs = exprs.Select((x, i) => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    if (nwX.Length != 1) {
                        $"A contiguous-range expression cannot have avariable number of {EXPR_NAMES[i]}-parameters".Report(x.Position.Concat(args.Position));
                        return x;
                    }
                    return nwX[0];
                }
                return x;
            }).ToArray();

            if (changed) {
                expr = new[] {new ContiguousRangeExpression(Position.Concat(args.Position),
                    ReturnType.IsTop() ? null : ReturnType,
                    nwExprs[0],
                    nwExprs[1]
                ) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}