using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using Imms;

namespace CompilerInfrastructure.Analysis.CallGraph {
    public class DefaultCallGraphAnalysis : ICallGraphAnalysis {
        public ISet<IDeclaredMethod> GetAllCallees(CallExpression call) {
            if (call is null)
                throw new ArgumentNullException(nameof(call));
            if (!call.IsCallVirt)
                return ImmSet.Of<IDeclaredMethod>(call.Callee);
            if (!(call.Callee.NestedIn is ITypeContext tcx) || tcx.Type is null) 
                return ImmSet.Of<IDeclaredMethod>(call.Callee);

            //TODO implement;
            throw new NotImplementedException();
        }
    }
}
