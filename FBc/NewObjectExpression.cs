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
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    [Serializable]
    class NewObjectExpression : ExpressionImpl, INotNullableExpression, IEphemeralExpression {
        /// <summary>
        /// The minimum-visibility for constructors. Only necessary for ephemeral NewObjectExpressions
        /// </summary>
        private readonly Visibility minimumVisibility;

        internal protected NewObjectExpression(Position pos, IType objectType, IMethod ctor, IExpression[] args, Visibility minimumVis = Visibility.Public) : base(pos) {
            ReturnType = objectType ?? Type.Error;
            Arguments = args ?? Array.Empty<IExpression>();
            this.minimumVisibility = minimumVis;
            this.Constructor = ctor ?? Method.Error;
            IsEphemeral = ctor is null || args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack);
        }
        public bool IsEphemeral {
            get;
        }
        public override IType ReturnType {
            get;
        }
        public IExpression[] Arguments {
            get;
        }
        public IMethod Constructor {
            get;
        }
        public bool IsNotNullable => true;

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(Arguments);
        public override IEnumerable<IExpression> GetExpressions() => Arguments;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new NewObjectExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Constructor.Replace(genericActualParameter, curr, parent),
                Arguments.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray(),
                minimumVisibility
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (IsEphemeral) {
                bool changed = false;
                var nwArgs = Arguments.SelectMany(x => {
                    if (x.TryReplaceMacroParameters(args, out var nwArg)) {
                        changed = true;
                        return nwArg;
                    }
                    return new[] { x };
                }).ToArray();
                if (changed) {
                    var mets = ReturnType.Context.InstanceContext.MethodsByName("ctor");
                    var met = args.Semantics.BestFittingMethod(Position.Concat(args.Position), mets, nwArgs.Select(x => x.MinimalType()).AsCollection(nwArgs.Length), PrimitiveType.Void);
                    if (met.IsError()) {
                        $"The called constructor is not defined in {ReturnType}".Report(Position.Concat(args.Position));
                    }
                    else if (met.Visibility < minimumVisibility) {
                        "The called constructor is not visible here".Report(Position.Concat(args.Position));
                    }

                    expr = new[] { new NewObjectExpression(Position.Concat(args.Position), ReturnType, met, nwArgs, minimumVisibility) };
                    return true;
                }
            }
            expr = new[] { this };
            return false;
        }
    }
}