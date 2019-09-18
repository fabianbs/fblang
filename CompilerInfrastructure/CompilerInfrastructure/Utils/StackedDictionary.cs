using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class StackedDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
        class Frame : IDisposable {
            readonly StackedDictionary<TKey, TValue> sdic;
            bool isDisposed = false;
            internal Frame(StackedDictionary<TKey, TValue> sdic) {
                this.sdic = sdic;
            }
            public void Dispose() {
                if (!isDisposed) {
                    isDisposed = true;
                    sdic.TryPop();
                }
            }
        }
        Stack<Dictionary<TKey, TValue>> stack = new Stack<Dictionary<TKey, TValue>>();
        Dictionary<TKey, TValue> tos = new Dictionary<TKey, TValue>();
        public void Push() {
            stack.Push(tos);
            tos = new Dictionary<TKey, TValue>();
        }
        public IDisposable PushFrame() {
            Push();
            return new Frame(this);
        }
        public bool TryPop() {
            if (stack.TryPop(out var nos)) {
                tos = nos;
                return true;
            }
            return false;
        }
        public void ClearStage() {
            tos.Clear();
        }

        #region IDictionary implementation

        public TValue this[TKey key] {
            get {
                if (TryGetValue(key, out var ret)) {
                    return ret;
                }
                throw new KeyNotFoundException();
            }
            set {
                tos[key] = value;
            }
        }

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values => throw new NotImplementedException();

        public int Count => throw new NotSupportedException();

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => tos.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>) tos).Add(item);
        public void Clear() {
            tos.Clear();
            stack.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            if(((IDictionary<TKey, TValue>) tos).Contains(item)) 
                return true;
            foreach(var dic in stack) {
                if (((IDictionary<TKey, TValue>) dic).Contains(item))
                    return true;
            }
            return false;
        }

        public bool ContainsKey(TKey key) {
            return TryGetValue(key, out _);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            var keySet = new HashSet<TKey>();
            foreach(var kvp in tos) {
                yield return kvp;
                keySet.Add(kvp.Key);
            }
            foreach(var dic in stack) {
                foreach(var kvp in dic) {
                    if (keySet.Add(kvp.Key))
                        yield return kvp;
                }
            }
        }
        public bool Remove(TKey key) => throw new NotSupportedException();
        public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();
        public bool TryGetValue(TKey key, out TValue ret) {
            if (tos.TryGetValue(key, out ret))
                return true;
            foreach(var dic in stack) {
                if (dic.TryGetValue(key, out ret))
                    return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
        public bool TryAdd(TKey ky, TValue val) {
            return tos.TryAdd(ky, val);
        }
    }
}
