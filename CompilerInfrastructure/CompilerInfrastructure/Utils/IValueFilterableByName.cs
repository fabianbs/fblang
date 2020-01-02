// /******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/

namespace CompilerInfrastructure.Utils {
    using System.Collections.Generic;

    public interface IValueFilterableByName<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> {
        IEnumerable<TValue> FilterValuesByName(string name);
    }
}
