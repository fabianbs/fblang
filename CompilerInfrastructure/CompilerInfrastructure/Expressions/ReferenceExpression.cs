using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using CompilerInfrastructure.Structure.Types;

namespace CompilerInfrastructure.Expressions {
    public class ReferenceExpression : ExpressionImpl {
        readonly IExpression lvalue;
        private ReferenceExpression(Position pos, IExpression exp) : base(pos) {
            lvalue = exp;
            var retTy = lvalue.ReturnType.UnWrapNatural().AsRef();
            if (lvalue is IndexerExpression)
                retTy = retTy.AsNullable();
            ReturnType = retTy;
        }
        public ReferenceExpression(Position pos, IExpression exp, IMethod parent = null) : this(pos, exp) {
            if (!exp.IsLValue(parent))
                "Cannot retrieve a reference to an rvalue-expression".Report(pos);
        }

        public IExpression Underlying => lvalue;
        public override IType ReturnType { get; }

        public override IEnumerable<IExpression> GetExpressions() {
            yield return lvalue;
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ReferenceExpression(Position, lvalue.Replace(genericActualParameter, curr, parent));
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (lvalue.TryReplaceMacroParameters(args, out var nwLVal)) {
                if (nwLVal.Length != 1)
                    "Cannot reference a variable number of expressions".Report(lvalue.Position);
                else {
                    expr = new IExpression[] { new ReferenceExpression(Position, nwLVal[0]) };
                    return true;
                }
            }
            expr = new IExpression[] { this };
            return false;
        }
    }
}
