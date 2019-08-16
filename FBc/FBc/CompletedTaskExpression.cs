using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    class CompletedTaskExpression : ExpressionImpl {
        public CompletedTaskExpression(Position pos, IExpression underlying) : base(pos) {
            Underlying = underlying ?? Expression.Error;
            ReturnType = Underlying.ReturnType.AsAwaitable();
        }

        public override IType ReturnType {
            get;
        }
        public IExpression Underlying {
            get;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() {
            yield return Underlying;
        }


        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new CompletedTaskExpression(Position,
                Underlying.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (Underlying.TryReplaceMacroParameters(args, out var nwUnd)) {
                if (nwUnd.Length != 1) {
                    "A completed-task expression cannot have a variable number of arguments".Report(Position.Concat(args.Position));
                    nwUnd = new[] { Underlying };
                }
                expr = new[] { new CompletedTaskExpression(Position.Concat(args.Position), nwUnd[0]) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
