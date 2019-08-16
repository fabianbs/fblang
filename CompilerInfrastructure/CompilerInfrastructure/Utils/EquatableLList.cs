using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class EquatableLList<T> : IEquatable<IList<T>>, IList<T>, IReadOnlyList<T> {
        readonly EquatableLList<T> next;
        T value;

        public EquatableLList(T val, EquatableLList<T> nxt = null) {
            value = val;
            next = nxt;
            Count = next != null ? next.Count + 1 : 1;
        }

        public T this[int index] {
            get {
                if (index < 0)
                    throw new IndexOutOfRangeException();
                if (index == 0)
                    return value;
                return next != null ? next[index - 1] : throw new IndexOutOfRangeException();
            }
            set {
                if (index < 0)
                    throw new IndexOutOfRangeException();
                if (index == 0)
                    this.value = value;
                if (next != null)
                    next[index - 1] = value;
                else
                    throw new IndexOutOfRangeException();
            }

        }
        public T Value => value;

        public int Count {
            get;
        }
        public bool IsReadOnly {
            get => true;
        }
        public EquatableLList<T> Next {
            get => next;
        }
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) {
            var nod = this;
            do {
                if (Equals(nod.value, item))
                    return true;
                nod = nod.next;
            } while (nod != null);
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            if (arrayIndex >= 0 && arrayIndex < array.Length) {
                array[arrayIndex] = value;
                next?.CopyTo(array, arrayIndex + 1);
            }
        }

        public bool Equals(IList<T> other) {
            if (other is null || other.Count != Count)
                return false;
            else {
                var node = this;
                foreach (var item in other) {
                    if (!Equals(item, node.value))
                        return false;
                    node = node.next;
                    if (node is null)
                        break;
                }
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator() {
            var nod = this;
            do {
                yield return value;
                nod = nod.next;
            } while (nod != null);
        }

        public override int GetHashCode() {
            int hc = Count;
            unchecked {
                var nod = this;
                do {
                    hc += 97 * nod.value.GetHashCode() + 13;
                    nod = nod.next;
                } while (nod != null);
            }
            return hc;
        }

        public int IndexOf(T item) {
            var nod = this;
            int i = 0;
            do {
                if (Equals(nod.value, item))
                    return i;
                nod = nod.next;
                i++;
            } while (nod != null);
            return -1;
        }
        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
