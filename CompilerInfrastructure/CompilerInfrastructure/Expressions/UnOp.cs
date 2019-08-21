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
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    public static class UnOpOperatorKindHelper {
        public static bool IsOverloadable(this UnOp.OperatorKind op, out OverloadableOperator ov) {
            switch (op) {
                case UnOp.OperatorKind.NEG:
                    ov = OverloadableOperator.Neg;
                    break;
                case UnOp.OperatorKind.NOT:
                    ov = OverloadableOperator.Not;
                    break;
                case UnOp.OperatorKind.LNOT:
                    ov = OverloadableOperator.LNot;
                    break;
                case UnOp.OperatorKind.INCR_PRE:
                case UnOp.OperatorKind.INCR_POST:
                    ov = OverloadableOperator.Increment;
                    break;
                case UnOp.OperatorKind.DECR_PRE:
                case UnOp.OperatorKind.DECR_POST:
                    ov = OverloadableOperator.Decrement;
                    break;
                case UnOp.OperatorKind.BOOLCAST:
                    ov = OverloadableOperator.Bool;
                    break;
                default:
                    ov = default;
                    return false;
            }
            return true;
        }
    }
    [Serializable]
    public class UnOp : ExpressionImpl, ICompileTimeEvaluable {
        [Serializable]
        public enum OperatorKind {
            NEG,
            NOT,
            LNOT,
            INCR_PRE,
            DECR_PRE,
            INCR_POST,
            DECR_POST,
            UNPACK,
            AWAIT,
            BOOLCAST
        }

        readonly IExpression[] subEx;
        public UnOp(Position pos, IType retTy, OperatorKind op, IExpression underlying) : base(pos) {
            subEx = new[] { underlying ?? "The target-expression for an unary operation must be specified".Report(pos, Expression.Error) };
            ReturnType = InferredReturnType(pos, op, SubExpression.ReturnType, retTy);
            Operator = op;
        }
        public static IType InferredReturnType(Position pos, OperatorKind op, IType subTy, IType retTy = null, ErrorBuffer err = null) {
            if (retTy is null) {
                switch (op) {
                    case OperatorKind.LNOT:
                    case OperatorKind.BOOLCAST:
                        retTy = PrimitiveType.Bool;
                        break;
                    case OperatorKind.AWAIT: {
                        if (subTy.UnWrap() is AggregateType agg) {
                            retTy = agg.ItemType;
                        }
                        else {
                            retTy = subTy.IsError()
                                ? subTy
                                : err.Report($"The type for this await-expression cannot be inferred and therefore must be specified explicitly", pos, Type.Error);
                        }
                        break;
                    }
                    case OperatorKind.UNPACK: {
                        if (subTy.UnWrap() is AggregateType agg) {
                            retTy = agg.ItemType.AsVarArg();
                        }
                        else {
                            retTy = subTy.IsError()
                                ? subTy
                                : err.Report($"The type for this unpack-expression cannot be inferred and therefore must be specified explicitly", pos, Type.Error);
                        }
                        break;
                    }
                    default:
                        retTy = subTy;
                        break;
                }
            }
            return retTy;
        }
        public IExpression SubExpression => subEx[0];
        public OperatorKind Operator {
            get;
        }
        public override IType ReturnType {
            get;
        }
        public bool IsCompileTimeEvaluable {
            get {
                switch (Operator) {
                    case OperatorKind.NEG:
                    case OperatorKind.NOT:
                    case OperatorKind.LNOT:
                    case OperatorKind.BOOLCAST:
                        return SubExpression.IsCompileTimeEvaluable();
                    default:
                        return false;
                }
            }
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(subEx);
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new UnOp(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Operator,
                SubExpression.Replace(genericActualParameter, curr, parent)
            );
        }

        public override IEnumerable<IExpression> GetExpressions() => subEx;
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (SubExpression.TryReplaceMacroParameters(args, out var nwSub)) {
                if (nwSub.Length != 1) {
                    "An unary operation can only have one target expression".Report(SubExpression.Position.Concat(args.Position));
                    nwSub = new[] { SubExpression };
                }
                expr = new[] { new UnOp(Position.Concat(args.Position), ReturnType.IsTop() ? null : ReturnType, Operator, nwSub.First()) };
                return true;
            }
            expr = new[] { this };
            return false;
        }

        public override bool IsLValue(IMethod met) {

            if (Operator == OperatorKind.UNPACK && (SubExpression.ReturnType.UnWrap().IsArray() || SubExpression.ReturnType.UnWrap().IsArraySlice()) && !SubExpression.ReturnType.IsConstant()) {
                return true;
            }
            return base.IsLValue(met);
        }

        public ILiteral Evaluate(ref EvaluationContext context) {
            if (!IsCompileTimeEvaluable || !SubExpression.TryEvaluate(ref context, out var sub))
                return null;
            switch (Operator) {
                case OperatorKind.NEG:
                    return Literal.WithRealType(sub, x => -x, x => ulong.MaxValue - x + 1, x => -x, x => -x);
                case OperatorKind.NOT:
                    return Literal.WithRealType(sub, x => ~x, x => ~x, x => ~x, null);
                case OperatorKind.LNOT:
                    return Literal.Bool(!sub.IsTrue());
                case OperatorKind.BOOLCAST:
                    return Literal.Bool(sub.IsTrue());
                default:
                    return null;
            }
        }
    }
    public static class UnOpOperatorHelper {
        public static bool IsIncDec(this UnOp.OperatorKind op) {
            return op == UnOp.OperatorKind.INCR_POST || op == UnOp.OperatorKind.INCR_PRE || op == UnOp.OperatorKind.DECR_POST || op == UnOp.OperatorKind.DECR_PRE;
        }
    }
}
