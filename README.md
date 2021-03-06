# FB language
This repository contains the compiler and language-runtime for my own programming language.

News
--------
- The language is completely redesigned. Thus, most of the code in this repository is outdated. I will upload the specification of the new language design soon

What is FB?
----------
The FB language is my attempt to create a general-purpose programming language. It is heavily influenced by Java, C#, C/C++, Python and Pony.

Soon I will upload a brief overview on the language.


Getting Started
---------------
The project consists of 5 different Visual Studio projects:
+ CompilerInfrastructure - Contains the abstract syntax model, the type system and some useful utility functions/classes
+ FBc - The main component, which defines the compiler and depends on each other component
+ LLVMCodeGenerator - Implements the CodeGenerator-interface from the CompilerInfrastructure to generate LLVM-IR code from an abstract syntax tree (AST)
+ LLVMInterface - Wraps commonly used functions from the LLVM IRBuilder, such that they can be used from the LLVMCodeGenerator using platform invoke
+ BasicBitcodeJIT - Contains both the language runtime-library and a wrapper over the LLVM MCJIT compiler

While the first three components are C# projects which run with .Net Core 2.1, the latter two are C/C++ projects which need at least C++14.

### Dependencies
#### External dependencies
+ .Net Core 2.1
+ boost 1.70.0
+ Boehm GC 8.0.2
+ LLVM 7.0.0
+ (Clang >= 7.0.0) optional: when this is missing, the `--native` compiler flag cannot be used

All these dependencies should be installed, before compiling the project.
#### Implicit dependencies
There are some NuGet packages, which are included by default:
+ Antlr4.CodeGenerator v4.6.5
+ Antlr4.Runtime v4.6.5
+ CommandLineParser v2.3.0

### Compatibility
Currently FB runs only on windows systems as the language runtime uses some functionality from the WinAPI. This may change later.

### Building the project
You can compile all components using Visual Studio (I used VS 17 for the C# components and VS 19 for the C/C++ components).
Due to the dependencies, please compile them in the following order:

1. LLVMInterface, BasicBitcodeJIT, CompilerInfrastructure
2. LLVMCodeGenerator
3. FBc


### Using the FB compiler
For compiling a script called `fbc` is provided.
It can be parametrized with one or more source-files, which should be compiled and the following list of options:

|option|description|
----------|---------------
  |-A                       | (Default: false) Disables code generation => only runs the analyzers and with --Lib generates a summary|
  |-o                        |The name of the binary-file. Will be inferred from the first specified sourcefile if missing.|
  |-O                      |  (Default: 1) The optimization-level the compiler should use; can be O0, O1, O2, or O3; the default is O1|
  |--emit-llvm             |  (Default: false) Generate the LLVM intermediate representation instead of LLVM bitcode. Cannot be used together with -native|
 | --native                 | (Default: false) Generate a native executable or library. Cannot be used together with -emit-llvm|
 | --force-loop-vectorize  |  (Default: false) Tries to vectorize all loops. This can result in a much larger output-file|
 | --disable-range-checks   | (Default: false) Disables all array-bounds checks|
  |--disable-null-checks   |  (Default: false) Disables all null-dereference checks|
 | -L, --Lib               |  (Default: false) Generate a library instead of an executable application|
 | --help                  |  Display this help screen.|
  |--version                | Display version information.
  
Note: When a directory is specified instead of a source file, all files in this directory will be included (the files will not be searched recursively)
