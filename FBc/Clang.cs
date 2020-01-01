/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FBc {
    public static class Clang {
        [DllImport("SimpleClangCompilerInterface")]
        extern static bool compile(string[] srcFiles, uint numSrcFiles, string outputFile, string[] dllNames, uint numDlls, bool enableWarnings);
        public static bool Compile(string[] srcFiles, string outputFile, string[] dllNames = null, bool enableWarnings = false) {
            if (srcFiles != null && srcFiles.Length > 0) {
                return compile(
                    srcFiles,
                    (uint)srcFiles.Length,
                    outputFile ?? srcFiles[0] + ".exe",
                    dllNames,
                    dllNames != null ? (uint)dllNames.Length : 0u,
                    enableWarnings
                );
            }
            return false;
        }
    }
}
