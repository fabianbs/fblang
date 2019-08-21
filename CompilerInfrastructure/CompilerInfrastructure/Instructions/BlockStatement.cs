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
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class BlockStatement : StatementImpl, IRangeScope {
        
        readonly IStatement[] instructions;
        public BlockStatement(Position pos, IStatement[] stmts, IContext innerScope) : base(pos) {
            instructions = stmts ?? Array.Empty<IStatement>();
            Context = innerScope;
        }
        public IStatement[] Statements => instructions;

        public IContext Context {
            get;
        }

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray<IASTNode>(instructions);
        }*/

        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        public override IEnumerable<IStatement> GetStatements() => instructions;

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BlockStatement(Position,
                instructions.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray(),
                Context.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            IStatement[] nwInst = instructions.Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwStmt)) {
                    changed = true;
                    return nwStmt;
                }
                else
                    return x;
            }).ToArray();
            if (changed) {
                stmt = new BlockStatement(Position.Concat(args.Position), nwInst, Context);

                return true;
            }
            stmt = this;
            return false;
        }
    }
}
