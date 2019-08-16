using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    class RandomInitDeclaration : Declaration {
        public RandomInitDeclaration(Position pos, IType type, Variable.Specifier specs, IEnumerable<string> names, IType randomDataSource, Visibility vis)
            : base(pos, type, specs, names, vis: vis) {
            if (!randomDataSource.IsError() && !randomDataSource.IsSubTypeOf(Type))
                "The deconstructed type cannot be converted to the variable-type".Report(pos);
            RandomDataSource = randomDataSource;
        }
        public IType RandomDataSource {
            get;
        }
    }
}
