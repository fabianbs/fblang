/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Contexts {
    public interface IRangeScope {
        IContext Context {
            get;
        }
    }
    public interface IRangeScope<T> : IRangeScope where T : IContext {
        new T Context {
            get;
        }
    }
}
