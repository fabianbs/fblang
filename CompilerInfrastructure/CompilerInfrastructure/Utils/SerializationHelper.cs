using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class SerializationHelper {
        public static T GetT<T>(this SerializationInfo info, string name) {
            return (T)info.GetValue(name, typeof(T));
        }
    }
}
