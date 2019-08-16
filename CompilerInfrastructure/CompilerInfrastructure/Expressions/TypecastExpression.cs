using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class TypecastExpression : ExpressionImpl, ICompileTimeEvaluable {
        readonly IExpression[] sub;
        public TypecastExpression(Position pos, IExpression subEx, IType retTy, bool throwOnError = true) : base(pos) {
            ThrowOnError = throwOnError;
            sub = new[] { subEx ?? "The target-type of a typecast expression must always be specified explicitly".Report(pos, Expression.Error) };
            ReturnType = retTy ?? SubExpression.ReturnType;
            if (!SubExpression.IsError() && !ReturnType.IsTop() && !(Type.IsAssignable(ReturnType, SubExpression.ReturnType) || Type.IsAssignable(SubExpression.ReturnType, ReturnType))) {
                $"An expression of type {SubExpression.ReturnType.Signature} cannot be cast to {ReturnType.Signature}".Report(pos);
            }
        }
        public bool ThrowOnError {
            get;
        }
        public override IType ReturnType {
            get;
        }
        public IExpression SubExpression => sub[0];

        public bool IsCompileTimeEvaluable => ReturnType.IsPrimitive() && SubExpression.IsCompileTimeEvaluable();

        public ILiteral Evaluate(ref EvaluationContext context) {
            if (!IsCompileTimeEvaluable)
                return null;
            return SubExpression.Evaluate(ref context).ValueCast(ReturnType);
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(sub);
        public override IEnumerable<IExpression> GetExpressions() => sub;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new TypecastExpression(Position,
                SubExpression.Replace(genericActualParameter, curr, parent),
                ReturnType.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (SubExpression.TryReplaceMacroParameters(args, out var nwSub)) {
                if (nwSub.Length != 1) {
                    "A typecast cannot be applied to a variable number of arguments".Report(SubExpression.Position.Concat(args.Position));
                    nwSub = sub;
                }
                expr = new[] { new TypecastExpression(Position.Concat(args.Position), nwSub[0], ReturnType) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
