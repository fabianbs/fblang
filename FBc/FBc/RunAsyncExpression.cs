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
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    [Serializable]
    class RunAsyncExpression : ExpressionImpl {
        readonly IExpression[] task;
        IReadOnlyCollection<LambdaCapture> captures = List.Empty<LambdaCapture>();
        IReadOnlyCollection<ICaptureExpression> captureAccesses = List.Empty<ICaptureExpression>();
        public RunAsyncExpression(Position pos, IExpression _task) : base(pos) {
            task = new[] { _task ?? Expression.Error };
            ReturnType = Task.ReturnType.AsAwaitable();
        }
        public override IType ReturnType {
            get;
        }
        public IExpression Task => task[0];
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(task);
        public override IEnumerable<IExpression> GetExpressions() => task;

        public IReadOnlyCollection<LambdaCapture> Captures {
            get => captures;
            set => captures = value ?? List.Empty<LambdaCapture>();
        }
        public IReadOnlyCollection<ICaptureExpression> CaptureAccesses {
            get => captureAccesses;
            set => captureAccesses = value ?? List.Empty<CaptureAccessExpression>();
        }

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new RunAsyncExpression(Position,
                Task.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            var nwCapAcc = CaptureAccesses.Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    // CaptureAccessExpression can only be instantiated inside of this module => nwX has always length 1
                    return nwX[0] as CaptureAccessExpression;
                }
                return x;
            }).ToArray();
            IReadOnlyCollection<LambdaCapture> nwCap;
            if (changed)
                nwCap = nwCapAcc.Select(x => (x as ICaptureExpression).Variable).Distinct().AsCollection().AsReadOnly();
            else
                nwCap = captures;

            if (Task.TryReplaceMacroParameters(args, out var nwTask)) {
                changed = true;
                if (nwTask.Length != 1) {
                    "The defer-expression cannot run a variable number of expresions async".Report(Task.Position.Concat(args.Position));
                    nwTask = task;
                }
            }

            if (changed) {
                expr = new[] { new RunAsyncExpression(Position.Concat(args.Position), nwTask[0]) {
                    CaptureAccesses = nwCapAcc,
                    Captures = nwCap
                } };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
