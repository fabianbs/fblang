/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure.Types.Generic {
    public interface IHierarchialTypeTemplate<out T> : ITypeTemplate<T> where T : IType {
        IHierarchialType SuperType { get; }
    }
}
