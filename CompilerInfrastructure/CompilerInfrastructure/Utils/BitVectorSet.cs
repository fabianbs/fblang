using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CompilerInfrastructure.Utils {
    using System.Diagnostics;

    public class BitVectorSet<T> : ISet<T>, IComparable<ISet<T>>, IEquatable<BitVectorSet<T>> {
        private static readonly Dictionary<T, uint> Forward = new Dictionary<T, uint>();

        //static readonly Dictionary<uint, T> backward = new Dictionary<uint, T>();
        private static Vector<T> _backward;
        private Vector<ulong> bitmap;

        public BitVectorSet() {
        }

        public BitVectorSet(params T[] elems) : this((IEnumerable<T>) elems) {
        }

        public BitVectorSet(IEnumerable<T> elems) {
            UnionWith(elems);
        }

        public BitVectorSet(BitVectorSet<T> other) {
            if (other != null)
                bitmap = new Vector<ulong>(other.bitmap);
        }

        public bool IsEmpty => bitmap.PopCount() == 0;

        public int CompareTo(ISet<T> other) {
            //TODO be more efficient here
            if (!IsSubsetOf(other))
                return 1;
            if (SetEquals(other))
                return 0;
            return -1;
        }

        public bool Equals([AllowNull] BitVectorSet<T> other) {
            return other != null && SetEquals(other);
        }

        public int Count => (int) bitmap.PopCount();
        public bool IsReadOnly => false;

        public bool Add(T item) {
            var ind = GetOrCreateIndex(item);
            var ret = bitmap.Get(ind);
            bitmap.Set(ind);
            return !ret;
        }

        public void Clear() {
            bitmap.Fill(0);
        }

        public bool Contains(T item) {
            Debug.Assert(item != null, nameof(item) + " != null");
            return Forward.TryGetValue(item, out var ind) && bitmap.Get(ind);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            //DOLATER: be more efficient
            foreach (var x in this)
                array[arrayIndex++] = x;
        }

        public void ExceptWith(IEnumerable<T> other) {
            if (ReferenceEquals(this, other))
                bitmap = default;
            else if (other is BitVectorSet<T> bvs)
                bitmap.AndNot(bvs.bitmap);
            else
                foreach (var x in other)
                    Remove(x);
        }

        public IEnumerator<T> GetEnumerator() //=> bitmap.GetSetPositions().Select(x => backward[x]).GetEnumerator();
        {
            return new BitVectorSetIterator(bitmap);
        }

        public void IntersectWith(IEnumerable<T> other) {
            if (ReferenceEquals(this, other))
                return;
            if (other is BitVectorSet<T> bvs) {
                bitmap.And(bvs.bitmap);
            }
            else {
                ISet<T> set;
                if (other is ISet<T>)
                    set = other as ISet<T>;
                else
                    set = new HashSet<T>(other);
                var toremove = this.Where(x => !set.Contains(x)).ToList();
                foreach (var x in toremove)
                    Remove(x);
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return false;
            if (other is BitVectorSet<T> bvs) {
                if (bitmap.Length > bvs.bitmap.Length) {
                    if (bitmap.ToSpan(bvs.bitmap.Length).PopCount() > 0)
                        return false;
                }
                var proper = bitmap.Length != bvs.bitmap.Length;
                var minlength = Math.Min(bitmap.Length, bvs.bitmap.Length);
                for (uint i = 0; i < minlength; ++i)
                    if ((bitmap[i] & ~bvs.bitmap[i]) != 0)
                        return false;
                    else if (bitmap[i] != bvs.bitmap[i])
                        proper = true;
                return proper;
            }

            var set = other is ISet<T> ts ? ts : new HashSet<T>(other);
            return set.Count > Count && this.All(x => set.Contains(x));
            //DOLATER be more performant here
        }

        public bool IsProperSupersetOf(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return false;
            if (other is BitVectorSet<T> bvs) {
                var proper = bitmap.Length != bvs.bitmap.Length;
                if (bitmap.Length < bvs.bitmap.Length) {
                    if (bvs.bitmap.ToSpan(bitmap.Length).PopCount() > 0)
                        return false;
                }
                var minlength = Math.Min(bitmap.Length, bvs.bitmap.Length);
                for (uint i = 0; i < minlength; ++i)
                    if ((bvs.bitmap[i] & ~bitmap[i]) != 0)
                        return false;
                    else if (bvs.bitmap[i] != bitmap[i])
                        proper = true;
                return proper;
            }

            uint c = 0;
            foreach (var x in other) {
                if (!Contains(x))
                    return false;
                c++;
            }

            return bitmap.PopCount() > c;
        }

        public bool IsSubsetOf(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return true;
            if (other is BitVectorSet<T> bvs) {
                if (bitmap.Length > bvs.bitmap.Length) {
                    if (bitmap.ToSpan(bvs.bitmap.Length).PopCount() > 0)
                        return false;
                }
                var minlength = Math.Min(bitmap.Length, bvs.bitmap.Length);
                for (uint i = 0; i < minlength; ++i)
                    if ((bitmap[i] & ~bvs.bitmap[i]) != 0)
                        return false;
                return true;
            }

            var set = other is ISet<T> ts ? ts : new HashSet<T>(other);
            //DOLATER be more performant here
            return this.All(x => set.Contains(x));
        }

        public bool IsSupersetOf(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return true;
            if (!(other is BitVectorSet<T> bvs))
                return other.All(Contains);
            if (bitmap.Length < bvs.bitmap.Length)
                if (bvs.bitmap.ToSpan(bitmap.Length).PopCount() > 0)
                    return false;
            var minlength = Math.Min(bitmap.Length, bvs.bitmap.Length);
            for (uint i = 0; i < minlength; ++i)
                if ((bvs.bitmap[i] & ~bitmap[i]) != 0)
                    return false;
            return true;

        }

        public bool Overlaps(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return true;
            if (!(other is BitVectorSet<T> bvs))
                return other.Any(Contains);
            var len = Math.Min(bitmap.Length, bvs.bitmap.Length);
            for (uint i = 0; i < len; ++i)
                if ((bitmap[i] & bvs.bitmap[i]) != 0)
                    return true;
            return false;

        }

        public bool Remove(T item) {
            Debug.Assert(item != null, nameof(item) + " != null");
            return Forward.TryGetValue(item, out var ind) && bitmap.Exchange(ind, false);
            //var ret= bitmap.Get(ind);
            //bitmap.Set(ind, false);
            //return ret;
        }

        public bool SetEquals(IEnumerable<T> other) {
            if (ReferenceEquals(this, other))
                return true;

            switch (other) {
                case BitVectorSet<T> bvs: {
                    var len = Math.Min(bitmap.Length, bvs.bitmap.Length);
                    var maxLen = Math.Max(bitmap.Length, bvs.bitmap.Length);
                    for (uint i = 0; i < len; ++i)
                        if (bitmap[i] != bvs.bitmap[i])
                            return false;
                    if (maxLen == len)
                        return true;
                    var larger = bitmap.Length > len ? this : bvs;
                    foreach (var x in larger.bitmap.ToSpan(len)) {
                        if (x != 0)
                            return false;
                    }

                    return true;
                }
                case ISet<T> set:
                    return set.SetEquals(this);
                default:
                    return new HashSet<T>(this).SetEquals(other);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            if (ReferenceEquals(this, other)) {
                bitmap = default;
            }
            else if (other is BitVectorSet<T> bvs) {
                // var vec = bitmap.Clone();
                // vec.And(bvs.bitmap);
                // bitmap.Or(bvs.bitmap);
                // bitmap.AndNot(vec); // symmetric except of two sets A and B is basically (A union B) except (A intersect B)
                bitmap.Xor(bvs.bitmap);
            }
            else {
                //DOLATER: be more efficient here...
                ISet<T> set;
                if (other is ISet<T> ts)
                    set = ts;
                else
                    set = new HashSet<T>(other);
                var intersection = new HashSet<T>();
                foreach (var x in this)
                    if (set.Contains(x))
                        intersection.Add(x);
                foreach (var x in intersection)
                    Remove(x);
                foreach (var x in set) {
                    if (!intersection.Contains(x))
                        Add(x);
                }
            }
        }

        public void UnionWith(IEnumerable<T> other) {
            if (ReferenceEquals(this, other))
                return;
            if (other is BitVectorSet<T> bvs)
                bitmap.Or(bvs.bitmap);
            else {
                foreach (var x in other)
                    Add(x);
            }
        }

        void ICollection<T>.Add(T item) {
            Debug.Assert(item != null, nameof(item) + " != null");
            Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        private static uint GetOrCreateIndex(T val) {
            if (Forward.TryGetValue(val, out var ret))
                return ret;
            ret = (uint) Forward.Count;
            Forward.Add(val, ret);
            _backward[ret] = val;

            return ret;
        }

        public bool IsDisjointWith(IEnumerable<T> other) {
            if (ReferenceEquals(other, this))
                return false;
            if (!(other is BitVectorSet<T> bvs))
                return other.All(x => !Contains(x));
            var len = Math.Min(bitmap.Length, bvs.bitmap.Length);
            for (uint i = 0; i < len; ++i) {
                if ((bitmap[i] & bvs.bitmap[i]) != 0)
                    return false;
            }

            return true;
            //DOLATER: more cases (ICollection, ISet, ...) for being more efficient

        }

        public override bool Equals(object obj) {
            return Equals(obj as BitVectorSet<T>);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() {
            uint len = 0;
            for (uint i = 0; i < bitmap.Length; ++i)
                if (bitmap[i] != 0)
                    len = i + 1;
            // trailing zero-words do not contribute
            return bitmap.ToSpan(0, len).GetArrayHashCode();
        }

        private struct BitVectorSetIterator : IEnumerator<T> {
            private Vector<ulong> bitmap;
            private uint index;
            private ulong offset;
            private uint globIdx;

            public BitVectorSetIterator(in Vector<ulong> bm) {
                bitmap = bm;
                index = ~0u;
                offset = 1ul << 63;
                globIdx = ~0u;
            }

            public T Current => _backward[globIdx];
            object IEnumerator.Current => Current;

            public void Dispose() {
            }

            public bool MoveNext() {
                do {
                    globIdx++;
                    offset <<= 1;
                    if (offset != 0)
                        continue;
                    if (++index >= bitmap.Length)
                        return false;
                    if (bitmap[index].PopCount() == 0) // little optimization for sparse sets
                        continue;
                    offset = 1;
                } while ((bitmap[index] & offset) == 0);

                return true;
            }

            public void Reset() {
                index = ~0u;
                offset = 1ul << 63;
                globIdx = ~0u;
            }
        }

        #region Operators

        public static BitVectorSet<T> operator |(BitVectorSet<T> set1, IEnumerable<T> other) {
            var copy = new BitVectorSet<T>(set1);
            copy.UnionWith(other);
            return copy;
        }

        public static BitVectorSet<T> operator &(BitVectorSet<T> set1, IEnumerable<T> other) {
            var copy = new BitVectorSet<T>(set1);
            copy.IntersectWith(other);
            return copy;
        }

        public static BitVectorSet<T> operator -(BitVectorSet<T> set1, IEnumerable<T> other) {
            var copy = new BitVectorSet<T>(set1);
            copy.ExceptWith(other);
            return copy;
        }

        public static BitVectorSet<T> operator -(BitVectorSet<T> set1, T other) {
            var copy = new BitVectorSet<T>(set1);
            copy.Remove(other);
            return copy;
        }

        public static BitVectorSet<T> operator +(BitVectorSet<T> set1, IEnumerable<T> other) {
            var copy = new BitVectorSet<T>(set1);
            copy.UnionWith(other);
            return copy;
        }

        public static BitVectorSet<T> operator +(BitVectorSet<T> set1, T other) {
            var copy = new BitVectorSet<T>(set1);
            copy.Add(other);
            return copy;
        }

        public static bool operator <=(BitVectorSet<T> set1, BitVectorSet<T> set2) {
            return set1.IsSubsetOf(set2);
        }

        public static bool operator >=(BitVectorSet<T> set1, BitVectorSet<T> set2) {
            return set1.IsSupersetOf(set2);
        }

        public static bool operator <(BitVectorSet<T> set1, BitVectorSet<T> set2) {
            return set1.IsProperSubsetOf(set2);
        }

        public static bool operator >(BitVectorSet<T> set1, BitVectorSet<T> set2) {
            return set1.IsProperSupersetOf(set2);
        }

        #endregion
    }
}