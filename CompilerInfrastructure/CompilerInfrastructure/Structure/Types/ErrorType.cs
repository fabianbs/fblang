/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class ErrorType : TopType {
        internal new static ErrorType Instance {
            get;
        } = new ErrorType();
        public override bool IsSubTypeOf(IType other, out int difference) {
            difference = int.MaxValue;
            return base.IsSubTypeOf(other);
        }
    }
}
