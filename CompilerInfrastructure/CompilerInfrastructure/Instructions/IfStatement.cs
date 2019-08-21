/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

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
    public class IfStatement : StatementImpl {
        readonly IASTNode[] body = new IASTNode[3];
        public IfStatement(Position pos, IExpression cond, IStatement then, IStatement @else = null) : base(pos) {
            body[1] = then ?? "The Then-Case of a conditional statement must not be null".Report(pos, Statement.Error);
            body[2] = @else ?? Statement.Nop;
            body[0] = cond ?? "The condition of a conditional statement must not be null".Report(pos, Expression.Error);
        }
        protected IfStatement(Position pos, IExpression cond, IEnumerable<IStatement> thenElse) : base(pos) {
            using (var it = thenElse.GetEnumerator()) {
                body[1] = it.MoveNext() ? it.Current : "The Then-Case of a conditional statement must not be null".Report(pos, Statement.Error);
                body[2] = it.MoveNext() ? it.Current : Statement.Nop;
            }
            body[0] = cond ?? "The condition of a conditional statement must not be null".Report(pos, Expression.Error);
        }
        public IStatement ThenStatement {
            get => (IStatement)body[1];
        }
        public IStatement ElseStatement {
            get => (IStatement)body[2];
        }
        public IExpression Condition {
            get => (IExpression)body[0];
        }
        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            //return RefEnumerator.Combine(ThenStatement, ElseStatement);
            var ret = RefEnumerator.FromArray(body);
            if (ElseStatement == Statement.Nop)
                ret = ret.Take(2);
            return ret;
        }*/

        public override IEnumerable<IExpression> GetExpressions() {
            yield return Condition;
        }
        public override IEnumerable<IStatement> GetStatements() {
            yield return ThenStatement;
            if (ElseStatement != null)
                yield return ElseStatement;
        }

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new IfStatement(Position,
                Condition.Replace(genericActualParameter, curr, parent),
                ThenStatement.Replace(genericActualParameter, curr, parent),
                ElseStatement?.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Condition.TryReplaceMacroParameters(args, out var nwCond)) {
                changed = true;
                if (nwCond.Length != 1) {
                    "A conditional statement cannot have a variable number of conditions".Report(Condition.Position.Concat(args.Position));
                    nwCond = new[] { Condition };
                }
            }
            else
                nwCond = new[] { Condition };

            var nwThenElse = GetStatements().Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    return nwX;
                }
                return x;
            });

            if (changed) {
                stmt = new IfStatement(Position.Concat(args.Position),
                    nwCond[0],
                    nwThenElse
                );
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
