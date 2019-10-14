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
    public abstract class ControlRedirectStatement : StatementImpl, ITerminatorStatement {
        readonly IStatement[] tar;
        public ControlRedirectStatement(Position pos, IStatement target) : base(pos) {
            tar = new[] { target ?? "The target of a control-redirect-statement (like bread or continue) must not bei null".Report(pos, Statement.Error) };
        }
        public ref IStatement Target => ref tar[0];
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(tar);
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        public override IEnumerable<IStatement> GetStatements() => tar;
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            stmt = this;
            return false;
        }
    }
    [Serializable]
    public class BreakStatement : ControlRedirectStatement {
        public BreakStatement(Position pos, IStatement target) : base(pos, target) {
        }

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BreakStatement(Position, Target.Replace(genericActualParameter, curr, parent));
        }
    }
    [Serializable]
    public class ContinueStatement : ControlRedirectStatement {
        public ContinueStatement(Position pos, IStatement target) : base(pos, target) {
        }
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ContinueStatement(Position, Target.Replace(genericActualParameter, curr, parent));
        }
    }
}
