/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Expressions {
    public enum ReferenceKind {
        None,
        RValue,
        ConstLValue,
        MutableLValue
    }
}
