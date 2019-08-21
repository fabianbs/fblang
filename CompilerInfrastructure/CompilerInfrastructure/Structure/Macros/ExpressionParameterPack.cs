/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Macros {
    [Serializable]
    public class ExpressionParameterPack : IPositional {
        public ExpressionParameterPack(Position position, string name) {
            Position = position;
            Name = name;
        }
        public string Name {
            get;
        }

        public Position Position { get; }
        public ISet<IEnumerable<ExpressionParameter.Constraint>> Constraints { get; } = new HashSet<IEnumerable<ExpressionParameter.Constraint>>();
    }
}
