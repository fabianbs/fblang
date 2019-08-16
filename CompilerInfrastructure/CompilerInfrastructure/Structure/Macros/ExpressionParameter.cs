﻿using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Structure.Macros {
    [Serializable]
    public class ExpressionParameter :IPositional{
        public class Constraint {
            public Constraint(IType typeConstraint, bool requiresLValue) {
                TypeConstraint = typeConstraint ?? throw new ArgumentNullException(nameof(typeConstraint));
                RequiresLValue = requiresLValue;
            }

            public IType TypeConstraint { get; }
            public bool RequiresLValue { get; set; }
        }
        public ExpressionParameter(Position pos, string name) {
            Position = pos;
            Name = name;
        }

        public Position Position { get; }
        public string Name {
            get;
        }
        public ISet<Constraint> Constraints { get; } = new HashSet<Constraint>();
    }
}
