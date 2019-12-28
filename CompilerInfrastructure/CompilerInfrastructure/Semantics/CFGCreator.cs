/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Semantics {
    using Type = Structure.Types.Type;

    public partial class BasicSemantics {

        public class CFGCreator {
            readonly IType expectedReturnType;
            readonly ICollection<IStatement> unreachableStatements;
            readonly RelativeStatementPosition reportStmts;
            readonly ErrorBuffer err;
            public CFGCreator(IType expectedReturnType, ICollection<IStatement> unreachableStatements, RelativeStatementPosition reportStmts = RelativeStatementPosition.Behind, ErrorBuffer err = null) {
                this.expectedReturnType = expectedReturnType;
                this.unreachableStatements = unreachableStatements;
                this.reportStmts = reportStmts;
                this.err = err;
            }
            public CFGNode CreateCFG(IStatement root, Func<IStatement, bool> isTarget) {
                return CreateCFG(root, new CFGNode(false), isTarget).start;
            }
            protected internal virtual CFGNode CreateReturnDetectingCFG(IStatement root, CFGNode previous) {
                return CreateCFG(root, previous, x => x is ReturnStatement).start;
            }
            protected internal virtual (CFGNode start, CFGNode end) CreateCFG(IStatement root, CFGNode previous, Func<IStatement, bool> isTarget) {
                //TODO break-statements + unreachable statements?
                CFGNode singleRet = previous;
                switch (root) {
                    case BlockStatement blck: {
                        CFGNode nod = previous;
                        CFGNode ret = nod;
                        int index = 0;
                        foreach (var innerStmt in blck.Statements) {
                            if (nod != null && nod.IsTerminated) {
                                if (reportStmts.HasFlag(RelativeStatementPosition.Behind))
                                    unreachableStatements?.Add(innerStmt);
                                else
                                    break;
                            }
                            else {
                                CFGNode startNod;
                                (startNod, nod) = CreateCFG(innerStmt, nod, isTarget);
                                if (index == 0)
                                    ret = startNod;
                                if (nod.IsTerminated && index > 0 && reportStmts.HasFlag(RelativeStatementPosition.Before)) {
                                    unreachableStatements.AddRange(blck.Statements.Take(index));
                                }
                            }
                            index++;
                        }
                        return (ret, nod);
                    }

                    case ReturnStatement ret: {
                        if (!expectedReturnType.IsTop() && !expectedReturnType.IsError()) {
                            if (ret.HasReturnValue == (!expectedReturnType.IsPrimitive(PrimitiveName.Void))) {//TODO consider reference capabilities
                                if (ret.ReturnValue != null && (!Type.IsAssignable(ret.ReturnValue.ReturnType.UnWrap(), expectedReturnType.UnWrap()) || ret.ReturnValue.IsNullable() && expectedReturnType.IsNotNullable())) {
                                    err.Report($"The return-statement cannot return a value of type {ret.ReturnValue.ReturnType.Signature}; expected a value of type {expectedReturnType.Signature}", ret.Position);
                                }
                            }
                            else {
                                if (!ret.HasReturnValue && !expectedReturnType.IsVoid()) {
                                    err.Report($"The return-statement must return a value of type {expectedReturnType.Signature} instead of nothing", ret.Position);
                                }
                                else {
                                    err.Report("The return-statement must not return a value", ret.Position);
                                }
                            }
                        }
                        singleRet = new CFGNode(isTarget(ret), previous);
                        break;
                    }
                    case ForeachLoop fe: {
                        var (_, doBranch) = CreateCFG(fe.Body, previous, isTarget);
                        singleRet = new CFGNode(isTarget(fe), previous, doBranch);
                        break;
                    }
                    case ForLoop fl: {
                        var (_, init) = CreateCFG(fl.Initialization, previous, isTarget);
                        var (_, doBranch) = CreateCFG(fl.Body, init, isTarget);
                        var (_, incr) = CreateCFG(fl.Increment, doBranch, isTarget);
                        singleRet = new CFGNode(isTarget(fl), init, incr);
                        break;
                    }
                    case IfStatement cond: {
                        var (_, then) = CreateCFG(cond.ThenStatement, previous, isTarget);
                        var (_, @else) = cond.ElseStatement != null && !(cond.ElseStatement is NopStatement) ? CreateCFG(cond.ElseStatement, previous, isTarget) : (previous, previous);
                        singleRet = new CFGNode(isTarget(cond), then, @else);
                        break;
                    }

                    case SwitchStatement cond: {
                        var cases = cond.Cases.Select(x => CreateCFG(x.OnMatch, previous, isTarget).end);
                        if (!cond.Cases.OfType<SwitchStatement.DefaultCase>().Any()) {
                            cases = cases.Append(previous);
                        }
                        singleRet = new CFGNode(isTarget(cond), cases.ToArray());
                        break;
                    }
                    case TryCatchFinallyStatement tcf: {

                        var alts = new List<CFGNode>(1 + tcf.CatchBlocks.Length);
                        foreach (var stmt in tcf.CatchBlocks) {
                            alts.Add(CreateCFG(stmt, previous, isTarget).end);
                        }
                        var merge = new CFGNode(isTarget(tcf), alts);
                        if (tcf.HasFinally)
                            singleRet = CreateCFG(tcf.FinallyBlock, merge, isTarget).end;
                        else
                            singleRet = merge;
                        break;
                    }
                    case WhileLoop wl when wl.IsHeadControlled: {
                        var doBranch = CreateCFG(wl.Body, previous, isTarget).end;
                        singleRet = new CFGNode(isTarget(wl), previous, doBranch);
                        break;
                    }
                    default:
                        singleRet = new CFGNode(isTarget(root), previous);
                        break;
                }
                return (previous ?? singleRet, singleRet);
            }
        }
    }
}
