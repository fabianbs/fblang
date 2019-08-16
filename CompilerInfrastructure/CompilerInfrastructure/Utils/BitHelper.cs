using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class BitHelper {
        public static unsafe long LogicalRightShift(this long l, int sh) {
            ulong ul = *((ulong*) &l);
            ul >>= sh;
            return *((long*) &ul);
        }
    }
}
