using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SimdVec = System.Numerics.Vector;

namespace CompilerInfrastructure {
    public static class EnumerableHelper {
        public static IEnumerable<T> ConcatMany<T, U>(this IEnumerable<U> enums) where U : IEnumerable<T> {
            foreach (var en in enums) {
                foreach (var x in en) {
                    yield return x;
                }
            }
        }
        internal class EnumerableCollection<T> : ICollection<T>, IReadOnlyCollection<T> {
            readonly IEnumerable<T> underlying;

            public EnumerableCollection(IEnumerable<T> it, int count) {
                underlying = it;
                Count = count;
            }

            public int Count {
                get;
            }
            public bool IsReadOnly {
                get => true;
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => underlying.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) {
                if (arrayIndex < 0)
                    throw new IndexOutOfRangeException(nameof(arrayIndex));
                using var it = underlying.GetEnumerator();
                for (int i = 0; it.MoveNext() && i < Count && arrayIndex < array.Length; ++i) {
                    array[arrayIndex++] = it.Current;
                }
            }
            public IEnumerator<T> GetEnumerator() {
                using var it = underlying.GetEnumerator();
                for (int i = 0; it.MoveNext() && i < Count; ++i) {
                    yield return it.Current;
                }
            }
            public bool Remove(T item) => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class AsReadOnlyCollection<T> : IReadOnlyCollection<T>, ICollection<T> {
            readonly ICollection<T> coll;
            public AsReadOnlyCollection(ICollection<T> col) {
                coll = col ?? Array.Empty<T>();
            }
            public int Count {
                get => coll.Count;
            }
            public bool IsReadOnly {
                get => true;
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            public bool Contains(T item) => coll.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => coll.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => coll.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static ICollection<T> AsCollection<T>(this IEnumerable<T> it, int count = int.MaxValue) {
            if (it is ICollection<T> coll)
                return coll;
            return new EnumerableCollection<T>(it, count);
        }
        public static IReadOnlyCollection<T> Append<T>(this ICollection<T> coll, T val) {
            return new EnumerableCollection<T>((coll as IEnumerable<T>).Concat(new[] { val }), coll.Count + 1);
        }

        public static void AddRange<T>(this ICollection<T> coll, IEnumerable<T> values) {
            if (coll is List<T> list) {
                list.AddRange(values);
            }
            else {
                foreach (var x in values) {
                    coll.Add(x);
                }
            }
        }
        public static void ForEach<T>(this IEnumerable<T> iter, Action<T> fe) {
            if (iter is null)
                return;
            foreach (var x in iter) {
                fe(x);
            }
        }
        public static IEnumerable<T> WrapTemporary<T>(this IEnumerator<T> it) {
            while (it.MoveNext()) {
                yield return it.Current;
            }
        }
        internal static IEnumerable<(T, U)> Zip<T, U>(this IEnumerable<T> it1, IEnumerable<U> it2) {
            using var itit1 = it1.GetEnumerator();
            using var itit2 = it2.GetEnumerator();
            while (itit1.MoveNext() && itit2.MoveNext()) {
                yield return (itit1.Current, itit2.Current);
            }
        }
        public static bool HasCount<T>(this IEnumerable<T> iter, int count) {
            if (count <= 0)
                return !iter.Any();
            using var it = iter.GetEnumerator();
            while (count > 0 && it.MoveNext()) {
                count--;
            }
            return count == 0 && !it.MoveNext();
        }
        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> coll) {
            if (coll is IReadOnlyCollection<T> roc)
                return roc;
            return new AsReadOnlyCollection<T>(coll);
        }
        public static IEnumerable<T[]> Fold<T>(this IEnumerable<T> it, uint foldLen) {
            using var iter = it.GetEnumerator();
            var vec = new T[foldLen];
            while (true) {
                uint i;
                for (i = 0; i < foldLen && iter.MoveNext(); ++i) {
                    vec[i] = iter.Current;
                }
                if (i == foldLen) {
                    yield return vec;
                    vec = new T[foldLen];
                }
                else {
                    Array.Resize(ref vec, (int) i);
                    yield return vec;
                    yield break;
                }
            }
        }
        public static int GetArrayHashCode<T>(this T[] arr) {
            if (arr is null)
                return 0;
            return GetArrayHashCode(arr.AsSpan());
        }
        /*public static int GetArrayHashCode<T>(this ReadOnlySpan<T> arr) {
            if (typeof(T).IsPrimitive) {
                Unsafe.AsRef()
            }
            else {
                // normal 
                var comp = EqualityComparer<T>.Default;
                //return arr.Aggregate(31, (acc, x) => unchecked(17 + acc * comp.GetHashCode(x)));
                int hc = 31;
                foreach (var x in arr) {
                    hc = 17 + hc * comp.GetHashCode(x);
                }
                return hc;
            }
        }*/
        public static int GetArrayHashCode<T>(this Span<T> arr) {
            return GetArrayHashCode((ReadOnlySpan<T>)arr);
        }
        public static int GetArrayHashCode<T>(this ReadOnlySpan<T> arr) {
            if (typeof(T).IsPrimitive) {
                // vectorize
                unchecked {
                    var vectorLen = System.Numerics.Vector<byte>.Count;
                    var ret = new System.Numerics.Vector<byte>(31);
                    ref var ptr = ref Unsafe.As<T, byte>(ref Unsafe.AsRef<T>(arr[0]));
                    Span<byte> buf = stackalloc byte[vectorLen]; // vectorlen is always small, so allocate on the stack...
                    buf.Fill(0);
                    var arrLen = Marshal.SizeOf<T>() * arr.Length;
                    int i;
                    for (i = 0; i <= arrLen - vectorLen; i += vectorLen) {
                        Unsafe.CopyBlock(ref buf[0], ref Unsafe.AddByteOffset(ref ptr, (IntPtr) i), (uint) vectorLen);
                        ret += new System.Numerics.Vector<byte>(buf) * 17;
                    }
                    if (i < arrLen) {
                        Unsafe.CopyBlock(ref buf[0], ref Unsafe.AddByteOffset(ref ptr, (IntPtr) i), (uint) (arrLen - i));
                        ret += new System.Numerics.Vector<byte>(buf) * 17;
                    }
                    var ones = new System.Numerics.Vector<int>(1);

                    return SimdVec.Dot(ones, SimdVec.AsVectorInt32(ret));
                }
            }
            else {
                // normal 
                var comp = EqualityComparer<T>.Default;
                //return arr.Aggregate(31, (acc, x) => unchecked(17 + acc * comp.GetHashCode(x)));
                int hc = 31;
                foreach(var x in arr) {
                    hc = 17 + hc * comp.GetHashCode(x);
                }
                return hc;
            }
        }
    }
    public static class Collection {
        public static ICollection<T> Repeat<T>(T val, int count) {
            return new EnumerableHelper.EnumerableCollection<T>(Enumerable.Repeat(val, count), count);
        }
    }
    public static class List {
        static class EmptyList<T> {
            internal static readonly IReadOnlyList<T> value = new ReadOnlyCollection<T>(new List<T>());
        }
        public static IReadOnlyList<T> Empty<T>() {
            return EmptyList<T>.value;
        }
    }
    public static class ROSpanHelper {
        public static ReadOnlySpan<U> Select<T, U>(this ReadOnlySpan<T> inp, Func<T, U> f) {
            var ret = new U[inp.Length];
            for (int i = 0; i < inp.Length; ++i) {
                ret[i] = f(inp[i]);
            }
            return ret;
        }
    }
}
