using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public abstract class PrimitiveOperatorOverload : MethodImpl, IOperatorOverload {

        internal PrimitiveOperatorOverload(OverloadableOperator op) {
            Operator = op;
        }

        public virtual OverloadableOperator Operator {
            get;
        }

        public override Visibility Visibility {
            get;
        } = Visibility.Public;
        public override Position  Position {
            get;
        } = default;
        public override Method.Specifier Specifiers {
            get;
            protected set;
        } = Method.Specifier.OperatorOverload;

    }
}
