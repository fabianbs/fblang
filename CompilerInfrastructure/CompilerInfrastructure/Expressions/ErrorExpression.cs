/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    using Structure.Types;

    [Serializable]
    class ErrorExpression : ExpressionImpl {

        private ErrorExpression() : base(default) {
        }

        public override IType ReturnType {
            get;
        } = Type.Error;

        public static ErrorExpression Instance {
            get;
        } = new ErrorExpression();

        public override bool IsLValue(IMethod met) => true;
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public override IExpression Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }
    }
}
