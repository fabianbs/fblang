using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CompilerInfrastructure.Utils {
    public struct Vector<T> : IEnumerable<T>, IEquatable<Vector<T>> {
        T[] arrVal;
        uint capacity;
        uint size;
        public Vector(uint initialSize) {
            if (initialSize > 0) {
                arrVal = new T[capacity = Math.Max(4, initialSize)];
                size = initialSize;
            }
            else {
                this = default;
            }
        }
        public Vector(in Vector<T> other) {
            arrVal = (T[]) other.arrVal?.Clone();
            capacity = other.capacity;
            size = other.size;
        }
        public Vector(Vector<T> other) {
            arrVal = (T[]) other.arrVal?.Clone();
            capacity = other.capacity;
            size = other.size;
        }
        public Vector(IEnumerable<T> other) {
            if (other is ICollection<T> coll) {
                var cap = size = checked((uint) coll.Count);
                capacity = cap; // TODO 
                arrVal = new T[cap];
                coll.CopyTo(arrVal, 0);
            }
            else {
                arrVal = other.ToArray();
                capacity = size = checked((uint) arrVal.LongLength);
            }
        }
        void EnsureCapacity(uint minCap) {
            if (minCap > capacity) {
                if (capacity == 0)
                    capacity = Math.Max(minCap, 2);
                else {
                    do {
                        capacity <<= 1;
                    } while (minCap > capacity && capacity > 0);
                }
                if (capacity == 0)// overflow
                    capacity = uint.MaxValue;
                Array.Resize(ref arrVal, (int) capacity);
            }
        }
        public static Vector<T> Reserve(uint minCap) {
            Vector<T> ret = default;
            ret.EnsureCapacity(minCap);
            return ret;
        }
        public static Vector<T> Reserve(int minCap) {
            var ret = default(Vector<T>);
            ret.EnsureCapacity(checked((uint) minCap));
            return ret;
        }
        public uint Length => size;
        public int Count => (int) size;
        public ref T this[uint index] {
            get {
                if (index >= size) {
                    EnsureCapacity(index + 1);
                    size = index + 1;
                    if (arrVal is null)
                        throw new InvalidProgramException();
                }
                return ref arrVal[index];
            }
        }

        public T PopBack() {
            if (size > 0) {
                var ret = arrVal[--size];
                arrVal[size] = default;
                return ret;
            }
            return default;
        }
        public ref T Back() {
            if (size > 0)
                return ref arrVal[size - 1];
            throw new IndexOutOfRangeException();
        }

        public ref T Front() {
            if (size > 0)
                return ref arrVal[0];
            throw new IndexOutOfRangeException();
        }
        public void PushBack(in T val) {
            EnsureCapacity(checked(size + 1));
            arrVal[size++] = val;
        }
        public void Add(T val) {
            EnsureCapacity(checked(size + 1));
            arrVal[size++] = val;
        }
        public void AddRange(params T[] vals) {
            if (vals is null)
                return;
            EnsureCapacity(checked(size + (uint) vals.LongLength));
            Array.Copy(vals, 0, arrVal, size, vals.LongLength);
            size += (uint) vals.LongLength;
        }
        public T[] ToArray() {
            if (size == 0)
                return Array.Empty<T>();
            var ret = new T[size];
            Array.Copy(arrVal, ret, size);
            return ret;
        }
        public void Fill(T val) {
            if (arrVal != null) {
                var len = size;
                var count = Math.Min(len, 20);
                int i;
                for (i = 0; i < count; ++i) {
                    arrVal[i] = val;
                }
                len -= count;

                while (len > 0) {
                    var oldC = count;
                    count = Math.Min(len, count);
                    Array.Copy(arrVal, 0, arrVal, oldC, count);
                    len -= count;
                    count += oldC;
                }
            }
        }
        public T[] AsArray() {
            if (size == 0)
                return Array.Empty<T>();
            if (size != capacity) {
                Array.Resize(ref arrVal, (int) size);
                capacity = size;
            }
            return arrVal;
        }
        public T[] AsArray(uint offset, uint count) {
            if (offset > 0) {
                if (offset > size)
                    return Array.Empty<T>();
                Array.Copy(arrVal, offset, arrVal, 0, size - offset);
                size -= offset;
                size = Math.Min(size, count);
            }
            return AsArray();
        }

        public IEnumerator<T> GetEnumerator() {
            for (uint i = 0; i < size; ++i)
                yield return arrVal[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override bool Equals(object obj) => obj is Vector<T> vector && Equals(vector);
        public bool Equals(Vector<T> other) {
            var sz = size;
            if (sz != other.size)
                return false;
            var comp = EqualityComparer<T>.Default;
            var otherArr = other.arrVal;
            for (uint i = 0; i < sz; ++i) {
                if (!comp.Equals(arrVal[i], otherArr[i]))
                    return false;
            }
            return true;

        }

        public override int GetHashCode() {
            return arrVal.AsSpan(0, (int)size).GetArrayHashCode();
            /*if (arrVal is null)
                return 0;
            else {
                
                return arrVal.Fold(8).Select(x => x.Length switch {
                    8 => HashCode.Combine(x[0], x[1], x[2], x[3], x[4], x[5], x[6], x[7]),
                    7 => HashCode.Combine(x[0], x[1], x[2], x[3], x[4], x[5], x[6]),
                    6 => HashCode.Combine(x[0], x[1], x[2], x[3], x[4], x[5]),
                    5 => HashCode.Combine(x[0], x[1], x[2], x[3], x[4]),
                    4 => HashCode.Combine(x[0], x[1], x[2], x[3]),
                    3 => HashCode.Combine(x[0], x[1], x[2]),
                    2 => HashCode.Combine(x[0], x[1]),
                    1 => HashCode.Combine(x[0]),
                    _ => 0
                }).Sum();
            }*/
        }
        public static bool operator ==(Vector<T> v1, Vector<T> v2) {
            return v1.capacity == v2.capacity
                && v1.size == v2.size
                && v1.arrVal == v2.arrVal;
        }
        public static bool operator !=(Vector<T> v1, Vector<T> v2) {
            return !(v1 == v2);
        }
    }
}
