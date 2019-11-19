using CompilerInfrastructure;
using CompilerInfrastructure.Analysis;
using CompilerInfrastructure.Analysis.TaintAnalysis;
using CompilerInfrastructure.Analysis.ParameterCaptureAnalysis;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;
using CompilerInfrastructure.Semantics;
using System.Linq;

namespace CITest {
    public class ParameterCaptureAnalysisTest {
        ITestOutputHelper cout;
        Module mod;
        CaptureAnalysisDescription desc;
        InterMonoAnalysis<TaintAnalysisSummary<IVariable>, IVariable> analysis;
        public ParameterCaptureAnalysisTest(ITestOutputHelper _cout) {
            cout = _cout;
            mod = new Module();
            analysis = new InterMonoAnalysis<TaintAnalysisSummary<IVariable>, IVariable>(new TaintAnalysisSummary<IVariable>(), new BasicSemantics());
            desc = new CaptureAnalysisDescription(analysis);
            analysis.SetAnalysis(desc);
        }
        [Fact]
        public void TestSimpleFunction() {
            
            var met = new BasicMethod(default, "func",Visibility.Public,PrimitiveType.Int,new[]{
                new BasicVariable(default, PrimitiveType.Int, Variable.Specifier.LocalVariable, "x", null),
                new BasicVariable(default, PrimitiveType.Double,Variable.Specifier.LocalVariable, "y",null)
            } );
            met.NestedIn = mod;
            met.Specifiers |= Method.Specifier.Static;
            met.Body.Instruction = new ReturnStatement(default, new VariableAccessExpression(default, null, met.Arguments[0]));

            
            var result = analysis.Query(met);
            Assert.Empty(result.Leaks);
            Assert.Equal(2, result.MaximalFixpoint.Count);
            Assert.Single(result.ReturnFacts);
            Assert.Equal(met.Arguments[0], result.ReturnFacts.FirstOrDefault());
        }
        [Fact]
        public void TestSimpleLeak() {
            var met = new BasicMethod(default, "func",Visibility.Public,PrimitiveType.Void, new[]{
                new BasicVariable(default, PrimitiveType.Int, Variable.Specifier.LocalVariable, "x", null),
                new BasicVariable(default, PrimitiveType.Int.AsByRef(), Variable.Specifier.LocalVariable, "y",null)
            } );
            met.NestedIn = mod;
            met.Specifiers |= Method.Specifier.Static;
            met.Body.Instruction = new BlockStatement(default, new IStatement[] {
                new ExpressionStmt(default, new BinOp(default, null,
                    new VariableAccessExpression(default, null, met.Arguments[1]),
                    BinOp.OperatorKind.ASSIGN_NEW,
                    new VariableAccessExpression(default, null, met.Arguments[0]))
                ),
                new ReturnStatement(default)
            }, mod.NewScope(true));

            var result = analysis.Query(met);
            Assert.Single(result.Leaks);
            Assert.Single(result.Leaks.Values.FirstOrDefault());
            Assert.Equal(met.Arguments[0], result.Leaks.Values.FirstOrDefault()?.FirstOrDefault());
            Assert.Equal(2, result.MaximalFixpoint.Count);
           
        }
    }
}
