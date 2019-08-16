using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure {
    [Serializable]
    public class EquatableCollection<T> : IReadOnlyList<T>, IEquatable<IReadOnlyList<T>> {
        readonly IReadOnlyList<T> underlying;

        public EquatableCollection(uint count) {
            underlying = new T[count];
        }
        public EquatableCollection(IReadOnlyList<T> values) {
            underlying = values ?? List.Empty<T>();
        }
        public EquatableCollection(EquatableCollection<T> toCopy)
            : this((uint)(toCopy != null ? toCopy.Count : 0)) {
            toCopy?.CopyTo((T[])underlying, 0);
        }

        public T this[int index] {
            get {
                return underlying[index];
            }
        }

        public int Count => underlying.Count;

        public bool Equals(IReadOnlyList<T> other) {
            if (other is null || other.Count != Count)
                return false;
            for (int i = 0; i < Count; ++i) {
                if (!Equals(this[i], other[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj) => obj is IReadOnlyList<T> rol && Equals(rol);
        public override int GetHashCode() {
            int hc = Count;
            unchecked {
                foreach (var x in underlying) {
                    hc += 97 * x.GetHashCode() + 13;
                }
            }
            return hc;
        }

        public IEnumerator<T> GetEnumerator() => underlying.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) {
            for (int i = 0; i < underlying.Count; ++i) {
                if (Equals(item, underlying[i]))
                    return i;
            }return -1;
        }

        public bool Clear() {
            if (underlying is Array arr) {
                Array.Clear(arr, 0, arr.Length);
                return true;
            }
            return false;
        }
        public bool Contains(T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex) {
            int k = 0;
            for (int i = arrayIndex; i < array.Length; ++i, ++k) {
                array[i] = underlying[k];
            }
        }

        /*#region Not supported IList-methods
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        #endregion*/

        public static implicit operator EquatableCollection<T>(T[] arr) {
            return new EquatableCollection<T>((IReadOnlyList<T>)arr);
        }
    }
    public static class EquatableCollection {
        public static EquatableCollection<T> FromIList<T>(IReadOnlyList<T> lst) {
            if (lst is EquatableCollection<T> eqc) {
                return eqc;
            }
            else {
                return new EquatableCollection<T>(lst);
            }
        }
    }
}
