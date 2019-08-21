/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class WhileLoop : StatementImpl {
        readonly IASTNode[] content = new IASTNode[2];

        public WhileLoop(Position pos, IExpression cond, IStatement body, bool isHeadControlled = true) : base(pos) {
            content[0] = cond ?? Literal.True;// empty while-condition may be allowed
            content[1] = body ?? Statement.Nop;
            IsHeadControlled = isHeadControlled;
        }

        public IExpression Condition => (IExpression)content[0];
        public IStatement Body {
            get {
                return (IStatement)content[1];
            }
            set {
                content[1] = value ?? Statement.Nop;
            }
        }

        public bool IsHeadControlled {
            get;
        }
       /* public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray(content);
        }
        */
        public override IEnumerable<IExpression> GetExpressions() {
            yield return Condition;
        }
        public override IEnumerable<IStatement> GetStatements() {
            yield return Body;
        }


        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new WhileLoop(Position,
                Condition.Replace(genericActualParameter, curr, parent),
                Body.Replace(genericActualParameter, curr, parent),
                IsHeadControlled
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Condition.TryReplaceMacroParameters(args, out var nwCond)) {
                changed = true;
                if (nwCond.Length != 1) {
                    $"A {(IsHeadControlled ? "" : "do..")}while-loop cannot have a variable number of loop-conditions".Report(Condition.Position.Concat(args.Position));
                    nwCond = new[] { Condition };
                }
            }
            else
                nwCond = new[] { Condition };

            if (Body.TryReplaceMacroParameters(args, out var nwBody))
                changed = true;
            else
                nwBody = Body;

            if (changed) {
                stmt = new WhileLoop(Position.Concat(args.Position),
                    nwCond[0],
                    nwBody,
                    IsHeadControlled
                );
                return true;
            }
            stmt = this;
            return false;
        }
    }
    [Serializable]
    public class DoWhileLoop : WhileLoop {
        public DoWhileLoop(Position pos, IExpression cond, IStatement body) : base(pos, cond, body, false) {
        }
    }
}
