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
    /// Simple interface, which every compiler-object may implement somehow
    /// </summary>
    public interface ICompiler {
        /// <summary>
        /// Performs the actual compilation
        /// </summary>
        /// <param name="sources">The paths to the source-files</param>
        /// <param name="destination">The destination path</param>
        /// <param name="references">The paths to the reference-libraries</param>
        /// <returns>True on success, false otherwise</returns>
        bool Compile(string[] sources, string destination, string[] references);
    }
}
