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
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class StatementParameterAccess : StatementImpl {
        public StatementParameterAccess(Position pos, StatementParameter param) : base(pos) {
            StatementParameter = param;
        }
        public StatementParameter StatementParameter {
            get;
        }
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return this;
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            if (args.StatementCapture.HasValue && args.StatementCapture.Value.Item1 == StatementParameter) {
                stmt = args.StatementCapture.Value.Item2;
                return true;
            }
            "All statement-patameter-uses must be substituted".Report(Position.Concat(args.Position));
            stmt = this;
            return false;
        }
    }
}
