using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    readonly struct MethodInfo {
        readonly FBlangParser.ExprContext _expr;
        readonly FBlangParser.BlockInstructionContext _blockInstruction;

        public MethodInfo(FBlangParser.ExprContext expr, FBlangParser.BlockInstructionContext blockInstruction) {
            _expr = expr;
            _blockInstruction = blockInstruction;
        }
        public MethodInfo(FBlangParser.MethodDefContext ctx) {
            _expr = ctx.expr();
            _blockInstruction = ctx.blockInstruction();
        }
        public MethodInfo(FBlangParser.MainMethodDefContext ctx) {
            _expr = null;
            _blockInstruction = ctx.blockInstruction();
        }
#pragma warning disable IDE1006 // Benennungsstile
        public FBlangParser.ExprContext expr() => _expr;
        public FBlangParser.BlockInstructionContext blockInstruction() => _blockInstruction;
#pragma warning restore IDE1006 // Benennungsstile
    }
}
