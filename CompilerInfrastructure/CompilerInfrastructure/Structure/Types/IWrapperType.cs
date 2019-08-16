using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    public interface IWrapperType :IType{
        IType ItemType {
            get;
        }
    }
}
