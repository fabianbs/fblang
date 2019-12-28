using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Utils {
    public readonly struct Range : IComparable<Range>, IEquatable<Range> {
        public uint Start {
            get;
        }
        public uint Length {
            get;
        }

        public Range(uint start, uint length) {
            Start = start;
            Length = length;

        }

        public bool Empty => Length == 0;
        public uint End => Start + Length;
        public void Deconstruct(out uint start, out uint length) {
            start = Start;
            length = Length;
        }
        /// <summary>
        /// Compares this range with another DISJOINT range
        /// </summary>
        public int CompareTo(Range other) {
            return (int)(Start - (long)other.Start);
        }
        public bool Contains(uint value) {
            return value >= Start && value < End;
        }
        public override bool Equals(object obj) => obj is Range && Equals((Range)obj);
        public bool Equals(Range other) => Start == other.Start && Length == other.Length;
        public override int GetHashCode() => HashCode.Combine(Start, Length);
        public override string ToString() {
            return $"[{Start}, {End})";
        }
        public bool Overlaps(Range other) {
            return End > other.Start && End <= other.End || other.End > Start && other.End <= End;
        }
        public bool CompletelyOverlaps(Range other) {
            return Start <= other.Start && End >= other.End;
        }

        public static bool operator ==(Range range1, Range range2) => range1.Equals(range2);
        public static bool operator !=(Range range1, Range range2) => !(range1 == range2);
        public static Range Create(uint start, uint end) {
            Create(out var ret, start, end);
            return ret;
        }
        public static void Create(out Range ret, uint start, uint end) {
            if (end < start)
                end = start;
            ret = new Range(start, end - start);
        }

        public static bool operator <(Range left, Range right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(Range left, Range right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(Range left, Range right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(Range left, Range right) {
            return left.CompareTo(right) >= 0;
        }
    }
}
