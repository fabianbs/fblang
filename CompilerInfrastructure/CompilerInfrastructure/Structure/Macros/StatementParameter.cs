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
    public class StatementParameter : IPositional {
        public StatementParameter(Position position, string name) {
            Position = position;
            Name = name;
        }

        public Position Position { get; }
        public string Name {
            get;
        }
    }
}
