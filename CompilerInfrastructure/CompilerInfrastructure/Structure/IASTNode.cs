/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Structure.Summaries;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IASTNode /*: IRefEnumerable<IASTNode>*//* : ISummarizable */{

    }
    //public delegate void AstNodeVisitor(ref IASTNode node);
    public static class AstNode {
        /*public static void ForeachDFS(ref IASTNode root, AstNodeVisitor vis) {
            if (root != null) {
                using (var it = root.GetEnumerator()) {
                    while (it.MoveNext()) {
                        ForeachDFS(ref it.Current, vis);
                    }
                }
                vis(ref root);
            }
        }
        public static void ForeachBFS(ref IASTNode root, AstNodeVisitor vis) {
            vis(ref root);
            IRefEnumerable<IASTNode> nodes = root;
            IRefEnumerable<IASTNode> next = null;
            while (!(nodes is null)) {
                using (var it = nodes.GetEnumerator()) {
                    while (it.MoveNext()) {
                        vis(ref it.Current);
                        next = next.Concat(it.Current);
                    }
                    nodes = next;
                    next = null;
                }
            }
        }*/
    }
}
