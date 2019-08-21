/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static CompilerInfrastructure.Contexts.SimpleMethodContext;

namespace FBc {
    [Serializable]
    class RandomDeconstruction : StatementImpl {
        readonly IExpression[] lvalues;
        public RandomDeconstruction(Position pos, IEnumerable<IExpression> dest, IType randomDatasource, bool checkConsistency = false)
            : base(pos) {
            lvalues = dest.ToArray();
            RandomDataSource = randomDatasource;
            //TODO check consistency
        }
        public IType RandomDataSource {
            get;
        }
        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.FromArray<IASTNode>(lvalues);
        }*/

        public override IEnumerable<IExpression> GetExpressions() => lvalues;
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new RandomDeconstruction(Position,
                lvalues.Select(x => x.Replace(genericActualParameter, curr, parent)),
                RandomDataSource.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            var nwLValues = lvalues.SelectMany(x=> {
                if (x.TryReplaceMacroParameters(args, out var nwLV)) {
                    changed = true;
                    return nwLV;
                }
                return new[] { x };
            }).ToArray();

            if (changed) {
                var err = nwLValues.Where(x=>!x.ReturnType.IsSubTypeOf(RandomDataSource));
                if (err.Any()) {
                    $"The deconstruction-destinations {string.Join(", ", err)} are incompatible with the random-data source".Report(Position.Concat(args.Position));
                }
                stmt = new RandomDeconstruction(Position.Concat(args.Position), nwLValues, RandomDataSource);
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
