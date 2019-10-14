/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public abstract class ControlReturnStatement : StatementImpl {
        readonly IExpression[] retVal;
        public ControlReturnStatement(Position pos, IExpression returnValue = null) : base(pos) {
            retVal = new[] { returnValue };
        }
        public IExpression ReturnValue {
            get => retVal[0];
            set => retVal[0] = value;
        }
        public bool HasReturnValue => !(ReturnValue is null);
        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            if (HasReturnValue)
                return RefEnumerator.FromArray<IASTNode>(retVal);
            else
                return RefEnumerator.Empty<IASTNode>();
        }
        */
        public override IEnumerable<IExpression> GetExpressions() => retVal;
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
    }
    [Serializable]
    public class ReturnStatement : ControlReturnStatement, ITerminatorStatement {
        public ReturnStatement(Position pos, IExpression returnValue = null) : base(pos, returnValue) {
        }

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (HasReturnValue) {
                return new ReturnStatement(Position, ReturnValue.Replace(genericActualParameter, curr, parent));
            }
            return this;
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            if (HasReturnValue && ReturnValue.TryReplaceMacroParameters(args, out var expr)) {
                if (expr.Length != 1) {
                    "Returning tuples of variable length is not supported".Report(Position.Concat(args.Position));
                }
                stmt = new ReturnStatement(Position.Concat(args.Position), expr.FirstOrDefault());
                return true;
            }
            stmt = this;
            return false;
        }
    }
    [Serializable]
    public class YieldStatement : ControlReturnStatement {
        public YieldStatement(Position pos, IExpression returnValue = null) : base(pos, returnValue) {
        }
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (HasReturnValue) {
                return new YieldStatement(Position, ReturnValue.Replace(genericActualParameter, curr, parent));
            }
            return this;
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            if (HasReturnValue && ReturnValue.TryReplaceMacroParameters(args, out var expr)) {
                if (expr.Length != 1) {
                    "Returning tuples of variable length is not supported".Report(Position.Concat(args.Position));
                }
                stmt = new YieldStatement(Position.Concat(args.Position), expr.FirstOrDefault());
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
