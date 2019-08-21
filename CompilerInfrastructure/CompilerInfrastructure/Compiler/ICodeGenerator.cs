/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Compiler {
    /// <summary>
    /// A simple interface, which should be implemented by any code-generation component
    /// of a compiler
    /// </summary>
    public interface ICodeGenerator {
        /// <summary>
        /// Generates code from the given module
        /// </summary>
        /// <param name="mod">The module</param>
        /// <returns>True, when no errors were reported, false otherwise</returns>
        bool CodeGen(Module mod);
    }
}