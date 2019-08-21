/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IExpressionContainer {
        IEnumerable<IExpression> GetExpressions();
    }
}
