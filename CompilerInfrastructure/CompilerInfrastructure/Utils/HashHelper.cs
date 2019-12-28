using System;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public static class HashHelper {
        //static SHA1 sha= SHA1.Create();
        public static ulong UniqueHash(this string str) {
            ulong prime = 1099511628211uL;
            ulong hash = 14695981039346656037uL;
            var bytes = Encoding.UTF8.GetBytes(str);
            foreach (var bt in bytes) {
                hash ^= bt;
                hash *= prime;
            }
            return hash;
        }
        public static unsafe ulong ToBEInt(Span<byte> bts) {
            if (BitConverter.IsLittleEndian) {
                bts.Reverse();
            }
            fixed (byte* ptr = bts) {
                return *(ulong*) ptr;
            }
        }
    }
}
