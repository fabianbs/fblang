using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public struct StructBox <T>{
        public T Value;
        public StructBox(T val) { Value = val; }
        public static implicit operator T(StructBox<T> box) {
            return box.Value;
        }
        public static implicit operator StructBox<T>(T val) {
            return new StructBox<T>(val);
        }
    }
}
