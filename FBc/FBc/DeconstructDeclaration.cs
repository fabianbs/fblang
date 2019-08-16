using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    class DeconstructDeclaration : Declaration {
        public DeconstructDeclaration(Position pos, IType type, Variable.Specifier specs, IEnumerable<string> names, IExpression range, Visibility vis)
            : base(pos, type, specs, names, vis: vis) {
            DeconstructionRange = range;
        }
        public IExpression DeconstructionRange {
            get;
        }
    }
}
