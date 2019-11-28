﻿using System;
using System.Collections.Generic;
using System.Text;
using BitVector = CompilerInfrastructure.Utils.Vector<ulong>;
namespace CompilerInfrastructure.Utils {
    public static class BitVectorHelper {
        public static bool Get(this BitVector vec, uint idx) {
            uint index = idx >> 6;
            uint offset = 1u << (int)(index & 63);
            return (vec[index] & offset) != 0;
        }
        static byte BitCount(this ulong value) {
            //see "https://stackoverflow.com/questions/6097635/checking-cpu-popcount-from-c-sharp?noredirect=1"
            ulong result = value - ((value >> 1) & 0x5555555555555555UL);
            result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
            return (byte) (unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }
        public static uint PopCount(this BitVector vec) {
            uint ret = 0;
            foreach(var x in vec) {
                ret += BitCount(x);
            }
            return ret;
        }
        public static uint PopCount(this Span<ulong> vec) {
            return PopCount((ReadOnlySpan<ulong>) vec);
        }
        public static uint PopCount(this ReadOnlySpan<ulong> vec) {
            uint ret = 0;
            foreach (var x in vec) {
                ret += BitCount(x);
            }
            return ret;
        }
        public static void Set(this BitVector vec, uint idx, bool value) {
            uint index = idx >> 6;
            uint offset = 1u << (int)(index & 63);
            if (value)
                vec[index] |= offset;
            else
                vec[index] &= ~offset;
        }
        public static void Set(this BitVector vec, uint idx) {
            uint index = idx >> 6;
            uint offset = 1u << (int)(index & 63);
            vec[index] |= offset;
        }
        public static void Unset(this BitVector vec, uint idx) {
            uint index = idx >> 6;
            uint offset = 1u << (int)(index & 63);
            vec[index] &= ~offset;
        }
        public static bool Exchange(this BitVector vec, uint idx, bool nwVal) {
            uint index = idx >> 6;
            uint offset = 1u << (int)(index & 63);
            bool ret = (vec[index] & offset) != 0;
            if (nwVal != ret) {
                if (nwVal)
                    vec[index] |= offset;
                else
                    vec[index] &= ~offset;
            }
            return ret;
        }
        public static IEnumerable<uint> GetSetPositions(this BitVector vec) {
            uint index = 0;
            const int sz = sizeof(long) << 3;
            foreach (var x in vec) {
                for (int i = 0; i < sz; ++i, ++index) {
                    if ((x & (1ul << i)) != 0)
                        yield return index;
                }
            }
        }
        public static void And(this BitVector vec, BitVector vec2) {
            var len = Math.Min(vec.Length, vec2.Length);
            for (uint i = 0; i < len; ++i) {
                vec[i] &= vec2[i];
            }
            for (uint i = len; i < vec.Length; ++i) {
                vec[i] = 0;
            }
        }
        public static void AndNot(this BitVector vec, BitVector vec2) {
            var len = Math.Min(vec.Length, vec2.Length);
            for (uint i = 0; i < len; ++i) {
                vec[i] &= ~vec2[i];
            }
            for (uint i = len; i < vec.Length; ++i) {
                vec[i] = 0;
            }
        }
        public static void Or(this ref BitVector vec, BitVector vec2) {
            var len = Math.Max(vec.Length, vec2.Length);
            ref var dummy = ref vec[len-1];// resize vec
            for (uint i = 0; i < len; ++i) {
                vec[i] |= vec2[i];
            }
        }
        public static void Xor(this ref BitVector vec, BitVector vec2) {
            var len = Math.Max(vec.Length, vec2.Length);
            ref var dummy = ref vec[len-1];// resize vec
            for (uint i = 0; i < len; ++i) {
                vec[i] ^= vec2[i];
            }
        }
    }
}
