using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class Assert {
        [Conditional("DEBUG")]
#pragma warning disable IDE1006 // Benennungsstile
        public static void assert<T>(T value, string message) {
#pragma warning restore IDE1006 // Benennungsstile
            Debug.Assert(!Equals(value, default), message);
        }
        [Conditional("DEBUG")]
#pragma warning disable IDE1006 // Benennungsstile
        public static void assert<T>(T value) {
#pragma warning restore IDE1006 // Benennungsstile
            Debug.Assert(!Equals(value, default));
        }
    }
}
