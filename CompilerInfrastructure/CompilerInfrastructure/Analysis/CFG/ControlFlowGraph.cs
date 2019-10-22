using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using CMethodImplAttribute = System.Runtime.CompilerServices.MethodImplAttribute;
using static System.Runtime.CompilerServices.MethodImplOptions;
using System.Linq;

namespace CompilerInfrastructure.Analysis.CFG {
    public class ControlFlowGraph {
        LazyDictionary<IDeclaredMethod, ICFGNode> cache;
        Stack<(ICFGNode continueTarget, ICFGNode breakTarget)> controlRedirectTarget
            = new Stack<(ICFGNode continueTarget, ICFGNode breakTarget)>();
        public ControlFlowGraph() {
            cache = new LazyDictionary<IDeclaredMethod, ICFGNode>(CreateInternal);
            controlRedirectTarget.Push((null, null));
        }
        public ICFGNode Create(IDeclaredMethod met) {
            return cache[met];
        }
        ICFGNode CreateInternal(IDeclaredMethod met) {
            if (met is null || !met.Body.HasValue)
                return null;
            var (ret, _) = CreateInternal(null, met.Body.Instruction);
            return ret;
        }
        IDisposable Push(ICFGNode contTar, ICFGNode breakTar) {
            return controlRedirectTarget.PushFrame((contTar, breakTar));
        }
        IDisposable PushBreak(ICFGNode breakTar) {
            var contTar = controlRedirectTarget.Peek().continueTarget;
            return controlRedirectTarget.PushFrame((contTar, breakTar));
        }
        [CMethodImpl(AggressiveInlining)]
        static void Connect(ICFGNode start, ICFGNode exit) {
            if (start is null || exit is null || start.IsTerminatingNode)
                return;
            start.Next.Add(exit);
            exit.Previous.Add(start);
        }
        static void ConnectTerminator(ICFGNode start, ICFGNode exit) {
            if (start is null || exit is null || start.IsExitNode)
                return;
            start.Next.Add(exit);
            exit.Previous.Add(start);
        }
        [CMethodImpl(AggressiveInlining)]
        static void Connect(ICFGNode start, IEnumerable<ICFGNode> exits) {
            foreach (var nod in exits) {
                Connect(start, nod);
            }
        }
        static void Connect(IEnumerable<ICFGNode> starts, ICFGNode exit) {
            foreach (var nod in starts) {
                Connect(nod, exit);
            }
        }
        static void Connect(IEnumerable<ICFGNode> starts, IEnumerable<ICFGNode> exits) {
            foreach (var nod in starts) {
                Connect(nod, exits);
            }
        }
        (ICFGNode start, ICFGNode exit) CreateInternal(ICFGNode prev, IExpression expr) {
            //TODO
            throw new NotImplementedException();
        }
        /* (ISet<ICFGNode> start, ISet<ICFGNode> exit) CreateInternal(IEnumerable<ICFGNode> prev, IExpression expr) {
             HashSet<ICFGNode>
                 start = new HashSet<ICFGNode>(),
                 exit = new HashSet<ICFGNode>();
             foreach (var pre in prev) {
                 var (st, ex) = CreateInternal(pre, expr);
                 start.Add(st);
                 exit.UnionWith(ex);
             }
             return (start, exit);
         }
         (ISet<ICFGNode> start, ISet<ICFGNode> exit) CreateInternal(IEnumerable<ICFGNode> prev, IStatement stmt) {
             HashSet<ICFGNode>
                 start = new HashSet<ICFGNode>(),
                 exit = new HashSet<ICFGNode>();
             foreach (var pre in prev) {
                 var (st, ex) = CreateInternal(pre, stmt);
                 start.Add(st);
                 exit.UnionWith(ex);
             }
             return (start, exit);
         }*/
        static ICFGNode MergeStartNodes(IEnumerable<ICFGNode> prev) {
            if (prev.Any()) {
                if (prev.HasCount(2)) {
                    var nop = new NopCFGNode(prev.First().Position);
                    Connect(prev, nop);
                    return nop;
                }
                else
                    return prev.First();
            }
            else
                return null;
        }

        (ICFGNode start, ICFGNode exit) CreateInternal(ICFGNode prev, IStatement stmt) {
            switch (stmt) {
                case BlockStatement block: {
                    if (block.Statements.Length == 0) {
                        var nopStart = new NopCFGNode(prev?.Position ?? default);
                        var nopEnd = new NopCFGNode(prev?.Position ?? default);
                        Connect(prev, nopStart);
                        Connect(nopStart, nopEnd);

                        return (nopStart, nopEnd);
                    }
                    var (start, exit) = CreateInternal(prev, block.Statements[0]);
                    foreach (var subStmt in block.Statements.AsSpan(1)) {
                        (_, exit) = CreateInternal(exit, subStmt);
                    }
                    return (start, exit);
                }
                case ReturnStatement retStmt: {
                    //var start = MergeStartNodes(prev);
                    var retNode = new StatementCFGNode(retStmt, exit: true);
                    Connect(prev, retNode);
                    return (retNode, retNode);
                }
                case YieldStatement yieldStmt: {
                    //var start = MergeStartNodes(prev);
                    var yieldNode = new StatementCFGNode(yieldStmt);
                    Connect(prev, yieldNode);
                    return (yieldNode, yieldNode);
                }
                case BreakStatement breakStmt: {
                    var (_, breakTar) = controlRedirectTarget.Peek();
                    var breakNode= new StatementCFGNode(breakStmt, terminator: true, exit: breakTar is null);
                    Connect(prev, breakNode);
                    ConnectTerminator(breakNode, breakTar);
                    return (breakNode, breakNode);
                }
                case ContinueStatement continueStmt: {
                    var (contTar, _) = controlRedirectTarget.Peek();
                    var contNode = new StatementCFGNode(continueStmt, terminator: true, exit: contTar is null);
                    Connect(prev, contNode);
                    ConnectTerminator(contNode, contTar);
                    return (contNode, contNode);
                }

                case ExpressionStmt exprStmt: {
                    var (start, exit) = CreateInternal(prev, exprStmt.Expression);
                    return (start, exit);
                }
                case ForeachLoop felStmt: {
                    var (start, range) = CreateInternal(prev, felStmt.Range);
                    ICFGNode vr = null;
                    if (felStmt.TryGetDeclaration(out var decl)) {
                        (_, vr) = CreateInternal(range, decl);
                    }
                    else if (felStmt.TryGetLoopVariables(out var vrs)) {
                        (_, vr) = CreateInternal(range, vrs[0]);// assuming, we have at least one loop variable
                        foreach (var loopVr in vrs.Slice(1)) {
                            (_, vr) = CreateInternal(vr, loopVr);
                        }
                    }// there is no else

                    //TODO: iterator logic here?

                    var (bodyStart, bodyEnd) = CreateInternal(vr, felStmt.Body);

                    Connect(bodyEnd, bodyStart);// iterator::tryGetNext() call?

                    return (start, bodyEnd);
                }
                case ForLoop forlStmt: {
                    var (start, init) = CreateInternal(prev, forlStmt.Initialization);
                    var (condStart, condEnd) = CreateInternal(init, forlStmt.Condition);
                    var exit = new NopCFGNode(forlStmt.Position);
                    using var _ = Push(condStart, exit);
                    var (_, bodyEnd) = CreateInternal(condEnd, forlStmt.Body);
                    var (_, incrEnd) = CreateInternal(bodyEnd, forlStmt.Increment);
                    Connect(incrEnd, condStart);
                    Connect(condEnd, exit);
                    return (start, exit);
                }
                case IfStatement ifStmt: {
                    var (start, cond) = CreateInternal(prev, ifStmt.Condition);
                    var endNode = new NopCFGNode(ifStmt.Position);
                    {
                        if (!(ifStmt.Condition is ILiteral lit) || lit.IsTrue()) {
                            var (_, thenEnd) = CreateInternal(cond, ifStmt.ThenStatement);
                            Connect(thenEnd, endNode);
                        }
                    }
                    {
                        if (ifStmt.ElseStatement != null && (!(ifStmt.Condition is ILiteral lit) || !lit.IsTrue())) {
                            var (_, elseEnd) = CreateInternal(cond, ifStmt.ElseStatement);
                            Connect(elseEnd, endNode);
                        }
                    }
                    return (start, endNode);
                }
                case NopStatement nopStmt: {
                    var nopNode = new NopCFGNode(nopStmt.Position);
                    Connect(prev, nopNode);
                    return (nopNode, nopNode);
                }
                case SwitchStatement switchStmt:
                    //TODO switch
                    break;
                case TryCatchFinallyStatement tcfStmt:
                    // TODO trycatchfinally
                    break;
                case WhileLoop whlStmt when whlStmt.IsHeadControlled: {
                    var (start, cond) = CreateInternal(prev, whlStmt.Condition);
                    var exit = new NopCFGNode(whlStmt.Position);
                    using var _ = Push(cond, exit);
                    var (_, loopEnd) = CreateInternal(cond, whlStmt.Body);

                    Connect(cond, exit);
                    Connect(loopEnd, start);

                    return (start, exit);
                }
                case WhileLoop dowStmt: {
                    var exit = new NopCFGNode(dowStmt.Condition.Position);
                    var condition = new NopCFGNode(dowStmt.Condition.Position);
                    using var _ = Push(condition, exit);
                    var (start, loopEnd) = CreateInternal(prev, dowStmt.Body);
                    var (_, cond) = CreateInternal(loopEnd, dowStmt.Condition);

                    Connect(condition, cond);
                    Connect(cond, start);
                    Connect(cond, exit);

                    return (start, exit);
                }
                case null: {
                    throw new ArgumentNullException(nameof(stmt));
                }
                default: {
                    using var exprs = stmt.GetExpressions().GetEnumerator();
                    if (exprs.MoveNext()) {
                        var (start, entry) = CreateInternal(prev, exprs.Current);
                        while (exprs.MoveNext()) {
                            (_, entry) = CreateInternal(entry, exprs.Current);
                        }
                        var ret = new StatementCFGNode(stmt);
                        Connect(entry, ret);
                        return (start, ret);
                    }
                    else {
                        var ret = new StatementCFGNode(stmt);
                        Connect(prev, ret);
                        return (ret, ret);
                    }
                }
            }
        }
    }
}
