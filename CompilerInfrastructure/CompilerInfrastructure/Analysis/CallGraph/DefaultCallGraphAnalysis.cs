using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using Imms;

namespace CompilerInfrastructure.Analysis.CallGraph {
    /// <summary>
    /// Implements a simple, demand-driven CHA analysis
    /// </summary>
    public class DefaultCallGraphAnalysis : ICallGraphAnalysis {
        //Dictionary<IDeclaredMethod, ISet<IDeclaredMethod>> cache = new Dictionary<IDeclaredMethod, ISet<IDeclaredMethod>>();
        readonly RecursiveLazyDictionary<IMethod, ISet<IDeclaredMethod>> cache ;
        public DefaultCallGraphAnalysis() {
            cache = new RecursiveLazyDictionary<IMethod, ISet<IDeclaredMethod>>(GetAllPossibleImplementations, ImmSet.Empty<IDeclaredMethod>());
        }
        public ISet<IDeclaredMethod> GetAllCallees(CallExpression call) {
            if (call is null)
                throw new ArgumentNullException(nameof(call));
            if (!call.IsCallVirt)
                return ImmSet.Of<IDeclaredMethod>(call.Callee);

            return GetAllPossibleImplementations(call.Callee);


        }
        ISet<IDeclaredMethod> GetAllPossibleImplementations(IMethod met) {

            var ret = ImmSet.Of<IDeclaredMethod>(met);
            if ((met.NestedIn is ITypeContext tcx) && (tcx.Type is ClassType staticType)) {//DOLATER: work on arbitrary IHierarchialTypes
                
                foreach (var subty in staticType.SubTypes) {
                    if (subty.Context.InstanceContext.LocalContext.Methods.TryGetValue(met.Signature, out var impl)) {
                        ret += cache[impl];
                    }
                }
            }
            return ret;
        }
    }
}
