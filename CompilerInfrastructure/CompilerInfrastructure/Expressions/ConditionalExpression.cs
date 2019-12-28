/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    using Structure.Types;

    [Serializable]
    public class ConditionalExpression : ExpressionImpl, ICompileTimeEvaluable {
        readonly IExpression[] content;

        public ConditionalExpression(Position pos, IType retTy, IExpression cond, IExpression then, IExpression @else) : base(pos) {
            content = new[] {
                cond??"The condition for the conditional expression must not be null".Report(pos, Expression.Error),
                then??"The then-expression for the conditional expression must not be null".Report(pos, Expression.Error),
                @else??"The else-expression for the conditional expression must not be null".Report(pos, Expression.Error)
            };
            if (retTy is null) {
                ReturnType = Type.MostSpecialCommonSuperType(ThenExpression.ReturnType, ElseExpression.ReturnType);
                if (ReturnType.IsError() && !ThenExpression.ReturnType.IsError() && !ElseExpression.ReturnType.IsError())
                    "The type for the conditional expression cannot be inferred and must be specified explicitly".Report(pos);
            }
            else
                ReturnType = retTy;
        }
        public IExpression Condition => content[0];
        public IExpression ThenExpression => content[1];
        public IExpression ElseExpression => content[2];
        public override IType ReturnType {
            get;
        }
        bool? compileTimeEvaluable=null;
        public bool IsCompileTimeEvaluable {
            get {
                if (compileTimeEvaluable is null) {
                    compileTimeEvaluable = Condition.IsCompileTimeEvaluable() && ThenExpression.IsCompileTimeEvaluable() && ElseExpression.IsCompileTimeEvaluable();
                }
                return compileTimeEvaluable.Value;
            }
        }

        public ILiteral Evaluate(ref EvaluationContext context) {
            if (!Condition.TryEvaluate(ref context, out var cond))
                return null;
            if (cond.IsTrue())
                return ThenExpression.Evaluate(ref context);
            else
                return ElseExpression.Evaluate(ref context);
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        public override IEnumerable<IExpression> GetExpressions() => content;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ConditionalExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Condition.Replace(genericActualParameter, curr, parent),
                ThenExpression.Replace(genericActualParameter, curr, parent),
                ElseExpression.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            if (Condition.TryReplaceMacroParameters(args, out var nwCond)) {
                if (nwCond.Length != 1) {
                    "The conditional expression can only have one condition".Report(Position.Concat(args.Position));
                }
                changed = true;
            }
            else
                nwCond = new[] { Condition };
            if (ThenExpression.TryReplaceMacroParameters(args, out var nwThen)) {
                if (nwThen.Length != 1) {
                    "The conditional expression can only have one then-case expression".Report(Position.Concat(args.Position));
                }
                changed = true;
            }
            else
                nwThen = new[] { ThenExpression };
            if (ElseExpression.TryReplaceMacroParameters(args, out var nwElse)) {
                if (nwElse.Length != 1) {
                    "The conditional expression can only have one else-case expression".Report(Position.Concat(args.Position));
                }
                changed = true;
            }
            else
                nwElse = new[] { ElseExpression };

            if (changed) {
                expr = new[] { new ConditionalExpression(Position.Concat(args.Position),
                    ReturnType,
                    nwCond.FirstOrDefault(),
                    nwThen.FirstOrDefault(),
                    nwElse.FirstOrDefault()
                )};
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
