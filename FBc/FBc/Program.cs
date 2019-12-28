/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using CommandLine;
using CompilerInfrastructure;
using CompilerInfrastructure.Utils;

namespace FBc {
    class Program {
        static int Main(string[] args) {

            string destFile = null;
            IEnumerable<string> filenames = Enumerable.Empty<string>();
            byte optLevel = 1;
            bool doCodeGen = true;
            bool emitLLVM = false;
            bool native = false;
            bool forceLV = false;
            bool disableBoundsCheck = false;
            bool disableNullCheck = false;
            bool isLib = false;
            CommandLine.Parser.Default.ParseArguments<Options>(args).WithParsed(inp => {
                filenames = inp.SourceFiles;
                destFile = inp.DestinationFile;
                if (inp.OptLevel <= 3 && inp.OptLevel >= 0) {
                    optLevel = (byte) inp.OptLevel;
                }
                else {
                    Console.Error.WriteLine("Warning: The optimizer-level must be in range [0..3]; now use O1 instead");
                }
                doCodeGen = !inp.SuppressCodeGen;
                emitLLVM = inp.EmitLLVM;
                native = inp.GenerateNativeCode;
                forceLV = inp.ForceLoopVectorization;
                disableBoundsCheck = inp.DisableRangeCheck;
                disableNullCheck = inp.DisableNullCheck;
                isLib = inp.AsLibrary;
            });
            if (destFile is null) {
                //TODO support libraries
                ReadOnlySpan<char> fname = filenames.FirstOrDefault() ?? ".";
                if (fname.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                    fname = fname.Slice(0, fname.Length - 1);
                }
                destFile = FBCompiler.InferOutputFilenameEnding(Path.GetFileNameWithoutExtension(fname.ToString()), native, emitLLVM, isLib);
            }
            if (native && emitLLVM) {
                "Cannot use -native and -emit-llvm together".Report();
                return -2;
            }
            var actualFilenames = filenames.SelectMany(f => {
                if (File.Exists(f)) {
                    return new[] { f };
                }
                else if (Directory.Exists(f)) {
                    return Directory.EnumerateFiles(f);
                }
                else {
                    bool isProbFile = Path.HasExtension(f);
                    $"The given source-{(isProbFile ? "file" : "directory")} {f} does not exist".Report();
                    return Array.Empty<string>();
                }
            });
            if (!actualFilenames.Any()) {
                "It must be specified at least one input file".Report();
                return -1;
            }

            var compiler = new FBCompiler(
                doGenerateCode: doCodeGen,
                optLevel: optLevel,
                emitLLVM: emitLLVM,
                native: native,
                forceLV: forceLV,
                disableNullChecks: disableNullCheck,
                disableBoundsChecks: disableBoundsCheck,
                isLibrary: isLib,
                moduleName: Path.GetFileNameWithoutExtension(destFile)
            );
            compiler.Compile(actualFilenames.ToArray(), destFile, null);
            if (compiler.NumErrors > 0) {
                Console.WriteLine($"Build cancelled with {compiler.NumErrors} errors");
            }
            else {
                Console.WriteLine("Build finished successfully");
            }
#if DEBUG
            Console.ReadKey(true);
#endif
            return compiler.NumErrors;
        }
    }
}
