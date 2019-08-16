using System;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PInvokeGenerator {
    class Library : ILibrary {
        public void Postprocess(Driver driver, ASTContext ctx) {
        }
        public void Preprocess(Driver driver, ASTContext ctx) {
        }
        public void Setup(Driver driver) {
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            driver.ParserOptions.LibraryFile = "LLVMInterface.dll";
            driver.ParserOptions.AddIncludeDirs(@"D:\Besitzer\Documents\Visual Studio 2017\Projects\LLVMInterface\LLVMInterface\");
            options.AddModule("LLVMInterface");
            options.OutputDir = ".";
            
        }
        public void SetupPasses(Driver driver) {
        }
    }
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestMethod1() {
            ConsoleDriver.Run(new Library());
        }
        public static void Main() {
            new UnitTest1().TestMethod1();
        }
    }
}
