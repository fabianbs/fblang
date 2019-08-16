using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class IndentionWriter {
        readonly TextWriter tw;
        int indent = 0;
        bool linestart = true;
        public IndentionWriter(TextWriter tOut) {
            tw = tOut ?? throw new ArgumentNullException(nameof(tOut));
        }
        public void WriteLine(object o) {
            if (linestart && indent > 0)
                tw.Write(new string('\t', indent));
            tw.WriteLine(o);
            linestart = true;
        }
        public void WriteLine() {
            if (linestart && indent > 0)
                tw.WriteLine(new string('\t', indent));
            else
                tw.WriteLine();
            linestart = true;
        }
        public void Write(object o) {
            if (linestart && indent > 0) {
                tw.Write(new string('\t', indent));
            }
            linestart = false;
            tw.Write(o);
        }
        public static IndentionWriter operator ++(IndentionWriter iw) {
            if (iw != null) {
                iw.indent++;
            }
            return iw;
        }
        public static IndentionWriter operator --(IndentionWriter iw) {
            if (iw != null && iw.indent > 0) {
                iw.indent--;
            }
            return iw;
        }
    }
}
