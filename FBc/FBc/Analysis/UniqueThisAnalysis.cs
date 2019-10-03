using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Utils;
using System.Linq;
using CompilerInfrastructure.Contexts;

namespace FBc.Analysis {
    public class UniqueThisAnalysis : IDataFlowAnalysis<IDeclaredMethod, bool> {
        readonly CaptureAnalysis cap = new CaptureAnalysis();

        public UniqueThisAnalysis() {
        }

        public bool Query(IDeclaredMethod node, bool initialSeeds) {
            return Query(node);
        }
        public bool Query(IDeclaredMethod node) {
            if (node.NestedIn is ITypeContext tcx && tcx.Type != null)
                return !cap.Query(node, Set.Empty<IVariable>()).Contains(Variable.This(tcx.Type));
            return true;
        }
    }

}
