using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    [Serializable]
    public class Box<T> : IEquatable<Box<T>> where T : struct {
        public delegate void RefConsumer(ref T val);
        public delegate U RefFunction<U>(ref T val);
        T value;
        public bool HasValue { get; }
        public ref T Value => ref value;

        public bool DoIfPresent(RefConsumer rc) {
            if (HasValue && rc != null) {
                rc(ref value);
                return true;
            }
            return false;
        }
        public bool DoIfPresent<U>(RefFunction<U> fn, out U ret) {
            if (HasValue && fn != null) {
                ret = fn(ref value);
                return true;
            }
            ret = default;
            return false;
        }
        public Box() {
            HasValue = false;
            value = default;
        }
        public Box(in T val) {
            HasValue = true;
            value = val;
        }
        public override int GetHashCode() => HasValue ? value.GetHashCode() : base.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as Box<T>);
        public bool Equals(Box<T> other) {
            if (other is null || HasValue != other.HasValue)
                return false;
            if (HasValue) {
                return EqualityComparer<T>.Default.Equals(value, other.value);
            }
            else
                return ReferenceEquals(other, this);
        }
    }
    [Serializable]
    public sealed class ReadOnlyBox<T> : IEquatable<ReadOnlyBox<T>> where T : struct {
        public delegate void RefConsumer(in T val);
        public delegate U RefFunction<U>(in T val);

        readonly T value;
        public bool HasValue { get; }
        public ref readonly T Value => ref value;

        public bool DoIfPresent(RefConsumer rc) {
            if (HasValue && rc != null) {
                rc(value);
                return true;
            }
            return false;
        }
        public bool DoIfPresent<U>(RefFunction<U> fn, out U ret) {
            if (HasValue && fn != null) {
                ret = fn(value);
                return true;
            }
            ret = default;
            return false;
        }
        public ReadOnlyBox() {
            HasValue = false;
            value = default;
        }
        public ReadOnlyBox(in T val) {
            HasValue = true;
            value = val;
        }
        public override int GetHashCode() => HasValue ? value.GetHashCode() : base.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as Box<T>);
        public bool Equals(ReadOnlyBox<T> other) {
            if (other is null || HasValue != other.HasValue)
                return false;
            if (HasValue) {
                return EqualityComparer<T>.Default.Equals(value, other.value);
            }
            else
                return ReferenceEquals(other, this);
        }
    }
    public static class BoxHelper {
        public static Box<T> Box<T>(this ref T val) where T : struct {
            return new Box<T>(val);
        }
        public static ReadOnlyBox<T> BoxReadOnly<T>(this ref T val) where T : struct {
            return new ReadOnlyBox<T>(val);
        }
        
    }
}
