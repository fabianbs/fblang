using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class StringBuilderHelper {
        public static void AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> elems) {
            if (elems is null)
                return;
            using (var it = elems.GetEnumerator()) {
                if (!it.MoveNext())
                    return;
                sb.Append(it.Current);
                while (it.MoveNext()) {
                    sb.Append(separator);
                    sb.Append(it.Current);
                }
            }
        }
    }
}
