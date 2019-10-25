using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using CMethodImplAttribute = System.Runtime.CompilerServices.MethodImplAttribute;
using static System.Runtime.CompilerServices.MethodImplOptions;
using System.Linq;
using CompilerInfrastructure.Semantics;

namespace CompilerInfrastructure.Analysis.CFG {
    public class ControlFlowGraph {
        LazyDictionary<IDeclaredMethod, ICFGNode> cache;
        Stack<(ICFGNode continueTarget, ICFGNode breakTarget)> controlRedirectTarget
            = new Stack<(ICFGNode continueTarget, ICFGNode breakTarget)>();
        ISemantics sem;
        public ControlFlowGraph(ISemantics _sem) {
            sem = _sem ?? throw new ArgumentNullException(nameof(_sem));
            cache = new LazyDictionary<IDeclaredMethod, ICFGNode>(CreateInternal);
            controlRedirectTarget.Push((null, null));
        }
        public static ControlFlowGraph New<T>() where T : ISemantics, new() {
            return new ControlFlowGraph(new T());
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
        (ICFGNode start, ICFGNode exit) Chain(ICFGNode prev, IEnumerable<IExpression> _exprs) {
            using var exprs = _exprs.GetEnumerator();
            if (exprs.MoveNext()) {
                var (start, entry) = CreateInternal(prev, exprs.Current);
                while (exprs.MoveNext()) {
                    (_, entry) = CreateInternal(entry, exprs.Current);
                }
                return (start, entry);
            }
            else {
                var ret = new NopCFGNode(default);
                Connect(prev, ret);
                return (ret, ret);
            }
        }
        (ICFGNode start, ICFGNode exit) ShortCircuitChain(ICFGNode prev, IEnumerable<IExpression> _exprs) {
            using var exprs = _exprs.GetEnumerator();
            var exit = new NopCFGNode(default);
            if (exprs.MoveNext()) {
                var (start, entry) = CreateInternal(prev, exprs.Current);
                Connect(entry, exit);
                while (exprs.MoveNext()) {
                    (_, entry) = CreateInternal(entry, exprs.Current);
                    Connect(entry, exit);
                }
                return (start, exit);
            }
            else {

                Connect(prev, exit);
                return (exit, exit);
            }
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
                    IType vrTy = null;

                    var exit = new NopCFGNode(felStmt.Position);
                    
                    if (felStmt.TryGetDeclaration(out var decl)) {
                        (_, vr) = CreateInternal(range, decl);
                        vrTy = (decl as Declaration).Type;
                    }
                    else if (felStmt.TryGetLoopVariables(out var vrs)) {
                        (_, vr) = CreateInternal(range, vrs[0]);// assuming, we have at least one loop variable
                        //TODO: tuples
                        vrTy = vrs[0].ReturnType;
                        foreach (var loopVr in vrs.Slice(1)) {
                            (_, vr) = CreateInternal(vr, loopVr);
                        }
                    }// there is no else

                    // iterator logic here:
                    if (!sem.IsTriviallyIterable(felStmt.Range.ReturnType, vrTy)
                        && sem.IsIterable(felStmt.Range.ReturnType, vrTy, out var getIterator, out var tryGetNext)) {

                        // Note:  These artificial callexpressions do not match the exact function call/signature, but it does not matter for dataflow analysis.
                        // Note2: Use NopExpressions instead of felStmt.Range/the call to getIterator to avoid repeated execution behaviour.
                        (_, vr) = CreateInternal(vr, new CallExpression(felStmt.Range.Position, null, getIterator, new NopExpression(felStmt.Range.Position, felStmt.Range.ReturnType), Array.Empty<IExpression>()));
                        var (tgnStart, tgnEnd) = CreateInternal(vr, new CallExpression(felStmt.Range.Position, null, tryGetNext, new NopExpression(felStmt.Range.Position, getIterator.ReturnType), Array.Empty<IExpression>()));

                        using (Push(tgnStart, exit)) {
                            var (bodyStart, bodyEnd) = CreateInternal(tgnEnd, felStmt.Body);
                            Connect(bodyEnd, tgnStart);
                            Connect(bodyEnd, exit);
                            Connect(tgnEnd, exit);
                            return (start, exit);
                        }
                    }
                    else {
                        using var _ = Push(vr, exit);
                        var (bodyStart, bodyEnd) = CreateInternal(vr, felStmt.Body);
                        Connect(bodyEnd, bodyStart);// iterator::tryGetNext() call?
                        Connect(vr, exit);
                        Connect(bodyEnd, exit);
                        return (start, exit);
                    }

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
                case SwitchStatement switchStmt: {
                    var (start, cond) = CreateInternal(prev, switchStmt.Condition);
                    var prevPat = cond;
                    var exit = new NopCFGNode(switchStmt.Position);
                    using var _ = PushBreak(exit);
                    foreach (var cas in switchStmt.Cases) {
                        var (_, patEnd) = ShortCircuitChain(prevPat, cas.Patterns.SelectMany(x => x.GetExpressions()));
                        var (_, blockEnd) = CreateInternal(patEnd, cas.OnMatch);
                        Connect(blockEnd, exit);
                        prevPat = patEnd;
                    }
                    if (!switchStmt.IsExhaustive())
                        Connect(prevPat, exit);
                    return (start, exit);
                }
                case TryCatchFinallyStatement tcfStmt: {
                    var tryExit = new NopCFGNode(tcfStmt.Position);
                    var tryStmts = (tcfStmt.TryBlock is BlockStatement blck && blck.Statements.Length > 0 ? blck.Statements : new[]{ tcfStmt.TryBlock});
                    var (start, tryEnd) = CreateInternal(prev, tryStmts[0]);
                    Connect(tryEnd, tryExit);
                    foreach (var st in tryStmts.AsSpan(1)) {
                        (_, tryEnd) = CreateInternal(tryEnd, st);
                        Connect(tryEnd, tryExit);
                    }
                    ICFGNode catchExit;
                    if (tcfStmt.CatchBlocks.IsEmpty) {
                        catchExit = tryExit;
                    }
                    else {
                        catchExit = new NopCFGNode(tcfStmt.Position);
                        foreach (var catchSt in tcfStmt.CatchBlocks) {
                            var (_, catchEnd) = CreateInternal(tryExit, catchSt);
                            Connect(catchEnd, catchExit);
                        }
                    }
                    if (tcfStmt.HasFinally) {
                        var (finallyStart, finallyEnd) = CreateInternal(catchExit, tcfStmt.FinallyBlock);
                        Connect(tryExit, finallyStart);
                        return (start, finallyEnd);
                    }
                    else
                        return (start, catchExit);
                }
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
