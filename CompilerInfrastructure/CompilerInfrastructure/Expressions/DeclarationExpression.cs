/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class DeclarationExpression : ExpressionImpl, SwitchStatement.IPattern {
        public override IType ReturnType => Variable.Type;
        public IVariable Variable { get; }
        public IExpression DefaultValue => Variable.DefaultValue;

        public IType BaseType => ReturnType;

        public bool TypecheckNecessary { get; set; } = true;

        public DeclarationExpression(Position pos, IVariable nwVar) : base(pos) {
            Variable = nwVar;
        }
        public override bool IsLValue(IMethod met) => !Variable.IsFinal();
        public override IEnumerable<IExpression> GetExpressions() {
            if (DefaultValue != null)
                yield return DefaultValue;
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (DefaultValue != null) {
                var nwVar = Variable.Replace(genericActualParameter, curr, parent);
                return new DeclarationExpression(Position, nwVar);
            }
            return this;
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (DefaultValue != null && Variable.TryReplaceMacroParameters(args, out var nwVr)) {
                expr = new[] { new DeclarationExpression(Position.Concat(args.Position), nwVr) };
                return true;
            }
            expr = new[] { this };
            return false;
        }

        public bool TryReplaceMacroParameters(MacroCallParameters args, out SwitchStatement.IPattern ret) => throw new NotImplementedException();
        SwitchStatement.IPattern SwitchStatement.IPattern.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return (DeclarationExpression) Replace(genericActualParameter, curr, parent);
        }
        public IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
    }
}
