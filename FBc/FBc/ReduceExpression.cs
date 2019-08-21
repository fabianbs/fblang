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
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FBc {
    using static CompilerInfrastructure.Contexts.SimpleMethodContext;
    [Serializable]
    public class ReduceExpression : ExpressionImpl {
        IMethod reductionFunction;
        BinOp.OperatorKind reductionOperator;
        public IExpression DataSource {
            get;
        }
        public IExpression Seed {
            get;
        }
        public BinOp.OperatorKind ReductionOperator => reductionOperator;
        public IMethod ReductionFunction => reductionFunction;
        public bool HasOperator {
            get => reductionFunction is null;
        }
        public override IType ReturnType {
            get;
        }
        public IType ItemType {
            get;
        }
        public bool IsConcurrent {
            get;
        }
        public ReduceExpression(Position pos, IExpression dataSource, IExpression seed, BinOp.OperatorKind op, IMethod met, bool isConcurrent) : base(pos) {
            IsConcurrent = isConcurrent;
            DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            IType itemTy;
            if (dataSource.ReturnType.UnWrap() is AggregateType agg) {
                itemTy = agg.ItemType;
                if (isConcurrent && !agg.IsArray() && !agg.IsArraySlice()) {
                    "The concurrent reduce-operator can only reduce arrays or slices".Report(pos);
                }
            }
            else {
                itemTy = $"The reduce-operator can only reduce iterable objects, not {dataSource.ReturnType.Signature}".ReportTypeError(pos);
            }
            ItemType = itemTy;
            reductionFunction = met;
            reductionOperator = op;
            ReturnType = met is null ? itemTy : met.ReturnType;
            if (isConcurrent)
                ReturnType = ReturnType.AsAwaitable();

            if (seed is null) {
                // initialize seed
                if (met is null)
                    Seed = DefaultValue(pos, itemTy, op);
                else
                    Seed = "The reduction-seed cannot be inferred for custom reductions and must be specified explicitly".Report(pos, Expression.Error);
            }
            else {
                Seed = seed;
                if (seed.ReturnType != ReturnType) {
                    $"The reduction-seed must have the same type {ReturnType.Signature} as the reduction-returntype".Report(pos);
                }
            }

            if (met is null) {
                // operator(-overload)
                GetOperatorOverload(pos, itemTy, seed?.ReturnType, op, out reductionFunction);
            }
            //no 'else if', since the 'met is null' body can assign a value to met
            if (met != null && !met.IsError()) {

                if (met.Arguments.Length != 2) {
                    "The reduction-function must have exactly two parameter".Report(pos);
                }
                else if (met.Arguments.Any(x => x.Type.IsByRef())) {
                    "The arguments of the reduction-function must be passed by value".Report(pos);
                }
                else if (met.Arguments[0].Type != met.ReturnType) {
                    "The return-type of the reduction-function must match the type of the first parameter".Report(pos);
                }
                else if (!met.IsOperatorOverload() && !met.IsStatic()) {
                    "The reduction operator must be a global, or static function, or one of the operators '+', '-', '*', '/', '%', '&', '|' or '^'".Report(pos);
                }
            }


        }
        private ReduceExpression(ReduceExpression other, GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) : base(other.Position) {
            DataSource = other.DataSource.Replace(genericActualParameter, curr, parent);
            Seed = other.Seed.Replace(genericActualParameter, curr, parent);
            reductionFunction = other.reductionFunction?.Replace(genericActualParameter, curr, parent);
            reductionOperator = other.reductionOperator;
            ReturnType = other.ReturnType.Replace(genericActualParameter, curr, parent);
            ItemType = other.ItemType.Replace(genericActualParameter, curr, parent);
        }
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() {
            yield return DataSource;
            yield return Seed;
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ReduceExpression(this, genericActualParameter, curr, parent);
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            throw new NotImplementedException();
        }

        static bool GetOperatorOverload(Position pos, IType itemTy, IType seedTy, BinOp.OperatorKind op, out IMethod met) {

            if (itemTy is PrimitiveType prim) {
                met = null;
                if (op.IsBitwise() && !prim.IsFloatingPoint || !op.IsBitwise())
                    return false;
                return $"The bitwise operator {op} cannot be applied to the floating-point type {itemTy.Signature}".Report(pos, false);
            }
            else if (op.IsOverloadable(out var ovOp)) {
                // the reduction is left-recursive
                var err = new ErrorBuffer();
                met = null;
                if (seedTy != null && seedTy.OverloadsOperator(ovOp, out var mets, VisibleMembers.Instance)) {
                    met = FBSemantics.Instance.BestFittingMethod(pos, mets, new[] { itemTy }, seedTy, err);
                }
                if (met is null || met.IsError()) {

                    itemTy.OverloadsOperator(ovOp, out var itemOv, VisibleMembers.Static);
                    mets = itemOv ?? Enumerable.Empty<IDeclaredMethod>();
                    if (seedTy != null) {
                        seedTy.OverloadsOperator(ovOp, out var seedOv, VisibleMembers.Static);
                        mets = Enumerable.Concat(seedOv, itemOv);
                    }
                    met = FBSemantics.Instance.BestFittingMethod(pos, mets, new[] { seedTy, itemTy }, seedTy, err);
                }
                if (met is null) {
                    return $"The type {itemTy} does not overload the operator {op}".Report(pos, false);
                }
                else {
                    err.Flush();
                    return false;
                }
            }
            else {
                met = null;
                return $"The non-overloadable operator {op} cannot be applied to the type {itemTy.Signature}".Report(pos, false);
            }
        }
        static IExpression DefaultValue(Position pos, IType itemTy, BinOp.OperatorKind op) {
            if (itemTy is PrimitiveType prim) {

                switch (op) {
                    case BinOp.OperatorKind.OR:
                    case BinOp.OperatorKind.XOR:
                    case BinOp.OperatorKind.ADD:
                    case BinOp.OperatorKind.SUB: {
                        if (!Literal.TryGetZero(prim, out var ret))
                            return $"The null-value could not be inferred for the type {prim.Signature}. Try specifying the reduction-seed explicitly".Report(pos, Expression.Error);
                        return ret;
                    }
                    case BinOp.OperatorKind.AND:
                    case BinOp.OperatorKind.MUL:
                    case BinOp.OperatorKind.DIV:
                    case BinOp.OperatorKind.REM: {
                        if (!Literal.TryGetZero(prim, out var ret))
                            return $"The one-value could not be inferred for the type {prim.Signature}. Try specifying the reduction-seed explicitly".Report(pos, Expression.Error);
                        return ret;
                    }
                    default:
                        return $"The default-value of the type {itemTy.Signature} together with the operator {op} could not be inferred. Try specifying the reduction-seed explicitly".Report(pos, Expression.Error);
                }
            }
            else if (itemTy.IsError()) {
                // do not report the error twice
                return Expression.Error;
            }
            else {
                return $"The reduction-seed could not be inferred for the operator {op} applied on type {itemTy.Signature}. Try specifying the reduction-seed explicitly".Report(pos, Expression.Error);
            }
        }
    }
}
