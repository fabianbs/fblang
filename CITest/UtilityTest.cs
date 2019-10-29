using System;
using Xunit;
using CompilerInfrastructure.Utils;
using CompilerInfrastructure;
using System.Diagnostics;
using Assert = Xunit.Assert;
using System.Linq;

namespace CITest {
    public class UtilityTest {
        [Fact]
        public void TestArrayHashCode() {
            var arr = new []{ 1, 3, 5, 7, 9, 11};
            var hc = arr.GetArrayHashCode();
            Assert.NotEqual(0, hc);
        }
        [Fact]
        public void TestNullArrayHashCode() {
            int [] arr = null;
            var hc = arr.GetArrayHashCode();
            Assert.Equal(0, hc);
        }
        [Fact]
        public void TestSingeltonArrayHashCode() {
            var arr = new []{ 1 };
            var hc = arr.GetArrayHashCode();
            var vectorlen = System.Numerics.Vector<int>.Count;

            var _31 = 31<<24|31<<16|31<<8|31;
            var _48 = BitConverter.IsLittleEndian
                ? 31<<24|31<<16|31<<8|48
                : 48<<24|31<<16|31<<8|31;
            var expected = _48 + (vectorlen-1) * _31;
            Assert.Equal(expected, hc);
        }

    }
}
