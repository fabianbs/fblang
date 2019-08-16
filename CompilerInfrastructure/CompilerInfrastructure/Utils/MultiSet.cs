using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class MultiSet<T> : IEnumerable<T>, IReadOnlyDictionary<T, uint> {
        readonly Dictionary<T, Box<uint>> dic;
        int count;
        public MultiSet() {
            dic = new Dictionary<T, Box<uint>>();
        }
        public MultiSet(IEqualityComparer<T> cmp) {
            dic = new Dictionary<T, Box<uint>>(cmp);
        }
        public uint this[T key] {
            get {
                if (dic.TryGetValue(key, out var ret)) {
                    return ret.Value;
                }
                return 0;
            }
        }

        public IEnumerable<T> Keys {
            get => dic.Keys;
        }
        public IEnumerable<uint> Values {
            get => dic.Values.Select(x => x.Value);
        }
        public int Count {
            get => count;
        }
        public void Add(T val) {
            if (dic.TryGetValue(val, out var num)) {
                num.Value++;
            }
            else
                dic[val] = new Box<uint>(1);
            ++count;
        }
        public bool ContainsKey(T key) => TryGetValue(key, out _);

        public IEnumerator<T> GetEnumerator() => dic.Keys.GetEnumerator();
        public bool TryGetValue(T key, out uint value) {
            if (TryGetValue(key, out value)) {
                return value > 0;
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<KeyValuePair<T, uint>> IEnumerable<KeyValuePair<T, uint>>.GetEnumerator() {
            foreach (var kvp in dic) {
                yield return new KeyValuePair<T, uint>(kvp.Key, kvp.Value.Value);
            }
        }
    }
}
