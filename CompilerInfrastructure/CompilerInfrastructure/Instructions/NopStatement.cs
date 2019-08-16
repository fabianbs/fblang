using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class NopStatement : StatementImpl {
        NopStatement() : base(default) {
        }
        public static NopStatement Instance {
            get;
        } = new NopStatement();

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public override IStatement Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            stmt = this;
            return false;
        }
    }
}
