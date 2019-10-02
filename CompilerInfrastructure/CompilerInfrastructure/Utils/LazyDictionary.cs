using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    /// <summary>
    /// A dictionary, which caches the results of a function. Circular calls will cause an exception
    /// </summary>
    public class LazyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue> {
        readonly Dictionary<TKey, TValue> underlying = new Dictionary<TKey, TValue>();
        readonly HashSet<TKey> computing = new HashSet<TKey>();
        [NonSerialized]
        readonly Func<TKey, TValue> fn;

        public LazyDictionary(Func<TKey, TValue> fn) {
            this.fn = fn ?? throw new ArgumentNullException(nameof(fn));
        }

        public TValue this[TKey key] {
            get {
                if (underlying.TryGetValue(key, out TValue val)) {
                    return val;
                }
                else {
                    if (computing.Add(key)) {
                        try {
                            underlying[key] = val = fn(key);
                            return val;
                        }
                        finally {
                            computing.Remove(key);
                        }
                    }
                    else
                        throw new CircularDependencyException();
                }
            }
            set {
                if (!IsReadOnly)
                    underlying[key] = value;
                else
                    throw new NotSupportedException("The LazyDictionary is read-only");
            }
        }

        public IEnumerable<TKey> Keys {
            get => underlying.Keys;
        }
        public IEnumerable<TValue> Values {
            get => underlying.Values;
        }
        public int Count {
            get => underlying.Count;
        }
        ICollection<TKey> IDictionary<TKey, TValue>.Keys {
            get => underlying.Keys;
        }
        ICollection<TValue> IDictionary<TKey, TValue>.Values {
            get => underlying.Values;
        }
        bool isReadOnly = false;
        public bool IsReadOnly {
            get => isReadOnly || fn is null;
            private set => isReadOnly = value;
        }
        public void MakeReadOnly() {
            IsReadOnly = true;
        }

        public bool ContainsKey(TKey key) => underlying.ContainsKey(key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => underlying.GetEnumerator();
        public bool TryGetValue(TKey key, out TValue value) => underlying.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(TKey key, TValue value) {
            if (!IsReadOnly)
                underlying.Add(key, value);
            else
                throw new NotSupportedException("The LazyDictionary is read-only");
        }
        public bool Remove(TKey key) {
            if (!IsReadOnly)
                return underlying.Remove(key);
            else
                throw new NotSupportedException("The LazyDictionary is read-only");
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public void Clear() {
            if (!IsReadOnly)
                underlying.Clear();
            else
                throw new NotSupportedException("The LazyDictionary is read-only");
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => (underlying as IDictionary<TKey, TValue>).Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (underlying as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => (underlying as IDictionary<TKey, TValue>).Remove(item);
    }
}
