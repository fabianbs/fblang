using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CompilerInfrastructure {
    [Serializable]
    public class SignatureMultiMap<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ISerializable where TKey : ISignature {
        readonly Dictionary<string, Dictionary<TKey, TValue>> value = new Dictionary<string, Dictionary<TKey, TValue>>();
        readonly bool onlyConsiderPublicValuesForSerialization;
        int count = 0;

        public SignatureMultiMap(bool onlyConsiderPublicValuesForSerialization = true) {
            this.onlyConsiderPublicValuesForSerialization = onlyConsiderPublicValuesForSerialization;
        }
        #region IDictionary<TKey, TValue> implementation


        public TValue this[TKey key] {
            get {
                if (value.TryGetValue(key.Name, out Dictionary<TKey, TValue> dic)) {
                    return dic[key];
                }
                return default;
            }
            set {
                Add(key, value);
            }
        }

        public ICollection<TKey> Keys {
            get {
                return value.SelectMany(x => x.Value.Keys).ToList();
            }
        }
        public ICollection<TValue> Values {
            get {
                return value.SelectMany(x => x.Value.Values).ToList();
            }
        }
        public int Count {
            get => count;
        }
        public bool IsReadOnly {
            get => false;
        }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get => value.SelectMany(x => x.Value.Keys);
        }
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get => value.SelectMany(x => x.Value.Values);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) {
            this[key] = value;
        }
        public bool Add(TKey key, TValue value) {
            if (!this.value.TryGetValue(key.Name, out Dictionary<TKey, TValue> dic)) {
                dic = new Dictionary<TKey, TValue>();
                this.value.Add(key.Name, dic);
            }
            if (dic.TryAdd(key, value)) {
                count++;
                return true;
            }
            return false;
        }
        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }
        public void Clear() {
            value.Clear();
            count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return value.TryGetValue(item.Key.Name, out Dictionary<TKey, TValue> dic) && dic.Contains(item);
        }
        public bool ContainsKey(TKey key) {
            return value.TryGetValue(key.Name, out Dictionary<TKey, TValue> dic) && dic.ContainsKey(key);
        }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            foreach (var kvp in value) {
                (kvp.Value as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
                arrayIndex += kvp.Value.Count;
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            foreach (var kvp in value) {
                foreach (var x in kvp.Value)
                    yield return x;
            }
        }
        public bool Remove(TKey key) {
            if (value.TryGetValue(key.Name, out Dictionary<TKey, TValue> dic)) {
                if (dic.Remove(key)) {
                    count--;
                    return true;
                }
            }
            return false;
        }
        public bool Remove(KeyValuePair<TKey, TValue> item) {
            if (value.TryGetValue(item.Key.Name, out Dictionary<TKey, TValue> dic)) {
                if ((dic as IDictionary<TKey, TValue>).Remove(item)) {
                    count--;
                    return true;
                }
            }
            return false;
        }
        public bool TryGetValue(TKey key, out TValue val) {
            if (value.TryGetValue(key.Name, out Dictionary<TKey, TValue> dic) && dic.TryGetValue(key, out val)) {
                return true;
            }
            val = default;
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        public IReadOnlyDictionary<TKey, TValue> FilterByName(string name) {
            if (value.TryGetValue(name, out var ret))
                return ret;
            return Dictionary.Empty<TKey, TValue>();
        }

        protected static bool TValueIsIVisible() {
            return typeof(TValue).IsSubclassOf(typeof(IVisible)) || typeof(TValue) == typeof(IVisible);
        }

        protected SignatureMultiMap(SerializationInfo info, StreamingContext context) {
            onlyConsiderPublicValuesForSerialization = info.GetBoolean(nameof(onlyConsiderPublicValuesForSerialization));
            if (onlyConsiderPublicValuesForSerialization && TValueIsIVisible()) {
                var ser = (KeyValuePair<TKey, TValue>[])info.GetValue(nameof(value), typeof(KeyValuePair<TKey, TValue>[]));
                count = ser.Length;
                foreach (var kvp in ser) {
                    value[kvp.Key.Name][kvp.Key] = kvp.Value;
                }
            }
            else {
                value = (Dictionary<string, Dictionary<TKey, TValue>>)info.GetValue(nameof(value), typeof(Dictionary<string, Dictionary<TKey, TValue>>));
                count = info.GetInt32(nameof(count));
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(onlyConsiderPublicValuesForSerialization), onlyConsiderPublicValuesForSerialization);
            if (onlyConsiderPublicValuesForSerialization && TValueIsIVisible()) {
                var ser = this.Where(x => (x.Value as IVisible).Visibility == Visibility.Public).ToArray();
                info.AddValue(nameof(value), ser);
            }
            else {
                info.AddValue(nameof(value), value);
                info.AddValue(nameof(count), count);
            }

        }
    }
}
