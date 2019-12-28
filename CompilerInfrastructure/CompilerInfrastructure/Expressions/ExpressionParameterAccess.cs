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
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    using Structure.Types;

    [Serializable]
    public class ExpressionParameterAccess : ExpressionImpl {
        public ExpressionParameterAccess(Position pos, ExpressionParameter param) : base(pos) {
            ExpressionParameter = param;
        }

        public ExpressionParameter ExpressionParameter { get; }
        public override IType ReturnType => Type.Top;

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return this;
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if(args.RequiredArgs.TryGetValue(ExpressionParameter,out var ret)) {
                expr = new[] { ret };
                return true;
            }
            "All macro-parameter-uses must be substituted".Report(Position.Concat(args.Position));
            expr = new[] { this };
            return false;
        }
    }
    public class ExpressionParameterPackUnpack : ExpressionImpl {
        public ExpressionParameterPackUnpack(Position pos, ExpressionParameterPack paramPack) : base(pos) {
            ExpressionParameters = paramPack;
        }
        public ExpressionParameterPack ExpressionParameters { get; }
        public override IType ReturnType => Type.Top.AsVarArg();

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return this;
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if(args.OptionalArgs.HasValue && args.OptionalArgs.Value.Item1 == ExpressionParameters) {
                expr = args.OptionalArgs.Value.Item2;
                return true;
            }
            "All macro-parameter-uses must be substituted".Report(Position.Concat(args.Position));
            expr = new[] { this };
            return false;
        }
    }
}
