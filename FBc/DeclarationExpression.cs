/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    /*[Serializable]
    public class DeclarationExpression : ExpressionImpl {
        public DeclarationExpression(Position pos, IVariable vr, IExpression defaultValue) : base(pos) {
            Variable = vr ?? throw new ArgumentNullException(nameof(vr));
            DefaultValue = defaultValue;
        }
        public IVariable Variable {
            get;
        }
        public IExpression DefaultValue {
            get;
        }
        public override IType ReturnType {
            get => Variable.Type;
        }
        public override bool IsLValue(IMethod met) => true;

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() {
            yield return Variable.DefaultValue;
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new DeclarationExpression(Position,
                Variable.Replace(genericActualParameter, curr, parent),
                DefaultValue.Replace(genericActualParameter, curr, parent)
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (DefaultValue.TryReplaceMacroParameters(args, out var dflt)) {
                if (dflt.Length > 1)
                    "A variable cannot be initialized with a variable number of values".Report(Position.Concat(args.Position));
                if (!Type.IsAssignable(dflt[0].ReturnType, ReturnType))
                    $"The type {dflt[0].ReturnType.Signature} is incompatible with the variable-type {ReturnType.Signature}".Report(Position.Concat(args.Position));
                expr = new[] { new DeclarationExpression(Position.Concat(args.Position), Variable, dflt[0]) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }*/
}
