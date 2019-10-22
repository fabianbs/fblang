using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class Set {
        class EmptySet<T> : ISet<T> {
            public static readonly ISet<T> Instance = new EmptySet<T>();

            private EmptySet() {
            }
            public int Count {
                get => 0;
            }
            public bool IsReadOnly {
                get => true;
            }

            public bool Add(T item) => throw new NotSupportedException();
            public void Clear() {
            }
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) {
            }
            public void ExceptWith(IEnumerable<T> other) {
            }
            public IEnumerator<T> GetEnumerator() {
                yield break;
            }
            public void IntersectWith(IEnumerable<T> other) {
            }
            public bool IsProperSubsetOf(IEnumerable<T> other) {
                return other != null && other.Any();
            }
            public bool IsProperSupersetOf(IEnumerable<T> other) => false;
            public bool IsSubsetOf(IEnumerable<T> other) => true;
            public bool IsSupersetOf(IEnumerable<T> other) {
                return other is null || !other.Any();
            }
            public bool Overlaps(IEnumerable<T> other) => false;
            public bool Remove(T item) => throw new NotSupportedException();
            public bool SetEquals(IEnumerable<T> other) {
                return other is null || !other.Any();
            }
            public void SymmetricExceptWith(IEnumerable<T> other) {
            }
            public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static ISet<T> Empty<T>() {
            return EmptySet<T>.Instance;
        }
        public static ISet<T> Of<T>(T val, bool isReadonly = false) {
            if (isReadonly) 
                return Imms.ImmSet.Of(val);
            else
                return new HashSet<T> { val };
        }
    }
}
