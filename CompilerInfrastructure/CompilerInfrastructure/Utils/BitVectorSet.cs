﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class BitVectorSet<T> : ISet<T> {
        static readonly Dictionary<T, uint> forward  = new Dictionary<T, uint>();
        //static readonly Dictionary<uint, T> backward = new Dictionary<uint, T>();
        static readonly Vector<T> backward = default;
        Vector<ulong> bitmap;
        public BitVectorSet() { }
        uint GetOrCreateIndex(T val) {
            if (!forward.TryGetValue(val, out var ret)) {
                ret = (uint) forward.Count;
                forward.Add(val, ret);
                backward[ret] = val;
            }
            return ret;
        }
        public int Count => (int) bitmap.PopCount();
        public bool IsReadOnly => false;

        public bool Add(T item) {
            var ind = GetOrCreateIndex(item);
            bool ret = bitmap.Get(ind);
            bitmap.Set(ind);
            return !ret;
        }
        public void Clear() => bitmap.Fill(0);
        public bool Contains(T item) {
            return forward.TryGetValue(item, out var ind) && bitmap.Get(ind);
        }
        public void CopyTo(T[] array, int arrayIndex) {
            //DOLATER: be more efficient
            foreach (var x in this) {
                array[arrayIndex++] = x;
            }
        }
        public void ExceptWith(IEnumerable<T> other) {
            if (this == other) {
                bitmap = default;
            }
            else if (other is BitVectorSet<T> bvs) {
                bitmap.AndNot(bvs.bitmap);
            }
            else {
                foreach (var x in other) {
                    Remove(x);
                }
            }
        }
        //TODO be more efficient here...
        public IEnumerator<T> GetEnumerator() => bitmap.GetSetPositions().Select(x => backward[x]).GetEnumerator();
        public void IntersectWith(IEnumerable<T> other) {
            if (this == other)
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
                var toremove = new List<T>();
                foreach (var x in this) {
                    if (!set.Contains(x))
                        toremove.Add(x);
                }
                foreach (var x in toremove)
                    Remove(x);
            }
        }
        public bool IsProperSubsetOf(IEnumerable<T> other) {
            if (other == this)
                return false;
            if (other is BitVectorSet<T> bvs) {
                
                if (bitmap.Length > bvs.bitmap.Length) {
                    if (bitmap.ToSpan(bvs.bitmap.Length).PopCount() > 0)
                        return false;
                }
                bool proper = bitmap.Length != bvs.bitmap.Length;
                for (uint i = 0; i < bvs.bitmap.Length; ++i) {
                    if ((bitmap[i] & ~bvs.bitmap[i]) != 0)
                        return false;
                    else if (bitmap[i] != bvs.bitmap[i])
                        proper = true;
                }
                return proper;
            }
            else {
                ISet<T> set = other is ISet<T> ? other as ISet<T>:new HashSet<T>(other);
                if (set.Count <= Count)
                    return false;
                //DOLATER be more performant here
                
                foreach (var x in this) {
                    if (!set.Contains(x))
                        return false;
                }
                return true;
            }
        }
        public bool IsProperSupersetOf(IEnumerable<T> other) {
            if (other == this)
                return false;
            if (other is BitVectorSet<T> bvs) {
                bool proper = bitmap.Length!=bvs.bitmap.Length;
                if (bitmap.Length < bvs.bitmap.Length) {
                    if (bvs.bitmap.ToSpan(bitmap.Length).PopCount() > 0)
                        return false;
                }
                for (uint i = 0; i < bitmap.Length; ++i) {
                    if ((bvs.bitmap[i] & ~bitmap[i]) != 0)
                        return false;
                    else if (bvs.bitmap[i] != bitmap[i])
                        proper = true;
                }
                return proper;
            }
            else {
                uint c = 0;
                foreach (var x in other) {
                    if (!Contains(x))
                        return false;
                    c++;
                }
                return bitmap.PopCount() > c;
            }
        }
        public bool IsSubsetOf(IEnumerable<T> other) {
            if (other == this)
                return true;
            if (other is BitVectorSet<T> bvs) {
                if (bitmap.Length > bvs.bitmap.Length) {
                    if (bitmap.ToSpan(bvs.bitmap.Length).PopCount() > 0)
                        return false;
                }
                for (uint i = 0; i < bvs.bitmap.Length; ++i) {
                    if ((bitmap[i] & ~bvs.bitmap[i]) != 0)
                        return false;
                }
                return true;
            }
            else {
                ISet<T> set = other is ISet<T>?other as ISet<T>:new HashSet<T>(other);
                //DOLATER be more performant here
                foreach(var x in this) {
                    if (!set.Contains(x))
                        return false;
                }
                return true;
            }
        }
        public bool IsSupersetOf(IEnumerable<T> other) {
            if (other == this)
                return true;
            if (other is BitVectorSet<T> bvs) {
                if (bitmap.Length < bvs.bitmap.Length) {
                    if (bvs.bitmap.ToSpan(bitmap.Length).PopCount() > 0)
                        return false;
                }
                for (uint i = 0; i < bitmap.Length; ++i) {
                    if ((bvs.bitmap[i] & ~bitmap[i]) != 0)
                        return false;
                }
                return true;
            }
            else {
                foreach (var x in other) {
                    if (!Contains(x))
                        return false;
                }
                return true;
            }
        }
        public bool Overlaps(IEnumerable<T> other) {
            if (other == this)
                return true;
            if (other is BitVectorSet<T> bvs) {
                var len = Math.Min(bitmap.Length, bvs.bitmap.Length);
                for (uint i = 0; i < len; ++i) {
                    if ((bitmap[i] & bvs.bitmap[i]) != 0)
                        return true;
                }
                return false;
            }
            else {
                foreach (var x in other) {
                    if (Contains(x))
                        return true;
                }
                return false;
            }
        }
        public bool Remove(T item) {
            if (!forward.TryGetValue(item, out var ind))
                return false;
            //var ret= bitmap.Get(ind);
            //bitmap.Set(ind, false);
            //return ret;
            return bitmap.Exchange(ind, false);
        }
        public bool SetEquals(IEnumerable<T> other) {
            if (this == other)
                return true;

            if (other is BitVectorSet<T> bvs) {
                var len = Math.Min(bitmap.Length, bvs.bitmap.Length);
                var maxLen = Math.Max(bitmap.Length, bvs.bitmap.Length);
                for (uint i = 0; i < len; ++i) {
                    if (bitmap[i] != bvs.bitmap[i])
                        return false;
                }
                if (maxLen != len) {
                    var larger = bitmap.Length > len ? this : bvs;
                    foreach (var x in larger.bitmap.ToSpan(len)) {
                        if (x != 0)
                            return false;
                    }
                }
                return true;
            }
            else if (other is ISet<T> set) {
                return set.SetEquals(this);
            }
            else {
                return new HashSet<T>(this).SetEquals(other);
            }
        }
        public void SymmetricExceptWith(IEnumerable<T> other) {
            if (this == other)
                bitmap = default;
            else if (other is BitVectorSet<T> bvs) {
                var vec = bitmap.Clone();
                vec.And(bvs.bitmap);
                bitmap.Or(bvs.bitmap);
                bitmap.AndNot(vec); // symmetric except of two sets A and B is basically (A union B) except (A intersect B)
            }
            else {
                //DOLATER: be more efficient here...
                ISet<T> set;
                if (other is ISet<T>)
                    set = other as ISet<T>;
                else
                    set = new HashSet<T>(other);
                var intersection= new HashSet<T>();
                foreach (var x in this) {
                    if (set.Contains(x))
                        intersection.Add(x);
                }
                foreach (var x in intersection) {
                    Remove(x);
                }
                foreach (var x in set) {
                    if (!intersection.Contains(x))
                        Add(x);
                }
            }
        }
        public void UnionWith(IEnumerable<T> other) {
            if (this == other)
                return;
            if (other is BitVectorSet<T> bvs) {
                bitmap.Or(bvs.bitmap);
            }
            else {
                foreach (var x in other) {
                    Add(x);
                }
            }
        }
        void ICollection<T>.Add(T item) => Add(item);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
