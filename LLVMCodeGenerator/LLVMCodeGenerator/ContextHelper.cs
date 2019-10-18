using System;
using System.Collections.Generic;
using System.Text;

namespace LLVMCodeGenerator {
    public static class ContextHelper {
        class IRBContext : IDisposable {
            readonly IntPtr oldBB, irb;
            readonly NativeManagedContext.ManagedContext ctx;
            public IRBContext(NativeManagedContext.ManagedContext ctx, IntPtr oldBB, IntPtr irb) {
                this.ctx = ctx;
                this.irb = irb;
                this.oldBB = oldBB;
            }
            public void Dispose() {
                ctx.ResetInsertPoint(oldBB, irb);
            }
        }
        public static IDisposable PushIRBContext(this NativeManagedContext.ManagedContext ctx, IntPtr bb, IntPtr irb) {
            var oldBB = ctx.GetCurrentBasicBlock(irb);
            ctx.ResetInsertPoint(bb, irb);
            return new IRBContext(ctx, oldBB, irb);
        }
    }
}
