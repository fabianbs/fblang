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
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    [Serializable]
    class InstanceofExpression : ExpressionImpl {
        readonly IExpression[] inst;
        public InstanceofExpression(Position pos, IExpression lhs, IType rhs) : base(pos) {
            inst = new[] { lhs };
            Type = rhs ?? CompilerInfrastructure.Type.Error;
        }
        public IExpression Instance => inst[0];
        public IType Type {
            get;
        }
        public override IType ReturnType {
            get => PrimitiveType.Bool;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(inst);
        public override IEnumerable<IExpression> GetExpressions() => inst;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new InstanceofExpression(Position,
                Instance.Replace(genericActualParameter, curr, parent),
                Type.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (Instance.TryReplaceMacroParameters(args, out var nwInst)) {
                if (nwInst.Length != 1) {
                    "An instanceof-expression cannot check the type of a variable number of objects".Report(Instance.Position.Concat(args.Position));
                    nwInst = inst;
                }
                if (nwInst[0].ReturnType.UnWrapAll().IsSubTypeOf(Type))
                    expr = new[] { Literal.True };
                else
                    expr = new[]{ new InstanceofExpression(Position.Concat(args.Position),
                    nwInst[0],
                    Type
                ) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
    [Serializable]
    class IsTypeExpression : ExpressionImpl, ICompileTimeEvaluable {
        public IsTypeExpression(Position pos, IType sub, IType super) : base(pos) {
            Subtype = sub ?? Type.Error;
            Supertype = super ?? Type.Error;
        }
        public override IType ReturnType {
            get => PrimitiveType.Bool;
        }
        public IType Subtype {
            get;
        }
        public IType Supertype {
            get;
        }
        public bool IsCompileTimeEvaluable => true;

        public ILiteral Evaluate(ref EvaluationContext context) {
            bool succ = true;
            if (Subtype.MayRelyOnGenericParameters())
                succ = $"The LHS-type {Subtype} is generic and does not fully specialize all template-parameters. Hence the 'is' expression cannot be evaluated at compile-time".Report(Position, false);
            if (Supertype.MayRelyOnGenericParameters())
                succ = $"The RHS-type {Subtype} is generic and does not fully specialize all template-parameters. Hence the 'is' expression cannot be evaluated at compile-time".Report(Position, false);
            if (!succ)
                return null;
            if (Subtype.IsSubTypeOf(Supertype)) {
                context.AssertIsTypeFact(Subtype, Supertype);
                return context.AssertFact(this, Literal.True);
            }
            else
                return context.AssertFact(this, Literal.Bool(context.TryGetIsTypeFact(Subtype, Supertype)));
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new IsTypeExpression(Position,
                Subtype.Replace(genericActualParameter, curr, parent),
                Supertype.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }
    }
}
