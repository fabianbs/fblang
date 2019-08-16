using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    public interface IHierarchialType :IType{
        IHierarchialType SuperType {
            get;
        }
    }
}
