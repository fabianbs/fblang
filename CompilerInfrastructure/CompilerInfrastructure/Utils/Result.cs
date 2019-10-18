using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public readonly struct Result<T, U> {
        readonly T resultValue;

        public Result(T result) {
            IsSuccess = true;
            resultValue = result;
            Failure = default;
        }
        public Result(U failure) {
            IsSuccess = false;
            resultValue = default;
            Failure = failure;
        }
        public bool IsSuccess {
            get;
        }
        public bool TryGetResult(out T res) {
            if (IsSuccess) {
                res = resultValue;
                return true;
            }
            else {
                res = default;
                return false;
            }
        }
        public bool Get(out T result, out U failure) {
            if (IsSuccess) {
                result = resultValue;
                failure = default;
                return true;
            }
            else {
                result = default;
                failure = Failure;
                return false;
            }
        }
        public T GetOrElse(T dflt = default) {
            return IsSuccess ? resultValue : dflt;
        }

        public U Failure {
            get;
        }
        public static bool operator true(Result<T, U> res) {
            return res.IsSuccess;
        }
        public static bool operator false(Result<T, U> res) {
            return !res.IsSuccess;
        }
        public static implicit operator Result<T, U>(T res) {
            return new Result<T, U>(res);
        }
        public static implicit operator Result<T, U>(U fail) {
            return new Result<T, U>(fail);
        }
        public void Deconstruct(out T result, out U failure) {
            result = resultValue;
            failure = Failure;
        }

    }
    public readonly struct BooleanResult<U> : IEquatable<BooleanResult<U>> {
        private readonly U failure;

        public BooleanResult(bool _result, U _failure) {
            IsSuccess = _result;
            failure = _failure;
        }
        public static bool operator true(BooleanResult<U> res) => res.IsSuccess;
        public static bool operator false(BooleanResult<U> res) => !res.IsSuccess;
        public static implicit operator BooleanResult<U>(bool val) => new BooleanResult<U>(val, default);
        public static implicit operator BooleanResult<U>(U fail) => new BooleanResult<U>(false, fail);
        public static implicit operator BooleanResult<U>((bool, U) res) => new BooleanResult<U>(res.Item1, res.Item2);
        public bool IsSuccess {
            get;
        }
        public U Failure => IsSuccess ? default : failure;
        public bool Get(out U fail) {
            fail = Failure;
            return IsSuccess;
        }

        public override bool Equals(object obj) => obj is BooleanResult<U> result && Equals(result);
        public bool Equals(BooleanResult<U> other) => IsSuccess == other.IsSuccess && EqualityComparer<U>.Default.Equals(Failure, other.Failure);
        public override int GetHashCode() => HashCode.Combine(IsSuccess, Failure);

        /// <summary>
        /// Replace the failure with new reason
        /// </summary>
        public static BooleanResult<U> operator |(BooleanResult<U> bl, U fail) {
            if (!bl.Get(out _))
                return fail;
            return true;
        }
        public static BooleanResult<U> operator &(BooleanResult<U> bl, BooleanResult<U> res) {
            if (bl)
                return res;
            else
                return bl;
        }

        public static bool operator ==(BooleanResult<U> left, BooleanResult<U> right) => left.Equals(right);
        public static bool operator !=(BooleanResult<U> left, BooleanResult<U> right) => !(left == right);
    }
}
