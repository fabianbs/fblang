/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Semantics {
    public interface ISemantics {
        /*Func<IMethod, IExpression, bool> IsCallVirt {
            get;
        }*/
        bool IsCallVirt(IMethod callee, IExpression parent);
        bool IsFunctional(IType tp, out FunctionType fnTy);
        bool IsIterator(IType tp, IType over);
        bool IsIterable(IType tp, IType over);
        bool IsIterable(IType tp, IType over, out IMethod getIterator, out IMethod tryGetNext);
        bool IsTriviallyIterable(IType tp, IType over);
        bool CanBePassedAsParameterTo(IType actualTy, IType formalTy, out int diff);

        IVariable BestFittingVariable(Position pos, IEnumerable<IVariable> vrs,
                                      IType    expectedVariableType);
        IMethod BestFittingMethod(Position pos, IEnumerable<IMethod> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null);
        IMethod BestFittingMethod<T>(Position pos, IEnumerable<IMethodTemplate<T>> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null) where T : IMethod;
        ITypeTemplate<IType> BestFittingTypeTemplate(Position pos, IEnumerable<ITypeTemplate<IType>> types, IReadOnlyList<ITypeOrLiteral> genArgs, ErrorBuffer err = null);
        IMethod BestFittingMethod(Position pos, IEnumerable<IDeclaredMethod> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null);

        #region operator-overloads
        IStatement CreateDeconstruction(Position pos, ICollection<IExpression> dest, IExpression range, ErrorBuffer err = null, IMethod met = null);
        IExpression CreateBinOp(Position pos, IType retTy, IExpression lhs, BinOp.OperatorKind op, IExpression rhs, ErrorBuffer err = null);
        IExpression CreateUnOp(Position pos, IType retTy, UnOp.OperatorKind op, IExpression subEx, ErrorBuffer err = null);
        IExpression CreateCall(Position pos, IType retTy, IType parentTy, IExpression parent, string name, ICollection<IExpression> args, ErrorBuffer err = null);
        IExpression CreateIndexer(Position pos, IType retTy, IType parentTy, IExpression parent, ICollection<IExpression> args, ErrorBuffer err = null);
        IExpression CreateRangedIndexer(Position pos, IType retTy, IType parentTy, IExpression parent, IExpression offset, IExpression count, ErrorBuffer err = null);
        IExpression CreateDefault(Position pos, IType ofType, ErrorBuffer err = null);
        #endregion
        CFGNode CreateControlFlowGraph(IStatement root, Func<IStatement, bool> isTarget);
        CFGNode CreateControlFlowGraph(IStatement root, IType expectedReturnType, ICollection<IStatement> unreachableStatements, Func<IStatement, bool> isTarget, ErrorBuffer err = null);
        CFGNode CreateControlFlowGraph(IStatement root, IType expectedReturnType, ICollection<IStatement> problematicStatements, Func<IStatement, bool> isTarget, RelativeStatementPosition reportStmts, ErrorBuffer err = null);

    }
    [Flags]
    public enum RelativeStatementPosition {
        Before = 1,
        Behind = 2,
        Both = 3
    }
    public enum Error {
        Unknown,
        None,//TODO specify, what are the possible targets
        AmbiguousTarget,
        NoValidTarget
    }
}
