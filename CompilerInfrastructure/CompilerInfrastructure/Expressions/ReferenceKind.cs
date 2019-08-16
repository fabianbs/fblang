using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Expressions {
    public enum ReferenceKind {
        None,
        RValue,
        ConstLValue,
        MutableLValue
    }
}
