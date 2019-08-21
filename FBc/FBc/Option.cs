/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    class Options {
        [Value(0, Min = 1, Max = int.MaxValue, Required = true, HelpText = "The names of the source files. If there are directories, all files in this directory will be included (the files will not be searched recursively)")]
        public IEnumerable<string> SourceFiles {
            get; set;
        }
        [Option('A', Required = false, Default = false, HelpText = "Disables code generation => only runs the analyzers and with --Lib generates a summary")]
        public bool SuppressCodeGen {
            get; set;
        }
        [Option('o', Required = false, HelpText = "The name of the binary-file. Will be inferred from the first specified sourcefile if missing.")]
        public string DestinationFile {
            get; set;
        }
        [Option('O', Required = false, Default = 1, HelpText = "The optimization-level the compiler should use; can be O0, O1, O2, or O3; the default is O1")]
        public int OptLevel {
            get; set;
        }
        [Option("emit-llvm", Default = false, Required = false, HelpText = "Generate the LLVM intermediate representation instead of LLVM bitcode. Cannot be used together with -native")]
        public bool EmitLLVM {
            get; set;
        }
        [Option("native", Default = false, Required = false, HelpText = "Generate a native executable or library. Cannot be used together with -emit-llvm")]
        public bool GenerateNativeCode {
            get; set;
        }
        [Option("force-loop-vectorize", Default = false, HelpText = "Tries to vectorize all loops. This can result in a much larger output-file")]
        public bool ForceLoopVectorization {
            get; set;
        }
        [Option("disable-range-checks", Default = false, HelpText = "Disables all array-bounds checks")]
        public bool DisableRangeCheck {
            get; set;
        }
        [Option("disable-null-checks", Default = false, HelpText = "Disables all null-dereference checks")]
        public bool DisableNullCheck {
            get; set;
        }
        [Option('L', "Lib", Default = false, Required = false, HelpText = "Generate a library instead of an executable application")]
        public bool AsLibrary {
            get; set;
        }
    }
}
