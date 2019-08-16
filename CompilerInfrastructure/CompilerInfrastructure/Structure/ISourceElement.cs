using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure {
    public interface ISourceElement : IASTNode, IPositional {

    }
    public interface IPositional {
        Position Position {
            get;
        }
    }
}
