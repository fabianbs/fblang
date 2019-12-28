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
    public class ThisExpression : ExpressionImpl {
        public ThisExpression(Position pos, IType retTy) : base(pos) {
            ReturnType = retTy ?? "The type for the this-expression is undefined".ReportTypeError(pos);
        }

        public override IType ReturnType {
            get;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ThisExpression(Position, ReturnType.Replace(genericActualParameter, curr, parent));
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (args.ParentExpression != null) {
                expr = new[] { args.ParentExpression };
                return true;
            }
            expr = new[] { this };
            return false;
        }
        public override string ToString() => "this";
    }
    public class BaseExpression : ExpressionImpl {
        public BaseExpression(Position pos, IType retTy) : base(pos) {
            ReturnType = retTy ?? "The type for the base-expression is undefined".ReportTypeError(pos);
        }

        public override IType ReturnType {
            get;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BaseExpression(Position, ReturnType.Replace(genericActualParameter, curr, parent));
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (args.ParentExpression != null) {
                "Refering to the super-object is not allowed from a macro".Report(Position.Concat(args.Position));
            }
            expr = new[] { this };
            return false;
        }
        public override string ToString() => "base";
    }
}
