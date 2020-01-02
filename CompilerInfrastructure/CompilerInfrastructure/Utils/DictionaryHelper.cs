using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class Dictionary {
        class DictionaryHelper<TKey, TValue> {
            public static Lazy<IReadOnlyDictionary<TKey, TValue>> Instance = new Lazy<IReadOnlyDictionary<TKey, TValue>>(() => new Dictionary<TKey, TValue>());
        }
        public static IReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() {
            return DictionaryHelper<TKey, TValue>.Instance.Value;
        }
        public static IEnumerable<KeyValuePair<TKey, TValue>> FilterByName<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dic, string name) where TKey : ISignature {
            if (dic is SignatureMultiMap<TKey, TValue> smm) {
                return smm.FilterByName(name);
            }
            return dic.Where(x => x.Key.Name == name);
        }
        public static IEnumerable<TValue> FilterValuesByName<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dic, string name) where TKey : ISignature {
            return dic switch {
                IValueFilterableByName<TKey, TValue> filterable => filterable.FilterValuesByName(name),
                SignatureMultiMap<TKey, TValue> smm             => smm.FilterByName(name).Values,
                _ => dic.Where(x => x.Key.Name == name)
                                                                      .Select(x => x.Value)
            };
        }
    }
}
