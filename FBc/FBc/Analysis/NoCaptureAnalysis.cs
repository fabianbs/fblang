using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using Imms;
using CompilerInfrastructure.Utils;

namespace FBc.Analysis {
    public class CaptureAnalysis : InterMonoAnalysis<IVariable> {
        public CaptureAnalysis(IEqualityComparer<(IDeclaredMethod, ISet<IVariable>)> metComp = null)
            : base(AnalysisDirection.Backward, metComp) {
        }
        bool TryGetVariableAccess(IExpression expr, out ImmSet<IVariable> ret) {
            ret = ImmSet.Empty<IVariable>();
            if (expr is VariableAccessExpression varAcc) {
                ret += varAcc.Variable;
            }
            else if (expr is BinOp bo && bo.Operator.IsAssignment()) {
                if (bo.Left is VariableAccessExpression lvar)
                    ret += lvar.Variable;
                if (TryGetVariableAccess(bo.Right, out var rvar))
                    ret += rvar;
            }

            return !ret.IsEmpty;
        }
        protected override ImmSet<IVariable> CallFlow(CallExpression call, ImmSet<IVariable> In, Func<(IDeclaredMethod, ISet<IVariable>), ISet<IVariable>> FF) {
            var capturedInCallee = FF((call.Callee, ImmSet.Empty<IVariable>())).ToImmSet();
            var arguments = call.Callee.IsStatic()
                ? call.Callee.Arguments
                : call.Callee.Arguments.Append(Variable.This((call.Callee.NestedIn as ITypeContext).Type));
            //var capturedArguments = capturedInCallee.Intersect(arguments);

            foreach (var (actual, formal) in call.MapActualToFormalParameters()) {
                In = NormalFlow(actual, In);
                if (TryGetVariableAccess(actual, out var actVar) && capturedInCallee.Contains(formal)) {
                    In += actVar;
                }
            }
            return In;
        }

        protected override ImmSet<IVariable> NormalFlow(IStatement stmt, ImmSet<IVariable> In) {
            //TODO handle declarations
            return In;
        }

        protected override ImmSet<IVariable> NormalFlow(IExpression expr, ImmSet<IVariable> In) {
            //TODO handle Assignment
            return In;
        }
        public ISet<IVariable> Query(IDeclaredMethod node) {
            if (node is null)
                return Set.Empty<IVariable>();

            return Query(node, Set.Empty<IVariable>());
        }
    }
}
