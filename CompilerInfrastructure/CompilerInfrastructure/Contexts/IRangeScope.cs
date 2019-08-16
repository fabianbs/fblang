using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Contexts {
    public interface IRangeScope {
        IContext Context {
            get;
        }
    }
    public interface IRangeScope<T> : IRangeScope where T : IContext {
        new T Context {
            get;
        }
    }
}
