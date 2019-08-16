using System.Collections;
using System.Collections.Generic;

namespace CompilerInfrastructure.Utils {
    public static class Map {
        class EmptyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> {
            public TValue this[TKey key] => throw new KeyNotFoundException();

            public IEnumerable<TKey> Keys {
                get {
                    yield break;
                }
            }
            public IEnumerable<TValue> Values {
                get {
                    yield break;
                }
            }
            public int Count => 0;

            public bool ContainsKey(TKey key) => false;
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
                yield break;
            }
            public bool TryGetValue(TKey key, out TValue value) {
                value = default;
                return false;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


            private EmptyDictionary() {

            }

            public readonly static EmptyDictionary<TKey, TValue> Instance = new EmptyDictionary<TKey, TValue>();
        }

        public static IReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() {
            return EmptyDictionary<TKey, TValue>.Instance;
        }
    }
}