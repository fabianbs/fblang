using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    /// <summary>
    /// A dictionary, similar to <see cref="LazyDictionary{TKey, TValue}"/>, which caches the results
    /// of a function. Unlike to the <see cref="LazyDictionary{TKey, TValue}"/> it does not throw an exception
    /// on circular calls. Instead it will return a specified default-value.
    /// 
    /// This class might be useful for context-insensitive interprocedural static dataflow-analyses
    /// </summary>
    public class RecursiveLazyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue> {
        readonly Dictionary<TKey, TValue> cache;
        readonly HashSet<TKey> computing;
        bool contributing = true;
        readonly TValue dflt;
        [NonSerialized]
        readonly Func<TKey, TValue> fn;

        /// <summary>
        /// Creates a new, empty RecursiveLazyDictionary with the given properties
        /// </summary>
        /// <param name="_fn">The generator-function for the values</param>
        /// <param name="_default">The default-value, which is used in case of infinite recursion</param>
        /// <param name="_kyComp">The eqality-comparer for comparing two keys</param>
        public RecursiveLazyDictionary(Func<TKey, TValue> _fn, TValue _default, IEqualityComparer<TKey> _kyComp = null) {
            fn = _fn ?? throw new ArgumentNullException(nameof(_fn));
            if (_kyComp is null)
                _kyComp = EqualityComparer<TKey>.Default;
            cache = new Dictionary<TKey, TValue>(_kyComp);
            computing = new HashSet<TKey>(_kyComp);
            dflt = _default;
        }
        /// <summary>
        /// Creates a new, empty RecursiveLazyDictionary with the given properties
        /// </summary>
        /// <param name="_fn">The generator-function for the values with a special second argument for recursion. 
        /// This second argument will be bound to the generator itself</param>
        /// <param name="_default">The default-value, which is used in case of infinite recursion</param>
        /// <param name="_kyComp">The eqality-comparer for comparing two keys</param>
        public RecursiveLazyDictionary(Func<TKey, Func<TKey, TValue>, TValue> _fn, TValue _default, IEqualityComparer<TKey> _kyComp = null) {
            if (_fn is null)
                throw new ArgumentNullException(nameof(_fn));
            fn = ky => _fn(ky, x => this[x]);
            if (_kyComp is null)
                _kyComp = EqualityComparer<TKey>.Default;
            cache = new Dictionary<TKey, TValue>(_kyComp);
            computing = new HashSet<TKey>(_kyComp);
            dflt = _default;
        }

        public TValue this[TKey key] {
            get {
                if (cache.TryGetValue(key, out var ret))
                    return ret;
                if (computing.Add(key)) {
                    try {
                        ret = fn(key);
                        if (contributing || computing.Count == 1) {
                            cache[key] = ret;
                        }
                        return ret;
                    }
                    finally {
                        computing.Remove(key);
                        if (computing.Count == 0) {
                            contributing = true;
                        }
                    }
                }
                else {
                    // When we reach a loopback-call, we cannot contribute further to the result,
                    // so return the dflt;
                    // but nested callers may be incomplete, so don't cache their results (by setting contributing to false)
                    contributing = false;
                    return dflt;
                }
            }
            set {
                if (!IsReadOnly)
                    cache[key] = value;
                else
                    throw new NotSupportedException("The recursive LazyDictionary is readonly");
            }
        }
        public bool IsReadOnly {
            get;
            private set;
        }
        public void MakeReadOnly() {
            IsReadOnly = true;
        }
        public IEnumerable<TKey> Keys => cache.Keys;
        public IEnumerable<TValue> Values => cache.Values;
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => cache.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => cache.Values;
        public int Count => cache.Count;

        public bool ContainsKey(TKey key) => cache.ContainsKey(key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => cache.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGetValue(TKey key, out TValue value) => cache.TryGetValue(key, out value);
        public void Add(TKey key, TValue value) {
            if (!IsReadOnly)
                cache.Add(key, value);
            else
                throw new NotSupportedException("The recursive LazyDictionary is readonly");
        }
        public bool Remove(TKey key) {
            if (!IsReadOnly)
                return cache.Remove(key);
            else
                throw new NotSupportedException("The recursive LazyDictionary is readonly");
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public void Clear() {
            if (!IsReadOnly)
                cache.Clear();
            else
                throw new NotSupportedException("The recursive LazyDictionary is readonly");
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => (cache as IDictionary<TKey, TValue>).Contains(item);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => (cache as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => (cache as IDictionary<TKey, TValue>).Remove(item);
    }
}
