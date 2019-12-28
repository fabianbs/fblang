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
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    using Type = Structure.Types.Type;

    [Serializable]
    public class DefaultValueExpression : ExpressionImpl, ICompileTimeEvaluable {
        static readonly LazyDictionary<IType, DefaultValueExpression> dfltCache = new LazyDictionary<IType, DefaultValueExpression>(x => new DefaultValueExpression(default, x));
        IType retTy;
        public DefaultValueExpression(Position pos, IType _retTy) : base(pos) {
            retTy = _retTy ?? "The type of the default-expression cannot be inferred".ReportTypeError(pos);
        }
        public override IType ReturnType {
            get => retTy;
        }
        public bool IsCompileTimeEvaluable => true;

        public void ResetReturnType(IType tp) => retTy = (tp ?? Type.Top);

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new DefaultValueExpression(Position, ReturnType.Replace(genericActualParameter, curr, parent));
        }
        public static DefaultValueExpression Get(IType ofType) {
            return dfltCache[ofType];
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }

        public ILiteral Evaluate(ref EvaluationContext context) {
            if (ReturnType.IsPrimitive()) {
                if (Literal.TryGetZero((PrimitiveType) ReturnType, out var ret))
                    return ret;
                return null;
            }
            else if (ReturnType.IsValueType())
                return null;
            else
                return Literal.Null;
        }

    }
}
