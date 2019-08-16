using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CompilerInfrastructure.Utils {
    [Serializable]
    public struct ValueLazy<T> : ISerializable {
        T value;
        bool hasValue;
        readonly Func<T> generator;
        public ValueLazy(Func<T> _generator) {
            generator = _generator ?? throw new ArgumentNullException(nameof(_generator));
            hasValue = false;
            value = default;
        }
        public ValueLazy(T _value) {
            generator = null;
            hasValue = true;
            value = _value;
        }
        public ValueLazy(SerializationInfo info, StreamingContext context) {
            value = (T)info.GetValue(nameof(value), typeof(T));
            hasValue = true;
            generator = null;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(value), StoreAndWrap(generator ?? (() => default(T))));
        }
        public bool IsInitialized => hasValue || generator != null;
        public T Value {
            get {
                if (!hasValue) {
                    hasValue = true;
                    value = generator();
                }
                return value;
            }
        }
        public T StoreAndWrap(Func<T> _generator) {
            if (!hasValue) {
                hasValue = true;
                value = _generator();
            }
            return value;
        }




        public static implicit operator ValueLazy<T>(T val) {
            return new ValueLazy<T>(val);
        }
    }
}
