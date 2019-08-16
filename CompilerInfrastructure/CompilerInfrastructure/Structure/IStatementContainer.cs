using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IStatementContainer {
        IEnumerable<IStatement> GetStatements();
    }
    public static class ContainerHelper {
        public static IEnumerable<IExpression> GetExpressionFromStatements(this IStatementContainer stc) {
            var ret = //Enumerable.Empty<IExpression>();
                stc is IStatement stat ? stat.GetExpressions() : Enumerable.Empty<IExpression>();
            foreach (var stmt in stc.GetStatements()) {
                ret = ret.Concat(stmt.GetExpressions());
            }
            return ret;
        }
        public static IEnumerable<IExpression> GetExpressionsRecursively(this IExpressionContainer exc, bool containThis=false) {
            if (exc is null)
                yield break;
            if (containThis && exc is IExpression ex)
                yield return ex;
            foreach(var e in exc.GetExpressions()) {
                yield return e;
                foreach(var subex in e.GetExpressionsRecursively()) {
                    yield return subex;
                }
            }
        }
        public static IEnumerable<IStatement> GetStatementsRecursively(this IStatementContainer stc, bool containThis=false) {
            if (stc is null)
                yield break;
            if (containThis && stc is IStatement stmt)
                yield return stmt;
            foreach (var s in stc.GetStatements()) {
                yield return s;
                foreach (var subs in s.GetStatementsRecursively()) {
                    yield return subs;
                }
            }
        }

    }
}
