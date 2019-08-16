using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public interface IGreedyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyList<KeyValuePair<TKey, TValue>> {
        IGreedyDictionary<TKey, TValue> LazyCopy();
    }
}
