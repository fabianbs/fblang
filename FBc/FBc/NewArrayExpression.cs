using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    [Serializable]
    class NewArrayExpression : ExpressionImpl, INotNullableExpression {
        readonly IExpression[] dim;
        public NewArrayExpression(Position pos, IType itemType, IExpression count) : base(pos) {
            ReturnType = (itemType ?? Type.Error).AsArray();
            dim = new[] { count ?? Expression.Error };
        }
        public override IType ReturnType {
            get;
        }
        public IExpression Length => dim[0];

        public bool IsNotNullable => true;

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(dim);
        public override IEnumerable<IExpression> GetExpressions() => dim;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new NewArrayExpression(Position,
                (ReturnType as IWrapperType).ItemType.Replace(genericActualParameter, curr, parent),
                Length.Replace(genericActualParameter, curr, parent)
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (Length.TryReplaceMacroParameters(args, out var nwLen)) {
                if (nwLen.Length!=1) {
                    "Instantiating multidimensional arrays is currently not supported yet".Report(Length.Position.Concat(args.Position));
                    nwLen = dim;
                }
                expr = new[] { new NewArrayExpression(Position.Concat(args.Position),
                    (ReturnType as AggregateType).ItemType,
                    nwLen[0]
                )};
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
