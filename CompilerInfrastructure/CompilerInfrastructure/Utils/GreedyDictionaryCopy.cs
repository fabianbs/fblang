using System.Collections.Generic;

namespace CompilerInfrastructure.Utils {
    public partial class GreedyDictionary<TKey, TValue> {
        public class GreedyDictionaryCopy : GreedyDictionary<TKey, TValue> {
            readonly IReadOnlyList<KeyValuePair<TKey, TValue>> original;
            int originalCount = 0;
            public GreedyDictionaryCopy(IGreedyDictionary<TKey, TValue> gd) {
                original = gd;
            }
            void UpdateIfNecessary() {
                if (original.Count > originalCount) {
                    int diff = original.Count - originalCount;
                    for (int i = 0, k = original.Count - 1; i < diff; ++i, --k) {
                        var curr = original[k];
                        base.TryAdd(curr.Key, curr.Value);
                    }
                    originalCount = original.Count;
                }
            }
            public override TValue this[TKey key] {
                get {
                    UpdateIfNecessary();
                    return base[key];
                }

                set => base[key] = value;
            }

            public override ICollection<TKey> Keys {
                get {
                    UpdateIfNecessary();
                    return base.Keys;
                }
            }

            public override ICollection<TValue> Values {
                get {
                    UpdateIfNecessary();
                    return base.Values;
                }
            }

            public override bool ContainsKey(TKey key) {
                UpdateIfNecessary();
                return base.ContainsKey(key);
            }

            public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
                UpdateIfNecessary();
                base.CopyTo(array, arrayIndex);
            }

            public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
                UpdateIfNecessary();
                return base.GetEnumerator();
            }

            public override bool TryAdd(TKey key, TValue value) {
                UpdateIfNecessary();
                return base.TryAdd(key, value);
            }

            public override bool TryGetValue(TKey key, out TValue value) {
                UpdateIfNecessary();
                return base.TryGetValue(key, out value);
            }
        }
    }
}
