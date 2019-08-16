using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Semantics {
    public readonly struct FunctionProperty {
        public readonly Func<IStatement, bool> CheckStatement;
        public readonly Func<IExpression, bool> CheckExpression;
        public readonly Func<IDeclaredMethod, bool> CheckDefinition;
        public FunctionProperty(Func<IStatement, bool> _stmtProperty, Func<IExpression, bool> _exprProperty, Func<IDeclaredMethod, bool> _specifier) {
            CheckStatement = _stmtProperty ?? throw new ArgumentNullException(nameof(_stmtProperty));
            CheckExpression = _exprProperty ?? throw new ArgumentNullException(nameof(_exprProperty));
            CheckDefinition = _specifier ?? throw new ArgumentNullException(nameof(_specifier));
        }
        public bool IsInitialized => CheckStatement != null && CheckExpression != null && CheckDefinition != null;
    }
    public static class FunctionCharacteristics {

        public static bool Check(this IDeclaredMethod m, FunctionProperty prop, out Position pos) {
            pos = default;
            if (m is null)
                return false;
            if (!prop.IsInitialized)
                return true;
            if (m.Body.HasValue) {
                foreach (var stmt in m.Body.Instruction.GetStatementsRecursively(true)) {
                    if (prop.CheckStatement(stmt)) {
                        pos = stmt.Position;
                        return true;
                    }
                    foreach (var ex in stmt.GetExpressionsRecursively(true)) {
                        if (prop.CheckExpression(ex)) {
                            pos = ex.Position;
                            return true;
                        }
                    }
                }
                return false;
            }
            return prop.CheckDefinition(m);
        }
        public static bool MayHaveSideEffects(this IDeclaredMethod m, out Position pos) {
            return Check(
                m, 
                new FunctionProperty(
                    stmt => stmt.MayHaveSideEffects(), 
                    ex => ex.MayHaveSideEffects(), 
                    met => !met.Specifiers.HasFlag(Method.Specifier.SideEffectFree)
                )
                , out pos
            );
        }
    }
}
