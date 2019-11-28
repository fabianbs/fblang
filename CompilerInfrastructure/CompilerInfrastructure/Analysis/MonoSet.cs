using Imms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis {
    public struct MonoSet<T> : ISet<T> {
        ImmSet<T> underlying;
        readonly static ImmSet<T> EMPTY= ImmSet.Empty<T>();
        internal MonoSet(ImmSet<T> und) {
            underlying = und;
        }

        public int Count => ((ISet<T>) underlying).Count;
        public bool IsReadOnly => ((ISet<T>) underlying).IsReadOnly;
        public bool Add(T item) => ((ISet<T>) underlying).Add(item);
        public void Clear() => ((ISet<T>) underlying).Clear();
        public bool Contains(T item) => underlying.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other) => ((ISet<T>) underlying).ExceptWith(other);
        public IEnumerator<T> GetEnumerator() => underlying.GetEnumerator();
        public void IntersectWith(IEnumerable<T> other) => ((ISet<T>) underlying).IntersectWith(other);
        public bool IsProperSubsetOf(IEnumerable<T> other) => underlying.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => underlying.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => underlying.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => underlying.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => ((ISet<T>) underlying).Overlaps(other);
        public bool Remove(T item) => ((ISet<T>) underlying).Remove(item);
        public bool SetEquals(IEnumerable<T> other) => underlying.SetEquals(other);
        public void SymmetricExceptWith(IEnumerable<T> other) => ((ISet<T>) underlying).SymmetricExceptWith(other);
        public void UnionWith(IEnumerable<T> other) => ((ISet<T>) underlying).UnionWith(other);
        void ICollection<T>.Add(T item) => ((ISet<T>) underlying).Add(item);
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
            return new MonoSet<T>(underlying.Intersect(other));
        }
        public MonoSet<T> Union(IEnumerable<T> other) {
            return new MonoSet<T>(underlying.Union(other));
        }
        public bool IsEmpty => underlying.IsEmpty;

    }
    public static class MonoSet {
        public static MonoSet<T> ToMonoSet<T>(this IEnumerable<T> en) {
            return new MonoSet<T>(en.ToImmSet());
        }
        public static MonoSet<T> Of<T>(params T[] elems) {
            return new MonoSet<T>(ImmSet.Of(elems));
        }
        public static MonoSet<T> Empty<T>() {
            return new MonoSet<T>(ImmSet.Empty<T>());
        }
    }
}
