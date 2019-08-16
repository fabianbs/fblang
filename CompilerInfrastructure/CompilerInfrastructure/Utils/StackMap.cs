using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class StackMap<TKey, TValue> : IDictionary<TKey, Stack<TValue>> {
        class StackFirstMapFrame : IDisposable {
            readonly IDictionary<TKey, Stack<TValue>> dic;
            readonly TKey key;
            bool isDisposed = false;
            public StackFirstMapFrame(IDictionary<TKey, Stack<TValue>> dic, TKey key) {
                this.dic = dic;
                this.key = key;
            }
            public void Dispose() {
                if (!isDisposed) {
                    isDisposed = true;
                    dic.Remove(key);
                }
            }
        }
        //TODO implement that efficient
        readonly Dictionary<TKey, Stack<TValue>> underlying = new Dictionary<TKey, Stack<TValue>>();
        readonly IComparer<TKey> keyCmp;
        public StackMap(IComparer<TKey> kyCmp = null) {
            keyCmp = kyCmp ?? Comparer<TKey>.Create((x, y) => Equals(x, y) ? 0 : 1);
        }
        public TValue this[TKey ky] {
            get {
                TryPeekValue(ky, out var ret);
                return ret;
            }
        }
        Stack<TValue> IDictionary<TKey, Stack<TValue>>.this[TKey key] {
            get => underlying[key];
            set => underlying[key] = value;
        }

        public ICollection<TKey> Keys => ((IDictionary<TKey, Stack<TValue>>)underlying).Keys;

        public ICollection<Stack<TValue>> Values => ((IDictionary<TKey, Stack<TValue>>)underlying).Values;

        public int Count => underlying.Count;

        public bool IsReadOnly => ((IDictionary<TKey, Stack<TValue>>)underlying).IsReadOnly;

        public void Add(TKey key, Stack<TValue> value) => underlying.Add(key, value);
        public void Add(KeyValuePair<TKey, Stack<TValue>> item) => ((IDictionary<TKey, Stack<TValue>>)underlying).Add(item);
        public void Clear() => underlying.Clear();
        public bool Contains(KeyValuePair<TKey, Stack<TValue>> item) => ((IDictionary<TKey, Stack<TValue>>)underlying).Contains(item);
        public bool IsTop(KeyValuePair<TKey, TValue> item) => IsTop(item.Key, item.Value);
        public bool IsTop(TKey key, TValue val) => TryPeekValue(key, out var top) && Equals(top, val);
        public bool ContainsKey(TKey key) => underlying.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, Stack<TValue>>[] array, int arrayIndex) => ((IDictionary<TKey, Stack<TValue>>)underlying).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, Stack<TValue>>> GetEnumerator() => ((IDictionary<TKey, Stack<TValue>>)underlying).GetEnumerator();
        public bool Remove(TKey key) => underlying.Remove(key);
        public bool Remove(KeyValuePair<TKey, Stack<TValue>> item) => ((IDictionary<TKey, Stack<TValue>>)underlying).Remove(item);
        public bool TryGetValue(TKey key, out Stack<TValue> value) => underlying.TryGetValue(key, out value);
        bool FindLowest(TKey ky, out TKey ret) {
            if (underlying.ContainsKey(ky)) {
                ret = ky;
                return true;
            }
            var sorted = underlying.Keys.OrderBy(x => x, Comparer<TKey>.Create((x, y) => keyCmp.Compare(ky, x) - keyCmp.Compare(ky, y)));
            ret = sorted.FirstOrDefault();
            return sorted.Take(1).Any(x => keyCmp.Compare(ky, x) < int.MaxValue);
        }
        public bool TryPeekValue(TKey key, out TValue value) {
            if (FindLowest(key, out key) && underlying.TryGetValue(key, out var st)) {
                return st.TryPeek(out value);
            }
            value = default;
            return false;
        }
        public bool TryPopValue(TKey key, out TValue value) {
            if (FindLowest(key, out key) && underlying.TryGetValue(key, out var st) && st.TryPop(out value)) {
                if (st.Count == 0)
                    underlying.Remove(key);
                return true;
            }
            value = default;
            return false;
        }
        public bool TryPeekValue(TKey key, out TValue value, out TKey actualKey) {
            if (FindLowest(key, out actualKey) && underlying.TryGetValue(actualKey, out var st)) {
                return st.TryPeek(out value);
            }
            value = default;
            return false;
        }
        public bool TryPopValue(TKey key, out TValue value, out TKey actualKey) {
            if (FindLowest(key, out actualKey) && underlying.TryGetValue(actualKey, out var st) && st.TryPop(out value)) {
                if (st.Count == 0)
                    underlying.Remove(key);
                return true;
            }
            value = default;
            return false;
        }
        public void Push(TKey key, TValue val) {
            if (!underlying.TryGetValue(key, out var s)) {
                s = new Stack<TValue>();
                underlying.TryAdd(key, s);
            }
            s.Push(val);
        }
        public IDisposable PushFrame(TKey key, TValue val) {
            if (!underlying.TryGetValue(key, out var s)) {
                s = new Stack<TValue>();
                s.Push(val);
                underlying.TryAdd(key, s);
                return new StackFirstMapFrame(underlying, key);
            }
            return s.PushFrame(val);
        }
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<TKey, Stack<TValue>>)underlying).GetEnumerator();
    }
}
