using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class StringBuilderHelper {
        /*public static void AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> elems) {
            if (elems is null)
                return;
            using var it = elems.GetEnumerator();
            if (!it.MoveNext())
                return;
            sb.Append(it.Current);
            while (it.MoveNext()) {
                sb.Append(separator);
                sb.Append(it.Current);
            }
        }*/
        /// <summary>
        /// Makes the first letter upper-case
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Capitalize(this string str) {
            if (str is null || str == "" || char.IsUpper(str[0]))
                return str;
            return string.Create(str.Length, str, (arr, old) => {
                old.AsSpan().CopyTo(arr);
                arr[0] = char.ToUpper(arr[0]);
            });
        }
        /// <summary>
        /// Removes all leading whitespaces and then makes the first letter uppercase
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string CapitalizeTrim(this string str) {
            if (string.IsNullOrWhiteSpace(str))
                return str;
            int ind = 0;
            foreach (char c in str) {
                if (!char.IsWhiteSpace(c))
                    break;
                ind++;
            }

            return string.Create(str.Length - ind, (str, ind), (arr, strInd) => {
                strInd.str.AsSpan(strInd.ind).CopyTo(arr);
                arr[0] = char.ToUpper(arr[0]);
            });
        }
    }
}
