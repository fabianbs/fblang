using CompilerInfrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLVMCodeGenerator {
   public static class CallingContext {
        public static IType ReceiverType(this IType definedInType, IMethod met) {
            if (met.HasUniqueThis()) {
                return definedInType.AsValueType().AsByRef();
            }
            else
                return definedInType;
        }
    }
}
