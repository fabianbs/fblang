using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Contexts {
    [Serializable]
    public enum CannotDefine {
        Success,
        AlreadyExisting,
        EndlessRecursion,
        NotAllowedInCurrentContext
    }
}
