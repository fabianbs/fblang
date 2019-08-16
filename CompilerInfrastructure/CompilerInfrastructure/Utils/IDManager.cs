using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public interface IIDManager<T> {
        T this[uint id] { get; }
        bool TryGetValue(uint id, out T value);
        bool TryGetID(T value, out uint id);
        uint Add(T val);
        bool TryAdd(T val, uint id);
        bool Remove(T val);
        bool RemoveID(uint id);
    }
    public class IDManager<T> : IIDManager<T> {
        Vector<T> values;
        readonly Dictionary<T, uint> ids = new Dictionary<T, uint>();
        readonly SortedSet<uint> emptySet = new SortedSet<uint>();// priorityqueue?

        public T this[uint id] {
            get {
                if (TryGetValue(id, out T ret))
                    return ret;
                throw new KeyNotFoundException();
            }
        }
        /// <summary>
        /// Gets the value associated with the specified ID. Only safe if this id was never removed before, but might be more performant than the indexer or TryGetValue.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns></returns>
        public T UnsafeGet(uint id) {
            if (id < values.Length)
                return values[id];
            return default;
        }
        public uint Add(T val) {
            if (ids.TryGetValue(val, out var id)) {
                return id;
            }
            if (emptySet.Any()) {
                id = emptySet.Min;
                emptySet.Remove(id);
                values[id] = val;
                ids.Add(val, id);
            }
            else {
                id = (uint)values.Count;
                values.Add(val);
                ids.Add(val, id);
            }
            return id;
        }
        public bool Remove(T val) {
            if (ids.TryGetValue(val, out var id)) {
                ids.Remove(val);
                values[id] = default;
                if (id == values.Length - 1) {
                    values.PopBack();
                    //TODO remove max elements from emptyset if possible (also in removeID)
                }
                else {
                    emptySet.Add(id);
                }
                return true;
            }
            return false;
        }
        public bool RemoveID(uint id) {
            if (id < values.Length) {
                if (id == values.Length - 1) {
                    ids.Remove(values[id]);
                    values.PopBack();
                    return true;
                }
                values[id] = default;
                return emptySet.Add(id);
            }
            return false;
        }
        public bool TryAdd(T val, uint id) {
            if (id < values.Length) {
                if (emptySet.Remove(id)) {
                    values[id] = val;
                    ids.Add(val, id);
                    return true;
                }
            }
            else if (id == values.Length) {
                values.Add(val);
                ids.Add(val, id);
                return true;
            }
            else {
                values[id] = val;
                ids.Add(val, id);
                //emptySet.Add();// TODO adrange
            }
            return false;
        }
        public bool TryGetID(T value, out uint id) {
            return ids.TryGetValue(value, out id);
        }
        public bool TryGetValue(uint id, out T value) {
            if (id < (uint)values.Count && !emptySet.Contains(id)) {
                value = values[id];
                return true;
            }
            value = default;
            return false;
        }
    }
}
