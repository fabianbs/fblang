using System;
using Xunit;
using CompilerInfrastructure.Utils;
using CompilerInfrastructure;
using System.Diagnostics;
using Assert = Xunit.Assert;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace CITest {
    public class UtilityTest {
        ITestOutputHelper cout;
        public UtilityTest(ITestOutputHelper _cout) {
            cout = _cout;
        }
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
        [Fact]
        public void TestReinterpretCast() {
            var arr = new StructBox<object>[]{ new object() };
            Console.WriteLine(arr[0].Value.GetAddress());
            var arr2 = Unsafe.As<object[]>(arr);
            Console.Write(arr2[0].GetAddress());
            Assert.Equal(arr[0].Value.GetAddress(), arr2[0].GetAddress());
        }
        [Fact]
        public void TestReinterpretCastAlloc() {
            var arr = new StructBox<Box<int>>[]{ new Box<int>(42) };
            var weak = new WeakReference<Box<int>>(arr[0].Value);
            var heapsz = GC.GetTotalAllocatedBytes(true);
            var arr2 = Unsafe.As<Box<int>[]>(arr);
            var addr1 = arr[0].Value.Value;
            var addr2 = arr2[0].Value;
            arr = null;
            for (int i = 0; i < 10000; ++i) {
                _ = new object[100000];
            }
            GC.Collect();
            var res =  GC.WaitForFullGCComplete(5000);

            var nwHeapsz = GC.GetTotalAllocatedBytes(true);
            cout.WriteLine(res.ToString());

            cout.WriteLine("OldBytes: {0}; NewBytes: {1}; Diff: {2}", heapsz, nwHeapsz, heapsz - nwHeapsz);
            Assert.True(weak.TryGetTarget(out var obj));
            var addr3 = obj.Value;
            Assert.Equal(addr1, addr2);
            Assert.Equal(addr2, addr3);
        }
        [Fact]
        public void TestReinterpretCastType() {
            var arr = new Box<int>[]{ new Box<int>(42) };
            var arr2 = Unsafe.As<StructBox<Box<int>>[]>(arr);
            var ty1 = arr.GetType();
            var ty2 = arr2.GetType();
            cout.WriteLine(ty1 + " <=> " + ty2);
            arr[0] = new Box<int>(42);
            arr2[0] = new Box<int>(24);

        }
        [Fact]
        public void TestReinterpretCastCloneType() {
            var arr = new Box<int>[]{ new Box<int>(42) };
            var arr2 = Unsafe.As<StructBox<Box<int>>[]>(arr);
            var arr3 = arr2.Clone();
            Assert.Equal(typeof(Box<int>[]), arr3.GetType());
        }
        [Fact]
        public void TestVector1() {
            Vector<Box<int>> vec = default;
            vec.AddRange(new Box<int>(2), new Box<int>(3));
            vec.Add(new Box<int>(5));
            string s = "";
            foreach (var x in vec) {
                if (x != null)
                    s += x.Value.ToString();
            }
            Assert.Equal("235", s);
            vec[1] = new Box<int>(4);
            vec[5] = new Box<int>(6);
            s = "";
            foreach (var x in vec) {
                if (x != null)
                    s += x.Value.ToString();
            }
            Assert.Equal("2456", s);
            Assert.Null(vec[4]);
            Assert.Null(vec[3]);
        }
    }
}
