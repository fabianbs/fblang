using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class CoreExtensions {
        /*public static bool TryPop<T>(this Stack<T> stack, out T value) {
            if (stack.Count == 0) {
                value = default;
                return false;
            }
            value = stack.Pop();
            return true;
        }
        public static bool TryAdd<T, U>(this IDictionary<T, U> dic, T ky, U val) {
            if (dic.ContainsKey(ky))
                return false;

            dic.Add(ky, val);
            return true;
        }*/
        /*
        public static Span<T> AsSpan<T>(this T[] arr, int offs) {
            return new Span<T>(arr, offs, Math.Max(0, arr.Length - offs));
        }
        public static Span<T> AsSpan<T>(this T[] arr, int offs, int count) {
            return new Span<T>(arr, offs, Math.Min(count, arr.Length - offs));
        }*/
    }
    /*public static class HashCode {
        public static int Combine(params int[] hcs) {
            return Combine((IEnumerable<int>)hcs);
        }
        public static int Combine(IEnumerable<int> hcs) {
            if (hcs is null || !hcs.Any())
                return 0;
            int ret = 31;
            unchecked {
                foreach (var hc in hcs) {
                    ret += 197 * hc + 103;
                }
            }
            return ret;
        }
        public static int Combine(params object[] objs) {
            return Combine(objs?.Select(x => x != null ? x.GetHashCode() : 0));
        }
        
    }*/ /*
    public readonly struct Span<T> : IEnumerable<T> {
        readonly T[] arr;
        readonly int offs;
        readonly int len;
        public Span(T[] arr, int offset, int length) {
            this.arr = arr;
            offs = Math.Max(0, offset);
            len = Math.Max(0, length);
        }
        public ref T this[int index] {
            get {
                if (index < len)
                    return ref arr[offs + index];
                throw new IndexOutOfRangeException();
            }
        }
        public int Length => len;

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < len; ++i) {
                yield return arr[offs + i];
            }
        }

        public Span<T> Slice(int offset) {
            //TODO safety
            return new Span<T>(arr, offs + offset, len - offset);
        }
        public Span<T> Slice(int offset, int count) {
            //TODO safety
            return new Span<T>(arr, offs + offset, Math.Min(count, len - offset));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public static implicit operator Span<T>(T[] arr) {
            return new Span<T>(arr, 0, arr?.Length ?? 0);
        }


        public ArraySegment<T> AsArraySegment() {
            return new ArraySegment<T>(arr, offs, len);
        }
    }
    public readonly struct ReadOnlySpan<T> : IEnumerable<T> {
        readonly T[] arr;
        readonly int offs;
        readonly int len;
        public ReadOnlySpan(T[] arr, int offset, int length) {
            this.arr = arr;
            offs = Math.Max(0, offset);
            len = Math.Max(0, length);
        }
        public T this[int index] {
            get {
                if (index < len)
                    return arr[offs + index];
                throw new IndexOutOfRangeException();
            }
        }
        public int Length => len;

        public IEnumerator<T> GetEnumerator() {
            for (int i = 0; i < len; ++i) {
                yield return arr[offs + i];
            }
        }

        public Span<T> Slice(int offset) {
            //TODO safety
            return new Span<T>(arr, offs + offset, len - offset);
        }
        public Span<T> Slice(int offset, int count) {
            //TODO safety
            return new Span<T>(arr, offs + offset, Math.Min(count, len - offset));
        }
        public ArraySegment<T> AsArraySegment() {
            return new ArraySegment<T>(arr, offs, len);
        }
        public void CopyTo(Span<T> other) {
            var seg = other.AsArraySegment();
            Array.Copy(arr, offs, seg.Array, seg.Offset, Math.Min(len, seg.Count));
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public static implicit operator ReadOnlySpan<T>(T[] arr) {
            return new ReadOnlySpan<T>(arr, 0, arr?.Length ?? 0);
        }
        public static implicit operator ReadOnlySpan<T>(Span<T> arr) {
            var seg = arr.AsArraySegment();
            return new ReadOnlySpan<T>(seg.Array, seg.Offset, seg.Count);
        }
        public static ReadOnlySpan<T> Empty {
            get => new ReadOnlySpan<T>(Array.Empty<T>(), 0, 0);
        }
    }*/
}
