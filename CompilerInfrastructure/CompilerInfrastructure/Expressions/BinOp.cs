/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using static CompilerInfrastructure.Expressions.BinOp;

namespace CompilerInfrastructure.Expressions {
    using Type = Structure.Types.Type;

    [Serializable]
    public class BinOp : ExpressionImpl, ISideEffectFulExpression, ICompileTimeEvaluable {
        [Serializable]
        public enum OperatorKind {
            ASSIGN_NEW, ASSIGN_OLD,
            LOR, LAND,
            OR,
            XOR,
            EQ, NEQ,
            LT, LE, GE, GT,
            LSHIFT, SRSHIFT, URSHIFT,
            AND,
            ADD, SUB,
            MUL, DIV, REM
        }
        bool? compileTimeEvaluable=null;
        readonly IExpression[] content = new IExpression[2];
        /// <summary>
        /// Initializes a new instance of the <see cref="BinOp"/> class.
        /// </summary>
        /// <param name="pos">The position in the source-code.</param>
        /// <param name="retTy">The return type; will be inferred if <code>null</code> is specified.</param>
        /// <param name="lhs">The left hand side; must not be null.</param>
        /// <param name="op">The operator.</param>
        /// <param name="rhs">The right hand side; must not be null.</param>
        public BinOp(Position pos, IType retTy, IExpression lhs, OperatorKind op, IExpression rhs) : base(pos) {
            content[0] = lhs ?? "The left-hand-side of a binary expression must not be null".Report(pos, Expression.Error);
            content[1] = rhs ?? "The right-hand-side of a binary expression must not be null".Report(pos, Expression.Error);
            Operator = op;

            ReturnType = InferredReturnType(pos, op, Left.ReturnType, Right.ReturnType, retTy);
        }
        public static IType InferredReturnType(Position pos, OperatorKind op, IType lhsTy, IType rhsTy, IType retTy = null) {
            if (retTy is null) {
                switch (op) {
                    case OperatorKind.ASSIGN_NEW:
                        retTy = lhsTy.UnWrapNatural();
                        break;
                    case OperatorKind.ASSIGN_OLD:
                        retTy = rhsTy.UnWrapNatural();
                        break;
                    case OperatorKind.LOR:
                    case OperatorKind.LAND:
                    case OperatorKind.LT:
                    case OperatorKind.LE:
                    case OperatorKind.GE:
                    case OperatorKind.GT:
                    case OperatorKind.EQ:
                    case OperatorKind.NEQ:
                        retTy = PrimitiveType.Bool;
                        break;
                    default:
                        retTy = Type.MostSpecialCommonSuperType(lhsTy, rhsTy);
                        break;
                }
            }
            if (op.IsAssignment() && lhsTy.IsByConstRef()) {
                "Cannot assign to a const reference".Report(pos);
            }
            return retTy;
        }
        public IExpression Left {
            get => content[0];
        }
        public OperatorKind Operator {
            get;
        }
        public IExpression Right {
            get => content[1];
        }

        public override IType ReturnType {
            get;
        }

        public bool MayHaveSideEffects {
            get => Operator.IsAssignment() && Left.ReadsMutableData();
        }

        public bool IsAssignment => Operator.IsAssignment();
        public bool IsRefReassignment => IsAssignment && Right is ReferenceExpression;
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BinOp(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Left.Replace(genericActualParameter, curr, parent),
                Operator,
                Right.Replace(genericActualParameter, curr, parent)
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            if (Left.TryReplaceMacroParameters(args, out var nwLeft)) {
                if (nwLeft.Length != 1) {
                    "A binary operation can only have one single left hand side".Report(Left.Position.Concat(args.Position));
                    nwLeft = new[] { Left };
                }
                changed = true;
            }
            else
                nwLeft = new[] { Left };
            if (Right.TryReplaceMacroParameters(args, out var nwRight)) {
                if (nwRight.Length != 1) {
                    "A binary operation can only have one single right hand side".Report(Right.Position.Concat(args.Position));
                    nwRight = new[] { Right };
                }
                changed = true;
            }
            else
                nwRight = new[] { Right };
            if (changed) {
                /*expr = new[]{ new BinOp(Position.Concat(args.Position),
                    ReturnType,
                    nwLeft.FirstOrDefault(),
                    Operator,
                    nwRight.FirstOrDefault()
                ) };*/
                expr = new[] { args.Semantics.CreateBinOp(Position.Concat(args.Position), ReturnType.IsTop() ? null : ReturnType, nwLeft[0], Operator, nwRight[0]) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
        public override IEnumerable<IExpression> GetExpressions() => content;
        public bool IsCompileTimeEvaluable {
            get {
                if (compileTimeEvaluable is null) {
                    // forbid assignments, since they have side-effects
                    compileTimeEvaluable = !Operator.IsAssignment() && Left.IsCompileTimeEvaluable() && Right.IsCompileTimeEvaluable();
                }
                return compileTimeEvaluable.Value;
            }
        }
        public ILiteral Evaluate(ref EvaluationContext context) {
            if (context.TryGetFact(this, out var ret))
                return ret;
            ILiteral _lhs;
            ILiteral _rhs;
            if (Operator != OperatorKind.LOR) {
                _lhs = Left.Evaluate(ref context);
                _rhs = Right.Evaluate(ref context);
            }
            else {
                _lhs = _rhs = null;
            }
            IALiteral lhs, rhs;
            if ((!(_lhs is IALiteral) || !(_rhs is IALiteral)) && Operator != OperatorKind.LOR)// generic literalparameters which are not instantiated, cannot be operand of a calculation
                return null;
            else {
                lhs = _lhs as IALiteral;
                rhs = _rhs as IALiteral;
            }
            switch (Operator) {
                case OperatorKind.LOR: {
                    var lhsCtx = context.Clone();
                    var rhsCtx = context.Clone();
                    _lhs = Left.Evaluate(ref lhsCtx);
                    _rhs = Right.Evaluate(ref rhsCtx);
                    if (!(_lhs is IALiteral llhs) || !(_rhs is IALiteral rrhs))// generic literalparameters which are not instantiated, cannot be operand of a calculation
                        return null;
                    return Literal.Bool(llhs.IsTrue() || rrhs.IsTrue());
                }
                case OperatorKind.LAND:
                    return Literal.Bool(lhs.IsTrue() && rhs.IsTrue());
                case OperatorKind.XOR:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => {
                        if (lhs.TryConvertToLong(out var slhs) && rhs.TryConvertToLong(out var srhs))
                            return new LongLiteral(lhs.Position, (slhs ^ srhs) & ((1L << lhs.BitWidth()) - 1)).ValueCast(lhs.ReturnType);
                        else if (lhs.TryConvertToULong(out var ulhs) && rhs.TryConvertToULong(out var urhs))
                            return new ULongLiteral(lhs.Position, (ulhs ^ urhs) & ((1uL << lhs.BitWidth()) - 1)).ValueCast(lhs.ReturnType);
                        return null;
                    });
                case OperatorKind.EQ:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => Literal.Bool(x.ValueEquals(y)));
                case OperatorKind.NEQ:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => Literal.Bool(!x.ValueEquals(y)));
                case OperatorKind.LT:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x < y, (x, y) => x < y, (x, y) => x < y, (x, y) => x < y);
                case OperatorKind.LE:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x <= y, (x, y) => x <= y, (x, y) => x <= y, (x, y) => x <= y);
                case OperatorKind.GE:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x >= y, (x, y) => x >= y, (x, y) => x >= y, (x, y) => x >= y);
                case OperatorKind.GT:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x > y, (x, y) => x > y, (x, y) => x > y, (x, y) => x > y);
                case OperatorKind.AND:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x & y, (x, y) => x & y, (x, y) => x & y, null);
                case OperatorKind.OR:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x | y, (x, y) => x | y, (x, y) => x | y, null);
                case OperatorKind.LSHIFT:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x << (int) y, (x, y) => x << (int) y, (x, y) => x << (int) y, null);
                case OperatorKind.SRSHIFT:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x >> (int) y, (x, y) => x >> (int) y, (x, y) => x >> (int) y, null);
                case OperatorKind.URSHIFT:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x.LogicalRightShift((int) y), (x, y) => x >> (int) y, (x, y) => (x.Sign < 0 ? new BigInteger(x.ToByteArray().Append<byte>(0).ToArray()) : x) >> (int) y, null);
                case OperatorKind.ADD:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x + y, (x, y) => x + y, (x, y) => x + y, (x, y) => x + y);
                case OperatorKind.SUB:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x - y, (x, y) => x - y, (x, y) => x - y, (x, y) => x - y);
                case OperatorKind.MUL:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x * y, (x, y) => x * y, (x, y) => x * y, (x, y) => x * y);
                case OperatorKind.DIV:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x / y, (x, y) => x / y, (x, y) => x / y, (x, y) => x / y);
                case OperatorKind.REM:
                    return Literal.WithCommonType(lhs, rhs, (x, y) => x % y, (x, y) => x % y, (x, y) => x % y, (x, y) => x % y);
                default:
                    return null;
            }
        }
        public override string ToString() => Left + " " + BinOpOperatorKindHelper.ToString(Operator) + " " + Right;
    }
    public static class BinOpOperatorKindHelper {
        public static bool IsOverloadable(this OperatorKind op, out OverloadableOperator ov) {
            switch (op) {
                case OperatorKind.OR:
                    ov = OverloadableOperator.Or;
                    break;
                case OperatorKind.XOR:
                    ov = OverloadableOperator.Xor;
                    break;
                case OperatorKind.EQ:
                    ov = OverloadableOperator.Equal;
                    break;
                case OperatorKind.NEQ:
                    ov = OverloadableOperator.Unequal;
                    break;
                case OperatorKind.LT:
                    ov = OverloadableOperator.LessThan;
                    break;
                case OperatorKind.LE:
                    ov = OverloadableOperator.LessThanOrEqual;
                    break;
                case OperatorKind.GE:
                    ov = OverloadableOperator.GreaterThanOrEqual;
                    break;
                case OperatorKind.GT:
                    ov = OverloadableOperator.GreaterThan;
                    break;
                case OperatorKind.LSHIFT:
                    ov = OverloadableOperator.LShift;
                    break;
                case OperatorKind.SRSHIFT:
                    ov = OverloadableOperator.SRShift;
                    break;
                case OperatorKind.URSHIFT:
                    ov = OverloadableOperator.URShift;
                    break;
                case OperatorKind.AND:
                    ov = OverloadableOperator.And;
                    break;
                case OperatorKind.ADD:
                    ov = OverloadableOperator.Add;
                    break;
                case OperatorKind.SUB:
                    ov = OverloadableOperator.Sub;
                    break;
                case OperatorKind.MUL:
                    ov = OverloadableOperator.Mul;
                    break;
                case OperatorKind.DIV:
                    ov = OverloadableOperator.Div;
                    break;
                case OperatorKind.REM:
                    ov = OverloadableOperator.Rem;
                    break;
                default:
                    ov = default;
                    return false;
            }
            return true;
        }
        public static bool IsComparison(this OperatorKind op) {
            switch (op) {
                case OperatorKind.LT:
                case OperatorKind.LE:
                case OperatorKind.GE:
                case OperatorKind.GT:
                case OperatorKind.EQ:
                case OperatorKind.NEQ:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsBitwise(this OperatorKind op) {
            return op == OperatorKind.AND || op == OperatorKind.OR || op == OperatorKind.XOR;
        }
        public static bool IsBooleanOperator(this OperatorKind op) {
            return op == OperatorKind.LAND || op == OperatorKind.LOR;
        }
        public static bool IsShift(this OperatorKind op) {
            return op == OperatorKind.LSHIFT || op == OperatorKind.SRSHIFT || op == OperatorKind.URSHIFT;
        }
        public static bool IsAssignment(this OperatorKind op) {
            return op == OperatorKind.ASSIGN_NEW || op == OperatorKind.ASSIGN_OLD;
        }
        public static string ToString(this OperatorKind op) => op switch
        {
            OperatorKind.ASSIGN_NEW => "=",
            OperatorKind.ASSIGN_OLD => ":=",
            OperatorKind.LOR => "||",
            OperatorKind.LAND => "&&",
            OperatorKind.OR => "|",
            OperatorKind.XOR => "^",
            OperatorKind.EQ => "==",
            OperatorKind.NEQ => "!=",
            OperatorKind.LT => "<",
            OperatorKind.LE => "<=",
            OperatorKind.GE => ">=",
            OperatorKind.GT => ">",
            OperatorKind.LSHIFT => "<<",
            OperatorKind.SRSHIFT => ">>",
            OperatorKind.URSHIFT => ">>>",
            OperatorKind.AND => "&",
            OperatorKind.ADD => "+",
            OperatorKind.SUB => "-",
            OperatorKind.MUL => "*",
            OperatorKind.DIV => "/",
            OperatorKind.REM => "%",
            _ => throw new ArgumentException(),
        };

    }
}
