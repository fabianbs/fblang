using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    public interface IExpression : ISourceElement, IReplaceableStructureElement<IExpression>, IExpressionContainer {
        IType ReturnType {
            get;
        }
        bool IsLValue(IMethod met = null);
        ISet<IVariable> NotNullableVars {
            get; set;
        }
        //IExpression Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        bool TryReplaceMacroParameters(MacroCallParameters args, out IExpression[] expr);
    }
    public interface INotNullableExpression : IExpression {
        bool IsNotNullable {
            get;
        }
    }
    public interface ISideEffectFulExpression : IExpression {
        bool MayHaveSideEffects {
            get;
        }
    }
    public interface IMutableDataReadingExpression : IExpression {
        bool ReadsMutableData {
            get;
        }
    }
    public interface ICompileTimeEvaluable : IExpression {
        bool IsCompileTimeEvaluable { get; }
        ILiteral Evaluate(ref EvaluationContext context);
    }

    public static class ExpressionHelper {
        public static IType MinimalType(this IExpression ex) {
            if (ex is ILiteral lit)
                return lit.MinimalType;
            return ex.ReturnType;
        }
        public static bool IsNullable(this IExpression ex) {
            if (ex is INotNullableExpression nne)
                return !nne.IsNotNullable;
            else
                return !ex.ReturnType.IsNotNullable();
        }
        public static bool MayHaveSideEffects(this IExpression ex) {
            return ex is ISideEffectFulExpression see && see.MayHaveSideEffects;
        }
        public static bool ReadsMutableData(this IExpression ex) {
            return ex is IMutableDataReadingExpression mde && mde.ReadsMutableData;
        }
        public static bool IsError(this IExpression expr) {
            return expr == Expression.Error;
        }
        public static bool IsUnpack(this IExpression expr) {
            return expr is UnOp uo && uo.Operator == UnOp.OperatorKind.UNPACK;
        }
        public static bool IsUnpack(this IExpression expr, out IExpression subEx) {
            if (expr is UnOp uo && uo.Operator == UnOp.OperatorKind.UNPACK) {
                subEx = uo.SubExpression;
                return true;
            }
            subEx = default;
            return false;
        }
        public static bool IsUnpack(this IExpression expr, out IType itemType) {
            if (expr is UnOp uo && uo.Operator == UnOp.OperatorKind.UNPACK) {
                if (expr.ReturnType.UnWrap() is AggregateType agg) {
                    itemType = agg.ItemType;
                }
                else {
                    itemType = $"The item-type of the aggregate-type {expr.ReturnType} cannot be inferred".ReportTypeError(expr.Position);
                }
                return true;
            }
            itemType = default;
            return false;
        }
        public static bool IsNotNullable(this IExpression expr) {
            if (expr is VariableAccessExpression vre)
                return vre.Variable.IsNotNull() || (expr.NotNullableVars != null && expr.NotNullableVars.Contains(vre.Variable));
            else if (expr is INotNullableExpression nne && nne.IsNotNullable)
                return true;
            else if (expr == Literal.Null)
                return false;

            return expr.ReturnType.IsNotNullable() || expr is ThisExpression || expr is BaseExpression;
        }
        public static bool IsCompileTimeEvaluable(this IExpression expr) {
            return expr is ICompileTimeEvaluable cte && !cte.ReturnType.IsError() && cte.IsCompileTimeEvaluable;
        }
        public static bool TryEvaluate(this IExpression expr, ref EvaluationContext context, out ILiteral result) {
            if (expr is ICompileTimeEvaluable cte) {
                result = cte.Evaluate(ref context);
                return result != null;
            }
            result = null;
            return false;
        }
        public static bool TryEvaluate(this IExpression expr, out ILiteral result) {
            EvaluationContext context = default;
            return TryEvaluate(expr, ref context, out result);
        }
        public static ILiteral Evaluate(this IExpression expr, ref EvaluationContext context) {
            if (expr is ICompileTimeEvaluable cte) {
                return cte.Evaluate(ref context);
            }
            return null;
        }
        public static ILiteral Evaluate(this IExpression expr) {
            EvaluationContext context = default;
            return Evaluate(expr, ref context);
        }
    }
    public static class Expression {
        public static IExpression Error {
            get => ErrorExpression.Instance;
        }
    }
    [Serializable]
    public abstract class ExpressionImpl : IExpression {
        readonly Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IExpression> genericCache
            = new Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IExpression>();
        bool needToReplaceMacroParameters = true;
        public ExpressionImpl(Position pos) {
            Position = pos;
        }

        public abstract IType ReturnType {
            get;
        }
        public virtual Position Position {
            get;
        }
        public virtual bool IsLValue(IMethod met) => ReturnType.IsByRef() && !ReturnType.IsConstant();

        public ISet<IVariable> NotNullableVars {
            get; set;
        }

        //public abstract IRefEnumerator<IASTNode> GetEnumerator();
        protected abstract IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual IExpression Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                ret = ReplaceImpl(genericActualParameter, curr, parent);
            }
            return ret;
        }
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public abstract IEnumerable<IExpression> GetExpressions();
        public virtual bool TryReplaceMacroParameters(MacroCallParameters args, out IExpression[] expr) {
            if (needToReplaceMacroParameters) {
                bool ret = TryReplaceMacroParametersImpl(args, out expr);
                if (!ret)
                    needToReplaceMacroParameters = false;
                return ret;
            }
            expr = new[] { this };
            return false;
        }

        protected abstract bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr);
    }
}
