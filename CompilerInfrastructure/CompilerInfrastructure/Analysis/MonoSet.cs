using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis {
    public struct MonoSet<T> : ISet<T> {
        readonly BitVectorSet<T> underlying;
        static readonly BitVectorSet<T> EMPTY = new BitVectorSet<T>();
        internal MonoSet(BitVectorSet<T> und) {
            underlying = und ?? throw new ArgumentNullException(nameof(und));
        }

        public int Count => underlying.Count;



        public bool IsReadOnly => underlying.IsReadOnly;
        public bool Add(T item) => underlying.Add(item);
        public void Clear() => underlying.Clear();
        public bool Contains(T item) => underlying.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                underlying.ExceptWith(m.underlying);
            underlying.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator() => underlying.GetEnumerator();
        public void IntersectWith(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                underlying.IntersectWith(m.underlying);
            underlying.IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.IsProperSubsetOf(m.underlying);
            return underlying.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.IsProperSupersetOf(m.underlying);
            return underlying.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.IsSubsetOf(m.underlying);
            return underlying.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.IsSupersetOf(m.underlying);
            return underlying.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.Overlaps(m.underlying);
            return underlying.Overlaps(other);
        }

        public bool Remove(T item) => underlying.Remove(item);
        public bool SetEquals(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return underlying.SetEquals(m.underlying);
            return underlying.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                underlying.SymmetricExceptWith(m.underlying);
            underlying.SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                underlying.UnionWith(m.underlying);
            underlying.UnionWith(other);
        }

        void ICollection<T>.Add(T item) => underlying.Add(item);
        IEnumerator IEnumerable.GetEnumerator() => underlying.GetEnumerator();

        public static MonoSet<T> operator +(MonoSet<T> s1, MonoSet<T> s2) {
            return new MonoSet<T>((s1.underlying ?? EMPTY) + (s2.underlying ?? EMPTY));
        }
        public static MonoSet<T> operator +(MonoSet<T> s1, IEnumerable<T> s2) {
            return s1 + s2.ToMonoSet();
        }
        public static MonoSet<T> operator -(MonoSet<T> s1, MonoSet<T> s2) {
            return new MonoSet<T>((s1.underlying ?? EMPTY) - (s2.underlying ?? EMPTY));
        }
        public static MonoSet<T> operator -(MonoSet<T> s1, IEnumerable<T> s2) {
            return s1 - s2.ToMonoSet();
        }
        public static MonoSet<T> operator +(MonoSet<T> s1, T s2) {
            return new MonoSet<T>((s1.underlying ?? EMPTY) + s2);
        }
        public bool IsDisjointWith(IEnumerable<T> other) {
            return underlying.IsDisjointWith(other);
        }
        public MonoSet<T> Intersect(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return new MonoSet<T>(underlying & m.underlying);
            return new MonoSet<T>(underlying & other);
        }
        public MonoSet<T> Union(IEnumerable<T> other) {
            if (other is MonoSet<T> m)
                return new MonoSet<T>(underlying | m.underlying);
            return new MonoSet<T>(underlying | other);
        }
        public bool IsEmpty => underlying.IsEmpty;

    }
    public static class MonoSet {
        public static MonoSet<T> ToMonoSet<T>(this IEnumerable<T> en) {
            if (en is MonoSet<T> m)
                return m;
            if (en is BitVectorSet<T> bvs)
                return new MonoSet<T>(bvs);
            return new MonoSet<T>(new BitVectorSet<T>(en));
        }
        public static MonoSet<T> Of<T>(params T[] elems) {
            return new MonoSet<T>(new BitVectorSet<T>(elems));
        }
        public static MonoSet<T> Empty<T>() {
            return new MonoSet<T>(new BitVectorSet<T>());
        }
    }
}
