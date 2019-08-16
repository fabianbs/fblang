using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class ExpressionStmt : StatementImpl, ISideEffectFulStatement {
        readonly IExpression[] exp = new IExpression[1];
        ValueLazy<bool> _mayHaveSideEffects;
        public ExpressionStmt(Position pos, IExpression exp) : base(pos) {
            this.exp[0] = exp ?? "An expression used as statement must not be null".Report(pos, Expressions.Expression.Error);
            _mayHaveSideEffects = new ValueLazy<bool>(() => Expression.MayHaveSideEffects());
        }

        public IExpression Expression {
            get => exp[0]; set => exp[0] = value;
        }
        public ref ValueLazy<bool> MayHaveSideEffects => ref _mayHaveSideEffects;

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray<IASTNode>(exp);
        }*/

        public override IEnumerable<IExpression> GetExpressions() => exp;
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ExpressionStmt(Position, Expression.Replace(genericActualParameter, curr, parent));
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            if (Expression.TryReplaceMacroParameters(args, out var nwEx)) {
                if (nwEx.Length != 1) {
                    "An expression-statement cannot execute a variable number of expressions".Report(Position.Concat(args.Position));
                    nwEx = exp;
                }
                stmt = new ExpressionStmt(Position.Concat(args.Position), nwEx.First());
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
