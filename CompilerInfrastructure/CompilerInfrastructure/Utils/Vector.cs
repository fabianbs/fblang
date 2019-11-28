using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CompilerInfrastructure.Utils {
    /// <summary>
    /// A lightweight, high-performance array-list with similar API as <see cref="List{T}"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Vector<T> : IEnumerable<T>, IEquatable<Vector<T>> {
        // Use StructBox<T>[] instead of T[] here to bypass .Net's array-covariance. May increase performance (in tests by 1,5)
        StructBox<T>[] arrVal;
        uint capacity;
        uint size;
        public Vector(uint initialSize) {
            if (initialSize > 0) {
                arrVal = Unsafe.As<StructBox<T>[]>(new T[capacity = Math.Max(4, initialSize)]);
                size = initialSize;
            }
            else {
                this = default;
            }
        }
        public Vector(in Vector<T> other) {
            arrVal = (StructBox<T>[]) other.arrVal?.Clone();
            capacity = other.capacity;
            size = other.size;
        }
        public Vector(Vector<T> other) {
            arrVal = (StructBox<T>[]) other.arrVal?.Clone();
            capacity = other.capacity;
            size = other.size;
        }
        public Vector(IEnumerable<T> other) {
            if (other is Vector<T> vec) {
                arrVal = (StructBox<T>[]) vec.arrVal?.Clone();
                capacity = vec.capacity;
                size = vec.size;
            }
            else {
                T[] arr;
                if (other is T[] tarr) {
                    arr = tarr.Clone() as T[];
                    capacity = size = checked((uint) arr.LongLength);
                }
                else if (other is ICollection<T> coll) {
                    var cap = size = checked((uint) coll.Count);
                    capacity = cap;
                    arr = new T[cap];
                    coll.CopyTo(arr, 0);
                }
                else {
                    arr = other.ToArray();
                    capacity = size = checked((uint) arr.LongLength);
                }
                arrVal = Unsafe.As<StructBox<T>[]>(arr);
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
                var arr = Unsafe.As<T[]>(arrVal);
                Array.Resize<T>(ref arr, (int) capacity);
                arrVal = Unsafe.As<StructBox<T>[]>(arr);
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
                return ref arrVal[index].Value;
            }
        }
        /*public ref T this[Index index] {
            get {
                var i = checked((uint)index.Value);
                if (index.IsFromEnd) {
                    if (i > size)
                        throw new IndexOutOfRangeException();
                    i = size - i;
                }
                return ref this[i];
            }
        }*/
        public Vector<T> Clone() {
            return new Vector<T>(this);
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
                return ref arrVal[size - 1].Value;
            throw new IndexOutOfRangeException();
        }

        public ref T Front() {
            if (size > 0)
                return ref arrVal[0].Value;
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
            Array.Copy(vals, 0, Unsafe.As<T[]>(arrVal), size, vals.LongLength);
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
            var ret = Unsafe.As<T[]>(arrVal);
            if (size != capacity) {
                Array.Resize(ref ret, (int) size);
                arrVal = Unsafe.As<StructBox<T>[]>(ret);
                capacity = size;
            }
            return ret;
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
        public Span<T> ToSpan() {
            return new Span<T>(Unsafe.As<T[]>(arrVal), 0, (int) size);
        }
        public Span<T> ToSpan(int offset) {
            return new Span<T>(Unsafe.As<T[]>(arrVal), offset, (int) size - offset);
        }
        public Span<T> ToSpan(uint offset) {
            return new Span<T>(Unsafe.As<T[]>(arrVal), (int)offset, (int) (size - offset));
        }
        public Span<T> ToSpan(int offset, int len) {
            return new Span<T>(Unsafe.As<T[]>(arrVal), offset, Math.Max(0, Math.Min((int) size - offset, len)));
        }

        public IEnumerator<T> GetEnumerator() {
            for (uint i = 0; i < size; ++i)
                yield return arrVal[i];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEach(Action<T> fn) {
            for (uint i = 0; i < size; ++i)
                fn(arrVal[i]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEach<TState>(TState state, Action<TState, T> fn) {
            for (uint i = 0; i < size; ++i)
                fn(state, arrVal[i]);
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
            return arrVal.AsSpan(0, (int) size).GetArrayHashCode();
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
