using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types.Generic {
    public interface IHierarchialTypeTemplate<out T> : ITypeTemplate<T> where T : IType {
        IHierarchialType SuperType { get; }
    }
}
