/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Structure.Types.Generic {
    [Serializable]
    public class GenericLiteralParameter : ILiteral, IGenericParameter, ICompileTimeEvaluable {

        public GenericLiteralParameter(Position pos, string name, PrimitiveType type, bool forceUnsigned = false) {
            Position = pos;
            Name = name;
            ReturnType = type ?? "Für einen generischen Literalparameter muss ein primitiver Typ angegeben werden".Report(pos, Type.Error);
            IsUnsigned = forceUnsigned || type.IsUnsignedInteger;
        }

        public IType ReturnType {
            get;
        }
        public bool IsUnsigned {
            get;
        }
        public bool IsLValue(IMethod met) => false;
        public Position Position {
            get;
        }
        public string Name {
            get;
        }
        bool ILiteral.IsPositiveValue {
            get => IsUnsigned;
        }
        /// <summary>
        /// Never ask for NotNullable-DataflowFacts on literals
        /// </summary>
        public ISet<IVariable> NotNullableVars { get; set; }
        public IType MinimalType {
            get => ReturnType;
        }
        public IType BaseType => ReturnType;

        public bool TypecheckNecessary => false;

        public bool IsCompileTimeEvaluable => true;

        public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        public IExpression Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (genericActualParameter.TryGetValue(this, out var ret)) {
                return (IExpression) ret;
            }
            return this;
        }
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return (ILiteral) Replace(genericActualParameter, curr, parent);
        }
        public bool ValueEquals(ILiteral other) => other == this;
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public bool TryReplaceMacroParameters(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }

        public bool TryReplaceMacroParameters(MacroCallParameters args, out SwitchStatement.IPattern ret) {
            ret = this;
            return false;
        }
        SwitchStatement.IPattern SwitchStatement.IPattern.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return (ILiteral) Replace(genericActualParameter, curr, parent);
        }
        public IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public ILiteral Evaluate(ref EvaluationContext ctx) => ctx.AssertFact(this, this);
    }
}
