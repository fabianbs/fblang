using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;

namespace UnitTestProject1 {
    [TestClass]
    public class UnitTest1 {
        const string filename = "D:\\Besitzer\\Desktop\\TestModule.bc";
        [TestMethod]
        public void TestMethod1() {
            Console.WriteLine("Starting...");
            var cg = new LLVMCodeGenerator.LLVMCodeGenerator(filename);

            var mod = new Module("MyNewModule");

            var met = new BasicMethod(new Position(filename, 0, 0), "getBigLong", Visibility.Public, PrimitiveType.BigLong) {
                Specifiers = Method.Specifier.Static,
                Context = SimpleMethodContext.GetImmutable(mod)
            };
            met.Body.Instruction = new ReturnStatement(new Position(filename, 1, 4), new BigLongLiteral(new Position(filename, 1, 11), (BigInteger)ulong.MaxValue + 1, true));
            mod.DefineMethod(met);
            cg.CodeGen(mod);
            Console.WriteLine("Finished");
            //Console.ReadKey(true);
        }
    }
}
