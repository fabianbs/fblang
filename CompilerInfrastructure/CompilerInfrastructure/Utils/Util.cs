using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class Util {
        public static bool CompareSelect<T>(T expected, T desired, ref T result) {
            if (Equals(expected, desired)) {
                result = expected;
                return true;
            }
            return false;
        }
    }
}
