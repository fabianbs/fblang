using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    [Serializable]
    enum ReferenceCapability {
        Normal,
        Unique,
        Const,
        Immutable
    }
}
