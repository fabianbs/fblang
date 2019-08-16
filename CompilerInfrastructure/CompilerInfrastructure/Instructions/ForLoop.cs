using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class ForLoop : StatementImpl, IRangeScope {
        readonly IASTNode[] content = new IASTNode[4];

        public ForLoop(Position pos, IStatement init, IExpression cond, IStatement incr, IStatement body, IContext headerScope) : base(pos) {
            content[0] = init ?? Statement.Nop;
            content[1] = cond ?? Literal.True;// empty for-condition may be allowed
            content[2] = incr ?? Statement.Nop;
            content[3] = body ?? Statement.Nop;
            Context = headerScope;
        }

        public IStatement Initialization => (IStatement)content[0];
        public IExpression Condition => (IExpression)content[1];
        public IStatement Increment => (IStatement)content[2];
        public IStatement Body {
            get {
                return (IStatement)content[3];
            }
            set {
                content[3] = value ?? Statement.Nop;
            }
        }

        public IContext Context {
            get;
        }

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray(content);
        }
        */
        public override IEnumerable<IExpression> GetExpressions() {
            yield return Condition;
        }
        public override IEnumerable<IStatement> GetStatements() {
            yield return Initialization;
            yield return Body;
            yield return Increment;
        }

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ForLoop(Position,
                Initialization.Replace(genericActualParameter, curr, parent),
                Condition.Replace(genericActualParameter, curr, parent),
                Increment.Replace(genericActualParameter, curr, parent),
                Body.Replace(genericActualParameter, curr, parent),
                Context.Replace(genericActualParameter, curr, parent)
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Condition.TryReplaceMacroParameters(args, out var nwCond)) {
                changed = true;
                if (nwCond.Length != 1) {
                    "A for-loop cannot have a variable number of loop-conditions".Report(Condition.Position.Concat(args.Position));
                    nwCond = new[] { Condition };
                }
            }
            else
                nwCond = new[] { Condition };

            var nwInitBodyIncr = GetStatements().Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    return nwX;
                }
                return x;
            }).ToArray();

            if (changed) {
                stmt = new ForLoop(Position.Concat(args.Position),
                    nwInitBodyIncr[0],
                    nwCond[0],
                    nwInitBodyIncr[1],
                    nwInitBodyIncr[2],
                    Context
                );
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
