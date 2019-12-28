/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Antlr4.Runtime;
using CompilerInfrastructure;
using CompilerInfrastructure.Compiler;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FBc {
    public class FBCompiler : AbstractCompiler {
        FBModule module;
        readonly byte _warningLevel;
        public FBCompiler(byte warningLevel = 1, bool doGenerateCode = true, byte optLevel = 1, bool emitLLVM = false, bool native = false, bool forceLV = false, bool disableBoundsChecks = false, bool disableNullChecks = false, bool isLibrary = false, string moduleName = "") {
            _warningLevel = warningLevel;
            DoGenerateCode = doGenerateCode;
            OptimizationLevel = optLevel;
            EmitLLVM = emitLLVM;
            CompileToNativeCode = native;
            ForceLoopVectorization = forceLV;
            DisableBoundsChecks = disableBoundsChecks;
            DisableNullChecks = disableNullChecks;
            IsLibrary = isLibrary;
            ModuleName = moduleName;
        }
        public string ModuleName {
            get;
        }
        public int NumErrors {
            get; private set;
        }
        public bool ForceLoopVectorization {
            get;
        }
        public byte OptimizationLevel {
            get;
        }
        public bool EmitLLVM {
            get;
        }
        public bool CompileToNativeCode {
            get;
        }
        public bool DisableBoundsChecks {
            get;
        }
        public bool DisableNullChecks {
            get;
        }
        public bool IsLibrary {
            get;
        }
        public override bool DoGenerateCode {
            get;
        }
        public override bool Compile(string[] sources, string destination, string[] references) {
            var ret = base.Compile(sources, destination, references);
            NumErrors = Math.Max(ErrorCollector.NumErrors, NumErrors);
            return ret & NumErrors == 0;
        }

        FBlangParser.ProgramContext ParseSourceFile(string filename, out int numErrors) {
            TextReader inp = null;
            try {
                inp = new StreamReader(filename);
                var input = new AntlrInputStream(inp);
                var lex = new FBlangLexer(input);
                lex.AddErrorListener(ConsoleErrorListener<int>.Instance);
                /*if (filename.Contains("VectorTest")) {

                    Console.WriteLine(string.Join(" ", lex.GetAllTokens().Select(x => {
                        var nom = lex.Vocabulary.GetDisplayName(x.Type);
                        return string.IsNullOrWhiteSpace(nom) ? "<???>" : nom;
                    })));
                }*/
                var par = new FBlangParser(new CommonTokenStream(lex));
                par.AddErrorListener(ConsoleErrorListener<IToken>.Instance);
                var root = par.program();
                numErrors = par.NumberOfSyntaxErrors;
                return root;
            }
            catch (IOException e) {
                ErrorCollector.ErrorOutput.WriteLine(e.Message);
                numErrors = 1;
                return null;
            }
            finally {
                if (inp != null) {
                    inp.Close();
                }
            }
        }
        bool IsValidMainMethod(IMethod main) {
            return (main.ReturnType.IsPrimitive(PrimitiveName.Void) || main.ReturnType.IsPrimitive(PrimitiveName.Int))
                && (main.Arguments.Length == 0
                    || (main.Arguments.Last().Type.IsPrimitive() || main.Arguments.Last().Type.IsArrayOf(PrimitiveType.String))
                        && main.Arguments.Take(main.Arguments.Length - 1).All(x => x.Type.IsPrimitive() || x.Type.IsArrayOf(out var elem) && elem.IsPrimitive())
                );
        }
        protected override bool Analyze(string[] sources, string[] references, out Module mod) {
            int errc = 0;
            ErrorCollector.ResetErrors();
            var roots = sources.Select(src => {
                var ret = ParseSourceFile(src, out var numErrors);
                errc += numErrors;
                return (src, ret);
            }).Where(x => x.ret != null).ToArray();
            if (errc > 0) {
                mod = null;
                NumErrors = errc;
                return false;
            }
            module = new FBModule(ModuleName, Path.HasExtension(sources[0]) ? Path.GetFileName(sources[0]) : Path.GetDirectoryName(sources[0])) {
                WarningLevel = _warningLevel
            };
            mod = module;
            var ast1s = Array.ConvertAll(roots, root => {
                var ast1 = new ASTFirstPass(module, root.src);
                ast1.Visit(root.ret);
                return ast1;
            });

            var allTypes = ast1s.SelectMany(x => x.AllTypes);
            var allTypeTemplates = ast1s.SelectMany(x => x.AllTypeTemplates);

            var ast2 = new ASTSecondPass(module, allTypes, allTypeTemplates);
            ast2.VisitAllTypes();

            var ast3s = Array.ConvertAll(roots, root => {
                var _ast3 = new ASTThirdPass(module, allTypes, allTypeTemplates);
                _ast3.VisitGlobals(root.ret, root.src);
                return _ast3;
            });
            ast3s[0].Visit();

            var allFields = ast3s.SelectMany(x => x.AllFields);
            var allMethods = ast3s.SelectMany(x => x.AllMethods);
            var allmethodsTemplates = ast3s.SelectMany(x => x.AllMethodTemplates);
            var allMacros = ast3s.SelectMany(x => x.AllMacros);

            var ast4 = new ASTFourthPass(module, allTypes, allTypeTemplates, allFields, allMethods, allmethodsTemplates, allMacros);
            ast4.Visit();

            if (!IsLibrary && !module.MethodsByName("main").Any(IsValidMainMethod)) {
                bool hasMain = module.MethodsByName("main").Any();
                $"The executable must have a main-method{(hasMain ? "The exisiting main-methods have the wrong signature: The return-type must be int or void and the arguments must be primitive of a single string-array" : "")}".Report();
            }
            else if (IsLibrary && !ErrorCollector.HasErrors) {
                using var file = File.OpenWrite(mod.ModuleName + ".fbl");
                new FBSerializer().Serialize(module, file);
            }

            NumErrors = ErrorCollector.NumErrors;
            return !ErrorCollector.HasErrors;
        }
        // code-generator
        protected override ICodeGenerator GetCodeGenerator(string destination) {
            //return new ASTSerializer(new StreamWriter(new GZipStream(File.OpenWrite(destination), CompressionLevel.Fastest)));
            return new FBLLVMCodeGenerator(destination,
                module ?? (module = new FBModule()),
                (LLVMCodeGenerator.LLVMCodeGenerator.OptLevel) OptimizationLevel,
                EmitLLVM,
                CompileToNativeCode,
                ForceLoopVectorization,
                DisableBoundsChecks,
                DisableNullChecks,
                IsLibrary
            );
        }
        public static string InferOutputFilenameEnding(string frstInpFile, bool native, bool emitllvm, bool lib, string libEnding = null) {
            if (!native) {
                if (emitllvm)
                    return frstInpFile + ".ll";
                else
                    return frstInpFile + ".bc";
            }
            else {
                if (lib) {
                    if (libEnding != null) {
                        return frstInpFile + "." + libEnding;
                    }
                    else if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                        return frstInpFile + ".lib";
                    }
                    else {
                        return frstInpFile + ".so";
                    }
                }
                else {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                        return frstInpFile + ".exe";
                    }
                    else {
                        return frstInpFile;
                    }
                }
            }
        }
    }
}
