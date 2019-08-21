/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Antlr4.Runtime.Tree;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Parser {
    public static class TreeVisitor {
        public static void Constituents<T>(this ITree root, Action<T> onVisit) where T : ITree {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (onVisit == null)
                throw new ArgumentNullException(nameof(onVisit));
            var nodes = new Stack<ITree>();
            nodes.Push(root);
            while (nodes.TryPop(out var nod)) {
                if (nod is T t) {
                    onVisit(t);
                }
                for (int i = 0; i < nod.ChildCount; ++i) {
                    nodes.Push(nod.GetChild(i));
                }
            }
        }
        public static IEnumerable<T> Constituents<T>(this ITree root) where T : ITree {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            var nodes = new Stack<ITree>();
            nodes.Push(root);
            while (nodes.TryPop(out var nod)) {
                if (nod is T t) {
                    yield return t;
                }
                for (int i = 0; i < nod.ChildCount; ++i) {
                    nodes.Push(nod.GetChild(i));
                }
            }
        }
        public static T Constituents<T>(this ITree root, Func<ITree, bool> cond, Func<ITree, T, T> acc, T start) {
            var nodes = new Stack<ITree>();
            nodes.Push(root);
            while (nodes.TryPop(out var nod)) {
                if (cond(nod)) {
                    start = acc(nod, start);
                }
                for (int i = 0; i < nod.ChildCount; ++i) {
                    nodes.Push(nod.GetChild(i));
                }
            }
            return start;
        }
        public static void Including<T>(this ITree node, Action<T> onVisit) where T : ITree {
            if (onVisit == null)
                throw new ArgumentNullException(nameof(onVisit));
            while (!(node is null)) {
                if (node is T t) {
                    onVisit(t);
                    return;
                }
                node = node.Parent;
            }
            throw new Exception("No including-target found");
        }
        public static T Including<T>(this ITree node, Func<ITree, bool> cond, Func<ITree, T> onVisit) where T : ITree {
            if (onVisit == null)
                throw new ArgumentNullException(nameof(onVisit));
            while (!(node is null)) {
                if (cond(node)) {
                    return onVisit(node);
                }
                node = node.Parent;
            }
            throw new Exception("No including-target found");
        }
        public static bool Including(this ITree node, Func<ITree, bool> cond, Action<ITree> onVisit) {
            if (cond == null)
                throw new ArgumentNullException(nameof(cond));
            if (onVisit == null)
                throw new ArgumentNullException(nameof(onVisit));
            while (!(node is null)) {
                if (cond(node)) {
                    onVisit(node);
                    return true;
                }
                node = node.Parent;
            }
            return false;
        }

    }
}
