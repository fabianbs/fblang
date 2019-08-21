/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Semantics {

    public class CFGNode {
        internal protected enum State : byte {
            UNDEFINED, TRUE, FALSE
        }

        readonly ISet<CFGNode> next;
        State state;
        public CFGNode(bool terminator, params CFGNode[] previous)
            : this(terminator, (IEnumerable<CFGNode>)previous) {

        }
        public CFGNode(bool terminator, IEnumerable<CFGNode> previous) {
            next = new HashSet<CFGNode>();
            state = terminator ? State.TRUE : State.UNDEFINED;
            if (previous != null) {
                foreach (var pre in previous) {
                    if (pre != null)
                        pre.next.Add(this);
                }
            }
        }
        void Dump(IndentionWriter tw) {
            tw.Write(state);
            tw.WriteLine(" {");
            tw++;
            foreach (var nxt in next) {
                nxt.Dump(tw);
            }
            tw--;
            tw.WriteLine("}");
        }
        public void Dump(TextWriter _tw) {
            var tw = new IndentionWriter(_tw);
            Dump(tw);
        }
        public bool IsTerminated => state == State.TRUE;
        protected internal State CurrentState => state;
        public bool AllPathsTerminate() {
            if (state == State.UNDEFINED) {
                bool hasTrue = false;
                foreach (var nxt in next) {
                    hasTrue = nxt.AllPathsTerminate();

                    if (!hasTrue) {// There is one path which does not return
                        break;
                    }
                }

                state = hasTrue ? State.TRUE : State.FALSE;
            }
            return state == State.TRUE;
        }
        public bool AllPathsTerminate(out CFGNode notReturning) {
            if (state == State.UNDEFINED) {
                bool hasTrue = false;
                notReturning = null;
                foreach (var nxt in next) {
                    hasTrue = nxt.AllPathsTerminate();

                    if (!hasTrue) {// There is one path which does not return
                        notReturning = nxt;
                        break;
                    }
                }

                state = hasTrue ? State.TRUE : State.FALSE;
                return hasTrue;
            }
            else if (state == State.FALSE) {
                notReturning = this;
                return false;
            }
            else {
                notReturning = null;
                return true;
            }
        }
    }
}
