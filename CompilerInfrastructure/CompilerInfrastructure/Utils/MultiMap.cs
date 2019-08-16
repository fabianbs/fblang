using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CompilerInfrastructure.Utils {
    [Serializable]
    public class MultiMap<TKey, TValue> : Dictionary<TKey, ISet<TValue>> {
        public MultiMap() {
        }

        public MultiMap(IDictionary<TKey, ISet<TValue>> dictionary) : base(dictionary) {
        }

        public MultiMap(IEqualityComparer<TKey> comparer) : base(comparer) {
        }

        public MultiMap(int capacity) : base(capacity) {
        }

        public MultiMap(IDictionary<TKey, ISet<TValue>> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) {
        }

        public MultiMap(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) {
        }

        protected MultiMap(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
        public bool Add(TKey ky, TValue val) {
            if (!TryGetValue(ky, out var vals)) {
                vals = new HashSet<TValue>();
                Add(ky, vals);
            }
            return vals.Add(val);
        }
        public new void Add(TKey ky, ISet<TValue> val) {
            if (TryGetValue(ky, out var vals))
                vals.UnionWith(val);
            else
                base.Add(ky, val);
        }
        public void Add(TKey ky, IEnumerable<TValue> val) {
            if (TryGetValue(ky, out var vals))
                vals.UnionWith(val);
            else
                base.Add(ky, new HashSet<TValue>(val));
        }
        public bool Remove(TKey ky, TValue val) {
            if (TryGetValue(ky, out var vals) && vals.Remove(val)) {
                if (!vals.Any())
                    Remove(ky);
                return true;
            }
            return false;
        }
    }
}
