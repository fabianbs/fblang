using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IReplaceableStructureElement<out T, K, V> where K : IGenericParameter where V : ITypeOrLiteral {
        T Replace(GenericParameterMap<K, V> genericActualParameter, IContext curr, IContext parent);
    }
    public interface IReplaceableStructureElement<out T> : IReplaceableStructureElement<T, IGenericParameter, ITypeOrLiteral> {
        //T Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
    }
}
