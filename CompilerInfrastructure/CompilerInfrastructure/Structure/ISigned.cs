using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure {
    public interface ISigned<T> : ISigned where T : ISignature {
        new T Signature {
            get;
        }
    }
    public interface ISigned {
        ISignature Signature { get; }
    }
}
