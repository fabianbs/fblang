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
    public class NopExpression : ExpressionImpl {
        public NopExpression(Position pos, IType retTy):base(pos) {
            ReturnType = retTy ?? PrimitiveType.Void;
        }
        public override IType ReturnType { get; }

        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return this;
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }
    }
}
