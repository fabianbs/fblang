using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public partial class GreedyDictionary<TKey, TValue> : IGreedyDictionary<TKey, TValue> {
        readonly Dictionary<TKey, TValue> underlying;
        protected private readonly List<KeyValuePair<TKey, TValue>> values;
        public GreedyDictionary() {
            underlying = new Dictionary<TKey, TValue>();
            values = new List<KeyValuePair<TKey, TValue>>();
        }
        public GreedyDictionary(IDictionary<TKey, TValue> toCopy) {
            underlying = new Dictionary<TKey, TValue>(toCopy);
            values = new List<KeyValuePair<TKey, TValue>>(toCopy);
        }
       /* public GreedyDictionary(IEnumerable<KeyValuePair<TKey, TValue>> toCopy) {
            underlying = new Dictionary<TKey, TValue>(toCopy);
            values = new List<KeyValuePair<TKey, TValue>>(toCopy);
        }*/
        public GreedyDictionary(int initCapacity) {
            if (initCapacity < 4)
                initCapacity = 4;
            underlying = new Dictionary<TKey, TValue>(initCapacity);
            values = new List<KeyValuePair<TKey, TValue>>(initCapacity);
        }
        public virtual TValue this[TKey key] {
            get => underlying[key];
            set {
                if (underlying.TryAdd(key, value)) {
                    values.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
            }
        }
        public KeyValuePair<TKey, TValue> this[int index] {
            get => values[index];
        }
        public virtual ICollection<TKey> Keys {
            get => underlying.Keys;
        }
        public virtual ICollection<TValue> Values {
            get => underlying.Values;
        }
        public int Count {
            get => values.Count;
        }
        public bool IsReadOnly {
            get => false;
        }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get => Keys;
        }
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get => Values;
        }
        
        public virtual bool TryAdd(TKey key, TValue value) {
            if (underlying.TryAdd(key, value)) {
                values.Add(new KeyValuePair<TKey, TValue>(key, value));
                return true;
            }
            return false;
        }
        public void Add(TKey key, TValue value) {
            if (!TryAdd(key, value))
                throw new ArgumentException($"The key {key} already exists in the Dictionary");
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        /// </summary>
        /// <exception cref="NotSupportedException">A greedy dictionary does not support removing elements</exception>
        public void Clear() {
            throw new NotSupportedException("A greedy dictionary does not support removing elements");
        }
        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return TryGetValue(item.Key, out var val) && Equals(val, item.Value);
        }
        public virtual bool ContainsKey(TKey key) => underlying.ContainsKey(key);
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => values.GetEnumerator();
        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"></see>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key">key</paramref> was not found in the original <see cref="System.Collections.Generic.IDictionary`2"></see>.
        /// </returns>
        /// <exception cref="NotSupportedException">A greedy dictionary does not support removing elements</exception>
        public bool Remove(TKey key) {
            throw new NotSupportedException("A greedy dictionary does not support removing elements");
        }
        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.</param>
        /// <returns>
        /// true if <paramref name="item">item</paramref> was successfully removed from the <see cref="System.Collections.Generic.ICollection`1"></see>; otherwise, false. This method also returns false if <paramref name="item">item</paramref> is not found in the original <see cref="System.Collections.Generic.ICollection`1"></see>.
        /// </returns>
        /// <exception cref="NotSupportedException">A greedy dictionary does not support removing elements</exception>
        public bool Remove(KeyValuePair<TKey, TValue> item) {
            throw new NotSupportedException("A greedy dictionary does not support removing elements");
        }
        public virtual bool TryGetValue(TKey key, out TValue value) => underlying.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IGreedyDictionary<TKey, TValue> LazyCopy() => new GreedyDictionaryCopy(this);
    }
}
