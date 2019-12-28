using System;
using System.Collections.Generic;
using System.Text;

namespace CITest {
    using System.Linq;
    using CompilerInfrastructure;
    using CompilerInfrastructure.Analysis;
    using CompilerInfrastructure.Analysis.ParameterCaptureAnalysis;
    using CompilerInfrastructure.Analysis.StackRefCaptureAnalysis;
    using CompilerInfrastructure.Analysis.TaintAnalysis;
    using CompilerInfrastructure.Expressions;
    using CompilerInfrastructure.Instructions;
    using CompilerInfrastructure.Semantics;
    using CompilerInfrastructure.Structure;
    using CompilerInfrastructure.Structure.Types;
    using Xunit;
    using Xunit.Abstractions;

    public class StackRefCaptureTest {
        ITestOutputHelper                                                                    cout;
        readonly Module                                                                      mod;
        readonly StackRefCaptureAnalysisDescription                                          desc;
        readonly InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact> analysis;

        public StackRefCaptureTest(ITestOutputHelper _cout) {
            cout = _cout;
            mod = new Module();
            analysis = new InterMonoAnalysis<TaintAnalysisSummary<StackCaptureFact>, StackCaptureFact>(
                new TaintAnalysisSummary<StackCaptureFact>(), new BasicSemantics());
            desc = new StackRefCaptureAnalysisDescription(analysis);
            analysis.SetAnalysis(desc);
        }

        [Fact]
        public void TestSimpleFunction() {
            var met = new BasicMethod(default, "func", Visibility.Public, PrimitiveType.Int.AsReferenceType(), new[] {
                new BasicVariable(default, PrimitiveType.Int, Variable.Specifier.LocalVariable, "x", null),
            }) {
                NestedIn = mod
            };
            met.Specifiers |= Method.Specifier.Static;
            met.Body.Instruction = new ReturnStatement(default,
                                                       new ReferenceExpression(default,
                                                                               new VariableAccessExpression(
                                                                                   default, null, met.Arguments[0])));
            var result = analysis.Query(met);

            cout.WriteLine(result.ToString());
            Assert.NotEmpty(result.Leaks);
            Assert.Single(result.Leaks);
            Assert.Contains(result.Leaks.First().Value, x => x.FirstClass && x.Variable == met.Arguments[0]);

        }
    }
}
