using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class GenericParameterMap<TKey, TValue> : IDictionary<TKey, TValue>, IEquatable<GenericParameterMap<TKey, TValue>> where TKey : IGenericParameter where TValue : ITypeOrLiteral {
        readonly HashSet<IDictionary<TKey, TValue>> equiCache = new HashSet<IDictionary<TKey, TValue>>();
        readonly IDictionary<TKey, TValue> dic;
        public GenericParameterMap() {
            dic = new Dictionary<TKey, TValue>();
        }
        public GenericParameterMap(IDictionary<TKey, TValue> _dic) {
            dic = new Dictionary<TKey, TValue>(_dic);
        }
        private GenericParameterMap(IDictionary<TKey, TValue> _dic, bool doCopy) {
            if (doCopy) {
                dic = new Dictionary<TKey, TValue>(_dic);
            }
            else {
                dic = _dic;
            }
        }
        public GenericParameterMap(IEnumerable<KeyValuePair<TKey, TValue>> vals) : this() {
            foreach (var kvp in vals) {
                dic.TryAdd(kvp.Key, kvp.Value);
            }
        }
        public GenericParameterMap(IEnumerable<(TKey, TValue)> vals) : this() {
            foreach ((var ky, var val) in vals) {
                dic.TryAdd(ky, val);
            }
        }

        public TValue this[TKey key] {
            get => dic[key];
            set => dic[key] = value;
        }

        public ICollection<TKey> Keys {
            get => dic.Keys;
        }
        public ICollection<TValue> Values {
            get => dic.Values;
        }
        public int Count {
            get => dic.Count;
        }
        public bool IsReadOnly {
            get => dic.IsReadOnly;
        }

        public void Add(TKey key, TValue value) => dic.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => dic.Add(item);
        public void Clear() => dic.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => dic.Contains(item);
        public bool ContainsKey(TKey key) => dic.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => dic.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dic.GetEnumerator();
        public IReadOnlyList<TValue> ToValueList() {
            var contained = new HashSet<string>();
            var ret = new List<TValue>();
            foreach (var kvp in dic) {
                if (contained.Add(kvp.Key.Name))
                    ret.Add(kvp.Value);
            }
            return ret.AsReadOnly();
        }

        public bool Remove(TKey key) => dic.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => dic.Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => dic.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => dic.GetEnumerator();

        public override bool Equals(object obj) => Equals(obj as GenericParameterMap<TKey, TValue>);
        public bool Equals(GenericParameterMap<TKey, TValue> other) {
            if (other is null)
                return false;
            if (equiCache.Contains(other.dic))
                return true;
            if (dic.Count != other.dic.Count)
                return false;
            var equi = new MultiMap<TKey, TKey>();
            foreach (var kvp in dic) {
                foreach (var ovp in other.dic) {
                    if (Equals(kvp.Value, ovp.Value)) {
                        equi.Add(kvp.Key, ovp.Key);
                    }
                }
                if (!equi.ContainsKey(kvp.Key))
                    return false;
            }
            var equiSet = new MultiSet<ISet<TKey>>(new FunctionalEquiComparer<ISet<TKey>>(
                    (x, y) => x.SetEquals(y),
                    x => x is null ? 0 : HashCode.Combine(x.Select(y => y.GetHashCode()))
                )
            );
            foreach (var kvp in equi) {
                equiSet.Add(kvp.Value);
            }
            //DOLATER if value is IGenericParameter, is will be isomorphic too

            if (equiSet.Keys.Any(x => (uint)x.Count != equiSet[x]))
                return false;

            equiCache.Add(other.dic);
            other.equiCache.Add(dic);
            return true;
        }
        public override int GetHashCode() {
            //TODO hc must be equal for all pairs x, y W/ Equals(x, y)==true
            return 149728143 + (dic.Count == 0 ? 0 : HashCode.Combine(dic.Values.Select(x => x.GetHashCode())));
        }

        public static bool operator ==(GenericParameterMap<TKey, TValue> map1, GenericParameterMap<TKey, TValue> map2) => EqualityComparer<GenericParameterMap<TKey, TValue>>.Default.Equals(map1, map2);
        public static bool operator !=(GenericParameterMap<TKey, TValue> map1, GenericParameterMap<TKey, TValue> map2) => !(map1 == map2);
        public static implicit operator GenericParameterMap<TKey, TValue>(Dictionary<TKey, TValue> dic) {
            return new GenericParameterMap<TKey, TValue>(dic, false);
        }
    }
    public static class GenericParameterMapExtensions {
        public static GenericParameterMap<K, V> Then<K, V>(this GenericParameterMap<K, V> @this, GenericParameterMap<K, V> other, IContext current, IContext parent, bool onlyUseKeysFromThis=false) where K : IGenericParameter
                                                                                                                             where V : ITypeOrLiteral, IReplaceableStructureElement<V, K, V> {
            if (other is null || other.Count == 0) {
                return @this;
            }
            var ret = new GenericParameterMap<K, V>();
            /*foreach (var kvp in other) {
                if (@this.TryGetValue(kvp.Key, out var repl))
                    ret.Add(kvp.Key, repl.Replace(other, current, parent));
                else
                    ret.Add(kvp.Key, kvp.Value);
            }
            foreach (var ky in @this.Keys.Except(other.Keys)) {
                ret.Add(ky, @this[ky]);
            }*/
            foreach (var kvp in @this) {
                ret.Add(kvp.Key, kvp.Value.Replace(other, current, parent));
            }
            if (!onlyUseKeysFromThis) {
                foreach (var ky in other.Keys.Except(@this.Keys)) {
                    ret.Add(ky, other[ky]);
                }
            }
            return ret;
        }
    }
}
