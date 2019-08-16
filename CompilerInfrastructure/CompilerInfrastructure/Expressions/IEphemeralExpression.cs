using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Expressions {
    public interface IEphemeralExpression : IExpression {
        bool IsEphemeral {
            get;
        }
    }
    public static class EphemeralExpression {
        public static bool IsAnyEphemeral(IReadOnlyCollection<IExpression> ex) {
            return ex != null && ex.Any(x => x is IEphemeralExpression eph && eph.IsEphemeral);
        }
        public static bool IsAnyEphemeralOrThis(IReadOnlyCollection<IExpression> ex) {
            return ex != null && ex.Any(x => x is IEphemeralExpression eph && eph.IsEphemeral || x is ThisExpression);
        }
        public static bool IsAnyEphemeralOrVariable(IReadOnlyCollection<IExpression> ex) {
            return ex != null && ex.Any(x => x is IEphemeralExpression eph && eph.IsEphemeral || x is ThisExpression || x is VariableAccessExpression);
        }
    }
}
