/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Macros {
    [Serializable]
    public class MacroCallParameters {
        public readonly IDictionary<ExpressionParameter, IExpression> RequiredArgs;
        public readonly (ExpressionParameterPack, IExpression[])? OptionalArgs;
        public (StatementParameter, IStatement)? StatementCapture {
            get; private set;
        }
        public readonly ReadOnlyBox<Position> Position;
        public readonly ISemantics Semantics;
        public readonly Stack<IContext> contextStack;
        public readonly IExpression ParentExpression;
        public readonly MacroFunction Callee;
        public readonly Dictionary<IVariable, IVariable> VariableReplace = new Dictionary<IVariable, IVariable>();

        public MacroCallParameters(Position pos, IDictionary<ExpressionParameter, IExpression> requiredArgs, (ExpressionParameterPack, IExpression[])? optionalArgs, (StatementParameter, IStatement)? statementCapture, ISemantics sem, Stack<IContext> _cs, IExpression parent, MacroFunction fn) {
            Position = pos.BoxReadOnly();
            RequiredArgs = requiredArgs ?? throw new ArgumentNullException(nameof(requiredArgs));
            OptionalArgs = optionalArgs;
            StatementCapture = statementCapture;
            Semantics = sem ?? new BasicSemantics();
            contextStack = _cs ?? new Stack<IContext>().ChainedPush(Context.Immutable);
            ParentExpression = parent;
            Callee = fn ?? throw new ArgumentNullException(nameof(fn));
        }
        public bool IsReadOnly {
            get; private set;
        }
        public void MakeReadOnly() {
            IsReadOnly = true;
        }
        public bool TrySetStatementCapture(StatementParameter param, IStatement stmt) {
            if (IsReadOnly)
                return false;
            StatementCapture = (param, stmt);
            return true;
        }
    }
}
