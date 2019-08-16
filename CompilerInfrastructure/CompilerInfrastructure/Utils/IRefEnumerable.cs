using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public interface IRefEnumerable<T> : IEnumerable<T> {
        new IRefEnumerator<T> GetEnumerator();
    }
    public interface IRefEnumerator<T> : IEnumerator<T> {
        new ref T Current {
            get;
        }
    }
    public static class RefEnumerable {
        class ConcatRefEnumerable<T> : IRefEnumerable<T> {
            class ConcatRefEnumerator : IRefEnumerator<T> {
                private readonly IRefEnumerable<T> first;
                private readonly IRefEnumerable<T> second;
                private IRefEnumerator<T> curr = null;
                private int complete = 0;
                public ConcatRefEnumerator(IRefEnumerable<T> first, IRefEnumerable<T> second) {
                    this.first = first;
                    this.second = second;
                }

                public ref T Current {
                    get => ref curr.Current;
                }
                object IEnumerator.Current => Current;
                T IEnumerator<T>.Current => Current;

                public void Dispose() {
                    curr?.Dispose();
                }
                public bool MoveNext() {
                    if (curr == null) {
                        if (complete == 0) {
                            curr = first.GetEnumerator();
                        }
                        else if (complete == 1) {
                            curr = second.GetEnumerator();
                        }
                        else
                            return false;
                    }
                    if (curr.MoveNext())
                        return true;
                    curr.Dispose();
                    curr = null;
                    if (complete++ == 0) {
                        return MoveNext();
                    }
                    else
                        return false;
                }
                public void Reset() {
                    curr?.Dispose();
                    curr = null;
                    complete = 0;
                }
            }

            private readonly IRefEnumerable<T> first;
            private readonly IRefEnumerable<T> second;

            public ConcatRefEnumerable(IRefEnumerable<T> first, IRefEnumerable<T> second) {
                this.first = first;
                this.second = second;
            }

            public IRefEnumerator<T> GetEnumerator() {
                return new ConcatRefEnumerator(first, second);
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        class EmptyRefEnumerable<T> : IRefEnumerable<T> {
            public IRefEnumerator<T> GetEnumerator() => RefEnumerator.Empty<T>();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public static EmptyRefEnumerable<T> Instance {
                get;
            } = new EmptyRefEnumerable<T>();
        }
        class ArrayRefEnumerable<T> : IRefEnumerable<T> {
            readonly T[] arr;
            public ArrayRefEnumerable(T[] arr) {
                this.arr = arr;
            }

            public IRefEnumerator<T> GetEnumerator() => RefEnumerator.FromArray(arr);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static IRefEnumerable<T> Concat<T>(this IRefEnumerable<T> @this, IRefEnumerable<T> other) {
            if (@this is null)
                return other;
            if (other is null)
                return @this;
            return new ConcatRefEnumerable<T>(@this, other);
        }
        public static IRefEnumerable<T> FromArray<T>(T[] arr) {
            return new ArrayRefEnumerable<T>(arr);
        }
        public static IRefEnumerable<T> Empty<T>() {
            return EmptyRefEnumerable<T>.Instance;
        }
        public static ref T First<T>(this IRefEnumerable<T> it) {
            using (var itit = it.GetEnumerator()) {
                if (itit.MoveNext()) {
                    return ref itit.Current;
                }
                throw new IndexOutOfRangeException();
            }
        }
        public static ref T ElementAt<T>(this IRefEnumerable<T> it, int n) {
            using (var itit = it.GetEnumerator()) {

                for (int i = 0; i <= n; ++i) {
                    if (!itit.MoveNext())
                        throw new IndexOutOfRangeException();
                }
                return ref itit.Current;
            }
        }
    }
    public static class RefEnumerator {
        class EmptyRefEnumerator<T> : IRefEnumerator<T> {
             T value;
            public ref T Current {
                get => ref value;
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;

            public void Dispose() {
            }
            public bool MoveNext() => false;
            public void Reset() {
            }
        }
        class CombineRefEnumerator<T> : IRefEnumerator<T> {
            private readonly IEnumerable<IRefEnumerable<T>> it;
            private IRefEnumerator<T> curr = null;
            IEnumerator<IRefEnumerable<T>> itit;

            public CombineRefEnumerator(IEnumerable<IRefEnumerable<T>> it) {
                this.it = it ?? Array.Empty<IRefEnumerable<T>>();
                itit = it.GetEnumerator();
            }

            public ref T Current {
                get => ref curr.Current;
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;

            public void Dispose() {
                curr?.Dispose();
                itit.Dispose();
            }

            public bool MoveNext() {
                if (curr == null) {
                    if (itit.MoveNext()) {
                        curr = itit.Current.GetEnumerator();
                    }
                    else
                        return false;
                }

                if (curr.MoveNext())
                    return true;

                curr = null;
                return MoveNext();
            }
            public void Reset() {
                curr?.Dispose();
                curr = null;
                itit.Dispose();
                itit = it.GetEnumerator();
            }
        }
        class FirstRefEnumerator<T> : IRefEnumerator<T> {
            private readonly IRefEnumerable<T> it;
            private IRefEnumerator<T> curr = null;
            bool complete = false;
            public FirstRefEnumerator(IRefEnumerable<T> it) {
                this.it = it ?? throw new ArgumentNullException(nameof(it));
            }

            public ref T Current {
                get => ref curr.Current;
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;
            public void Dispose() {
                curr?.Dispose();
            }
            public bool MoveNext() {
                if (complete)
                    return false;
                if (curr == null) {
                    curr = it.GetEnumerator();
                }
                complete = true;
                return curr.MoveNext();
            }
            public void Reset() {
                curr?.Dispose();
                curr = null;
                complete = false;
            }
        }
        class SingleRefEnumerator<T> : IRefEnumerator<T> {
            private readonly SingleCreator<T> creator;
            bool has = false;
            public SingleRefEnumerator(SingleCreator<T> creator) {
                this.creator = creator ?? throw new ArgumentNullException(nameof(creator));
            }

            public ref T Current {
                get => ref creator();
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;
            public void Dispose() {
            }
            public bool MoveNext() {
                if (!has) {
                    return has = true;
                }
                return false;
            }
            public void Reset() {
                has = false;
            }
        }
        class ArrayRefEnumerator<T> : IRefEnumerator<T> {
            readonly T[] arr;
            int i = 0;
            bool started = false;
            public ArrayRefEnumerator(T[] arr) {
                this.arr = arr ?? Array.Empty<T>();
            }
            public ref T Current {
                get => ref arr[i];
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;
            public void Dispose() {
            }
            public bool MoveNext() {
                if (!started) {
                    started = true;
                    i = -1;
                }
                return ++i < arr.Length;
            }
            public void Reset() {
                started = false;
            }
        }
        class TakeRefEnumerator<T> : IRefEnumerator<T> {
            readonly IRefEnumerator<T> underlying;
            private int num = 0;
            private readonly int count;

            public TakeRefEnumerator(IRefEnumerator<T> other, int count) {
                underlying = other ?? Empty<T>();
                this.count = count;
            }
            public ref T Current {
                get => ref underlying.Current;
            }
            object IEnumerator.Current {
                get => Current;
            }
            T IEnumerator<T>.Current => Current;
            public void Dispose() {
            }
            public bool MoveNext() {
                if (num < count) {
                    num++;
                    return underlying.MoveNext();
                }
                return false;
            }
            public void Reset() {
                underlying.Reset();
                num = 0;
            }
        }

        public static IRefEnumerator<T> Empty<T>() {
            return new EmptyRefEnumerator<T>();
        }

        public static IRefEnumerator<T> ConcatMany<T>(params IRefEnumerable<T>[] it) {
            return ConcatMany((IEnumerable<IRefEnumerable<T>>)it);
        }
        public static IRefEnumerator<T> ConcatMany<T>(IEnumerable<IRefEnumerable<T>> it) {
            return new CombineRefEnumerator<T>(it);
        }
        public static IRefEnumerator<T> First<T>(IRefEnumerable<T> it) {
            return new FirstRefEnumerator<T>(it);
        }
        public delegate ref T SingleCreator<T>();
        public static IRefEnumerator<T> Single<T>(SingleCreator<T> creator) {
            return new SingleRefEnumerator<T>(creator);
        }
        public static IRefEnumerator<T> FromArray<T>(T[] arr) {
            return new ArrayRefEnumerator<T>(arr);
        }
        public static IRefEnumerator<T> Take<T>(this IRefEnumerator<T> it, int count) {
            return new TakeRefEnumerator<T>(it, count);
        }
    }
}
