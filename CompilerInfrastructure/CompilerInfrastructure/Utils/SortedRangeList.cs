using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace CompilerInfrastructure.Utils {
    public class SortedRangeList : IEnumerable<Range> {
        class RangeNode : IEnumerable<Range> {
            internal Range value;
            internal RangeNode left, right;
            internal RangeNode parent;
            internal RangeNode(in Range val) {
                value = val;
            }
            internal RangeNode Find(uint elem) {
                if (value.End > elem) {
                    if (value.Start <= elem) {
                        return this;
                    }
                    else {
                        return left?.Find(elem);
                    }
                }
                else {
                    return right?.Find(elem);
                }
            }
            internal RangeNode Min() {
                if (left is null)
                    return this;
                return left.Min();
            }
            internal RangeNode Max() {
                if (right is null)
                    return this;
                return right.Max();
            }
            internal bool IsRightChild() {
                return parent != null && parent.right == this;
            }
            internal bool IsLeftChild() {
                return parent != null && parent.left == this;
            }
            internal RangeNode NextSibling() {
                if (right != null)
                    return right.Min();
                if (IsLeftChild()) {
                    if (parent.right != null)
                        return parent.right.Min();
                    return parent;
                }
                return null;
            }
            internal RangeNode NextSiblingBelow(RangeNode top) {
                if (right != null)
                    return right.Min();
                if (IsLeftChild()) {
                    if (parent.right != null)
                        return top == this ? null : parent.right.Min();
                    return top == parent ? null : parent;
                }
                return null;
            }
            internal RangeNode PreviousSiblingBelow(RangeNode top) {
                if (left != null)
                    return left.Max();
                if (IsRightChild()) {
                    if (parent.left != null) {
                        return top == this ? null : parent.left;
                    }
                    return parent == top ? null : parent;
                }
                return null;
            }
            internal void Add(RangeNode nod) {

                if (nod.value.Start <= value.Start) {
                    if (left is null) {
                        nod.parent = this;
                        left = nod;
                    }
                    else
                        left.Add(nod);
                }
                else {
                    if (right is null) {
                        nod.parent = this;
                        right = nod;
                    }
                    else
                        right.Add(nod);
                }
            }
            internal void SpecialAdd(RangeNode nod) {
                if (nod.value.Start <= value.Start) {
                    if (left is null) {
                        nod.parent = this;
                        left = nod;
                    }
                    else {
                        if (nod.value.Start > left.value.Start) {
                            nod.left = left;// assuming nod is leaf node
                            left = nod;
                        }
                        else
                            left.Add(nod);
                    }
                }
                else {
                    if (right is null) {
                        nod.parent = this;
                        right = nod;
                    }
                    else {
                        if (nod.value.End <= right.value.End) {
                            nod.right = right;// assuming nod is leaf node
                            right = nod;
                        }
                        else
                            right.Add(nod);
                    }
                }
            }
            public IEnumerator<Range> GetEnumerator() {
                IEnumerable<Range> merge;
                if (left != null)
                    merge = left.Append(value);
                else
                    merge = new[] { value };
                if (right != null)
                    merge = merge.Concat(right);
                return merge.GetEnumerator();
            }
            internal void Dump(IndentionWriter tOut) {
                tOut.WriteLine('{');
                tOut++;
                left?.Dump(tOut);
                tOut.WriteLine(value);
                right?.Dump(tOut);
                tOut--;
                tOut.WriteLine('}');
            }
            internal void RemoveFromParent() {
                if (parent != null) {
                    if (this == parent.left)
                        parent.left = null;
                    else
                        parent.right = null;
                    parent = null;
                }
            }
            internal RangeNode FirstNotChildBiggerThan(RangeNode nod) {
                if (value.Start >= nod.value.End)
                    return this;
                if (right == nod)
                    return null;
                return right.FirstNotChildBiggerThan(nod);
            }
            internal RangeNode FirstNotChildSmallerThan(RangeNode nod) {
                if (value.End <= nod.value.Start)
                    return this;
                if (left == nod)
                    return null;
                return left.FirstNotChildSmallerThan(nod);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        RangeNode root;
        uint minVal, maxVal;
        RangeNode min, max;

        public bool Contains(uint value) {
            if (root is null || value < minVal || value > maxVal)
                return false;
            var left = root.Find(value);
            if (left != null) {
                if (left.value.Contains(value))
                    return true;
                left = left.NextSibling();// only consider next sibling, since otherwise we would have found the next with Find()
                if (left != null && left.value.Contains(value))
                    return true;
            }
            return false;
        }
        public void Add(Range nw) {
            RangeNode FindLeft() {
                var lft = root.Find(nw.Start);
                if (lft is null) {
                    lft = root.Find(nw.Start - 1);
                    if (lft is null) {
                        lft = new RangeNode(new Range(nw.Start, 1));
                        root.Add(lft);
                    }
                }
                return lft;
            }
            RangeNode FindRight() {
                var rgt = root.Find(nw.End - 1);
                if (rgt is null) {
                    rgt = root.Find(nw.End);
                    if (rgt is null) {
                        rgt = new RangeNode(new Range(nw.End - 1, 1));
                        root.Add(rgt);
                    }
                }
                return rgt;
            }
            RangeNode MergeTop(RangeNode nod) {
                if (nod.value.CompletelyOverlaps(root.value)) {

                    return root;
                }
                else if (nod.parent != null) {

                    var par = nod.parent;
                    while (par.parent != null) {
                        if (nod.value.CompletelyOverlaps(par.value)
                            && (par.parent is null || !nod.value.CompletelyOverlaps(par.parent.value))) {

                            return par;
                        }
                        par = par.parent;

                    }
                }
                return null;
            }
            void Merge(RangeNode nod, RangeNode mergeTop) {

                /* if (nod.value.CompletelyOverlaps(root.value)) {
                     nod.parent = null;
                     var ret = root;
                     root = nod;
                     return ret;
                 }
                 else if (nod.parent != null) {

                     var par = nod.parent;
                     while (par.parent != null) {
                         if (nod.value.CompletelyOverlaps(par.value)
                             && (par.parent is null || !nod.value.CompletelyOverlaps(par.parent.value))) {
                             nod.parent = par.parent;
                             if (par.parent != null) {
                                 if (par == par.parent.left) {
                                     par.parent.left = nod;
                                 }
                                 else {
                                     par.parent.right = nod;
                                 }
                             }
                             return par;
                         }
                         par = par.parent;

                     }
                 }
                 return null;*/
                if (mergeTop == root) {
                    root = nod;
                    nod.parent = null;
                }
                else if (mergeTop != null) {
                    if (mergeTop.parent != null) {
                        if (mergeTop == mergeTop.parent.left) {

                            mergeTop.parent.left = nod;
                        }
                        else {
                            mergeTop.parent.right = nod;
                        }
                    }
                }
            }
            if (nw.Empty)
                return;
            if (root is null) {
                root = min = max = new RangeNode(nw);
                minVal = nw.Start;
                maxVal = nw.End - 1;
            }
            else {
                if (nw.Start < minVal) {

                    if (nw.End > maxVal) {
                        root = min = max = new RangeNode(nw);
                        maxVal = nw.End - 1;
                    }
                    else if (nw.End < minVal) {
                        var nod = new RangeNode(nw);
                        min.Add(nod);
                        min = nod;
                    }
                    else {
                        RangeNode nod = FindRight();

                        Range.Create(out nod.value, nw.Start, nod.value.End);
                        var mergeTop = MergeTop(nod);
                        nod.left = null;
                        nod.right = nod.NextSiblingBelow(mergeTop);
                        if (nod.right != null && nod.right != nod)
                            nod.right.parent = nod;
                        Merge(nod, mergeTop);
                        min = nod;
                    }
                    minVal = nw.Start;
                }
                else if (nw.End > maxVal) {

                    RangeNode nod;
                    if (nw.Start > maxVal) {
                        nod = new RangeNode(nw);
                        max.Add(nod);
                    }
                    else {
                        nod = FindLeft();
                        Range.Create(out nod.value, nod.value.Start, nw.End);
                        var mergeTop = MergeTop(nod);
                        nod.right = null;
                        var nwLeft = nod.PreviousSiblingBelow(mergeTop);
                        nod.left = nwLeft;
                        if (nwLeft != nod) {
                            if (nod.left != null && nod.left != nod)
                                nod.left.parent = nod;
                        }
                        Merge(nod, mergeTop);
                    }
                    max = nod;
                    maxVal = nw.End - 1;
                }
                else {
                    RangeNode left, right;
                    left = FindLeft();
                    right = FindRight();

                    Range.Create(out left.value, left.value.Start, right.value.End);
                    if (left != right) {
                        var mergeTop = MergeTop(left);
                        var nwRight = right.NextSiblingBelow(mergeTop);
                        if (nwRight != right) {
                            left.right = nwRight;
                            if (nwRight != null)
                                nwRight.parent = left;
                        }

                        var nwLeft = left.PreviousSiblingBelow(mergeTop);
                        if (nwLeft != left) {

                            left.left = nwLeft;
                            if (left.left != null)
                                left.left.parent = left;
                        }
                        if (right != nwRight)
                            right.RemoveFromParent();
                        Merge(left, mergeTop);
                    }
                }
            }
        }

        public IEnumerator<Range> GetEnumerator() {
            if (root is null)
                return Enumerable.Empty<Range>().GetEnumerator();
            return root.GetEnumerator();
        }
        public void Dump(TextWriter tOut = null) {
            if (tOut is null)
                tOut = Console.Out;
            if (root is null)
                tOut.WriteLine("{ }");
            root.Dump(new IndentionWriter(tOut));
        }
        public string Dumped() {
            var sw = new StringWriter();
            Dump(sw);
            return sw.ToString();
        }
        public bool IsSorted() {
            using (var it = GetEnumerator()) {
                if (it.MoveNext()) {
                    var prev = it.Current;
                    while (it.MoveNext()) {
                        if (it.Current.Start < prev.End)
                            return false;
                        prev = it.Current;
                    }
                }
            }
            return true;
        }
        public bool Validate() {
            if (root is null)
                return true;
            var seen = new HashSet<RangeNode>();
            var stack = new Stack<RangeNode>();

            stack.Push(root);
            seen.Add(root);
            while (stack.TryPop(out var curr)) {
                if (curr.parent != null) {
                    if (curr.parent.left != curr && curr.parent.right != curr)
                        return false;
                    if (curr.parent == curr)
                        return false;
                }
                if (curr.left != null) {
                    if (seen.Add(curr.left))
                        stack.Push(curr.left);
                    else
                        return false;
                }
                if (curr.right != null) {
                    if (seen.Add(curr.right))
                        stack.Push(curr.right);
                    else
                        return false;
                }
            }
            return true;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
