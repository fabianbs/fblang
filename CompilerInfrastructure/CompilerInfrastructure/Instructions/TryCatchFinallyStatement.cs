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
    public class TryCatchFinallyStatement : StatementImpl {
        //DOLATER What about catch-labels???
        readonly IStatement[] blocks;
        readonly bool lastIsFinally;

        public TryCatchFinallyStatement(Position pos, IStatement tryBlock, ReadOnlySpan<IStatement> catchBlocks, IStatement finallyBlock = null)
            : base(pos) {
            lastIsFinally = finallyBlock != null;
            blocks = new BlockStatement[1 + catchBlocks.Length + (lastIsFinally ? 1 : 0)];
            blocks[0] = tryBlock ?? "The try-block must be present".Report(pos, Statement.Error);
            catchBlocks.CopyTo(blocks.AsSpan(1));
            if (lastIsFinally)
                blocks[^1] = finallyBlock;
        }
        public TryCatchFinallyStatement(Position pos, IStatement tryBlock, ICollection<IStatement> catchBlocks, IStatement finallyBlock = null)
            : base(pos) {
            lastIsFinally = finallyBlock != null;
            blocks = new BlockStatement[1 + catchBlocks.Count + (lastIsFinally ? 1 : 0)];
            blocks[0] = tryBlock ?? "The try-block must be present".Report(pos, Statement.Error);
            catchBlocks.CopyTo(blocks, 1);
            if (lastIsFinally)
                blocks[^1] = finallyBlock;
        }

        public IStatement TryBlock => blocks[0];
        public ReadOnlySpan<IStatement> CatchBlocks => blocks.AsSpan(1, blocks.Length - (lastIsFinally ? 2 : 1));
        public IStatement FinallyBlock => lastIsFinally ? blocks[^1] : null;

        public bool HasFinally => lastIsFinally;

       /* public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray<IASTNode>(blocks);
        }*/

        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        public override IEnumerable<IStatement> GetStatements() => blocks;

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new TryCatchFinallyStatement(Position,
                TryBlock.Replace(genericActualParameter, curr, parent),
                CatchBlocks.Select(x => x.Replace(genericActualParameter, curr, parent)),
                FinallyBlock.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            var nwBlocks = blocks.Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    return nwX;
                }
                return x;
            }).ToArray();
            if (changed) {
                stmt = new TryCatchFinallyStatement(Position.Concat(args.Position), 
                    nwBlocks[0], 
                    nwBlocks.AsSpan(1, lastIsFinally ? nwBlocks.Length - 2 : nwBlocks.Length - 1), 
                    lastIsFinally ? nwBlocks.Last() : null
                );
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
