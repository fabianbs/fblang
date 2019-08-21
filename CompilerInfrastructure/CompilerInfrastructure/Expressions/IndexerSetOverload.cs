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
    [Serializable]
    public class IndexerSetOverload : ExpressionImpl {
        readonly CallExpression[] call;
        internal IndexerSetOverload(Position pos, CallExpression ex) : base(pos) {
            call = new[] { ex };
            ReturnType = ex.Arguments[ex.Arguments.Length - 1].ReturnType;
        }
        public override IType ReturnType {
            get;
        }
        public CallExpression OperatorCall => call[0];
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(call);
        public override IEnumerable<IExpression> GetExpressions() => call;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new IndexerSetOverload(Position,
               (CallExpression)OperatorCall.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (OperatorCall.TryReplaceMacroParameters(args, out var nwCall)) {
                if (nwCall.Length != 1) {
                    "An indexer-set expression cannot call a variable number of methods".Report(OperatorCall.Position.Concat(args.Position));
                }
                expr = new[] { new IndexerSetOverload(Position.Concat(args.Position), nwCall.FirstOrDefault() as CallExpression) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
