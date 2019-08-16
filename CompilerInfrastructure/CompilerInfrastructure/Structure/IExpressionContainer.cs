using CompilerInfrastructure.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IExpressionContainer {
        IEnumerable<IExpression> GetExpressions();
    }
}
