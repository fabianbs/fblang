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
using System.Collections.Generic;

namespace CITest {
    public class BitVectorSetTest {
        ITestOutputHelper cout;
        public BitVectorSetTest(ITestOutputHelper _cout) {
            cout = _cout;
        }
        [Fact]
        public void TestSimpleSet() {
            int[] bspSet= { 1,2,4,3,5,6,7,5,8,9,12,13,14,16,15,13,17,16,22 };
            var set = new BitVectorSet<int>(bspSet);
            Assert.Equal(16, set.Count);
            Assert.Contains(16, set);
            Assert.DoesNotContain(26, set);
            //set.ForEach(x => cout.WriteLine(x.ToString()));
            var hs = new HashSet<int>(bspSet);
            foreach(var x in set) {
                if (!hs.Remove(x))
                    Assert.True(false);
            }
            Assert.Empty(hs);
        }
        [Fact]
        public void TestUnion() {
            var set1 = new BitVectorSet<int>(1,2,4,3,5,6,7,5,8,9,12,13,14);
            var set2 = new BitVectorSet<int>(9,12,13,14,16,15,13,17,16,22);
            var set3 = set1 | set2;
            Assert.Equal(16, set3.Count);

            Assert.DoesNotContain(10, set3);
            Assert.DoesNotContain(11, set3);
            Assert.DoesNotContain(21, set3);
            Assert.DoesNotContain(26, set3);
            foreach (var elem in set1) {
                Assert.Contains(elem, set3);
            }
            foreach (var elem in set2) {
                Assert.Contains(elem, set3);
            }
        }
        [Fact]
        public void TestIntersect() {
            var set1 = new BitVectorSet<int>(1,2,4,3,5,6,7,5,8,9,12,13,14);
            var set2 = new BitVectorSet<int>(9,12,13,14,16,15,13,17,16,22);
            var set3 = set1 & set2;
            Assert.Equal(4, set3.Count);
            Assert.DoesNotContain(10, set3);
            foreach (int i in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 15, 16, 17, 22 }) {
                Assert.DoesNotContain(i, set3);
            }
            foreach (int i in new[] { 9, 12, 13, 14 }) {
                Assert.Contains(i, set3);
            }
        }
        [Fact]
        public void TestExcept() {
            var set1 = new BitVectorSet<int>(1,2,4,3,5,6,7,5,8,9,12,13,14);
            var set2 = new BitVectorSet<int>(9,12,13,14,16,15,13,17,16,22);
            var set3 = set1 - set2;
            Assert.Equal(8, set3.Count);
            Assert.DoesNotContain(10, set3);
            foreach (var i in new[] { 1, 2, 3, 4, 5, 6, 7, 8 }) {
                Assert.Contains(i, set3);
            }
            foreach (var i in set2) {
                Assert.DoesNotContain(i, set3);
            }
        }
    }
}
