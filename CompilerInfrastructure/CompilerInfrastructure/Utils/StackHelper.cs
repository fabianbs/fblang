using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class StackHelper {
        class StackFrame<T> : IDisposable {
            readonly Stack<T> stack;
            StackFrame(Stack<T> s) {
                stack = s;
            }

            public void Dispose() {
                stack.Pop();
            }

            public static StackFrame<T> Push(Stack<T> s, T val) {
                if (s == null)
                    throw new ArgumentNullException(nameof(s));
                s.Push(val);
                return new StackFrame<T>(s);
            }
        }
        class DisposableConcat : IDisposable {
            private readonly IDisposable d1;
            private readonly IDisposable d2;

            public DisposableConcat(IDisposable _d1, IDisposable _d2) {
                d1 = _d1;
                d2 = _d2;
            }
            public void Dispose() {
                d1.Dispose();
                d2.Dispose();
            }
        }
        public static IDisposable PushFrame<T>(this Stack<T> stack, T val) {
            return StackFrame<T>.Push(stack, val);
        }
        public static IDisposable Combine(this IDisposable d1, IDisposable d2) {
            return new DisposableConcat(d1, d2);
        }
        public static bool TryPeek<T>(this Stack<T> s, out T ret) {
            if (s.Count > 0) {
                ret = s.Peek();
                return true;
            }
            ret = default;
            return false;
        }
        public static bool TryExchangeTop<T>(this Stack<T> s, ref T inout) {
            if (s.TryPop(out var ret)) {
                s.Push(inout);
                inout = ret;
                return true;
            }
            return false;
        }
        public static T ExchangeTop<T>(this Stack<T> s, T nwTop) {
            var ret = s.Pop();
            s.Push(nwTop);
            return ret;
        }

        public static Stack<T> ChainedPush<T>(this Stack<T> s, T val) {
            s.Push(val);
            return s;
        }
        public static Stack<T> ChainedPop<T>(this Stack<T> s, out T val) {
            val = s.Pop();
            return s;
        }
        public static Stack<T> ChainedPeek<T>(this Stack<T> s, out T val) {
            val = s.Peek();
            return s;
        }
    }
}
