using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public class MultipleTextfilesReader : TextReader {
        StreamReader underlying;
        readonly IEnumerator<string> it;
        public MultipleTextfilesReader(IEnumerable<string> filenames) {
            it = filenames.GetEnumerator();
            if (it.MoveNext()) {
                underlying = new StreamReader(it.Current);
            }
            else
                throw new ArgumentException();
        }
        bool MoveNextIfNecessary() {
            bool ret = false;
            while (underlying.Peek() < 0 && it.MoveNext()) {
                ret = true;
                underlying.Dispose();
                underlying = new StreamReader(it.Current);
            }
            return ret;
        }
        public override int Peek() {
            MoveNextIfNecessary();
            return underlying.Peek();
        }

        public override int Read(char[] buffer, int index, int count) {
            count = Math.Min(count, buffer.Length - index);
            var ret = underlying.Read(buffer, index, count);
            while (ret < count) {
                if (MoveNextIfNecessary()) {
                    ret += underlying.Read(buffer, index + ret, count - ret);
                }
                else
                    break;
            }
            return ret;
        }
        public override int Read() {
            Peek();
            return underlying.Read();
        }

       /* public virtual int Read(Span<char> buffer) {
            int count = buffer.Length;
            int tmp;
            var ret = tmp = underlying.Read(buffer, 0, buffer.Length);
            while (ret < count) {
                if (MoveNextIfNecessary()) {
                    buffer = buffer.Slice(tmp);
                    tmp = underlying.Read(buffer);
                    ret += tmp;
                }
                else
                    break;
            }
            return ret;
        }*/
        public override void Close() {
            underlying.Dispose();
            it.Dispose();
            base.Close();
        }
    }
}
