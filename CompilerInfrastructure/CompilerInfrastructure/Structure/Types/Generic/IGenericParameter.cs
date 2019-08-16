using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types.Generic {
    public interface IGenericParameter : ITypeOrLiteral, IPositional {
        string Name {
            get;
        }
    }
}
