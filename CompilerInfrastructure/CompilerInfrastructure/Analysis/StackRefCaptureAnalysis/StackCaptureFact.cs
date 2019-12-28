using System;
using System.Collections.Generic;

namespace CompilerInfrastructure.Analysis.StackRefCaptureAnalysis {
    public readonly struct StackCaptureFact : IEquatable<StackCaptureFact> {
        public readonly IVariable Variable;
        public readonly bool FirstClass;

        public StackCaptureFact(IVariable variable, bool firstClass) {
            Variable = variable;
            FirstClass = firstClass;
        }
        public override bool Equals(object obj) => obj is StackCaptureFact fact && Equals(fact);
        public bool Equals(StackCaptureFact other) => EqualityComparer<IVariable>.Default.Equals(Variable, other.Variable) && FirstClass == other.FirstClass;
        public override int GetHashCode() => HashCode.Combine(Variable, FirstClass);

        public static bool operator ==(StackCaptureFact left, StackCaptureFact right) => left.Equals(right);
        public static bool operator !=(StackCaptureFact left, StackCaptureFact right) => !(left == right);
        public static implicit operator StackCaptureFact((IVariable vr, bool frst) tup) {
            return new StackCaptureFact(tup.vr, tup.frst);
        }

        /// <inheritdoc />
        public override string ToString() {
            return "(" + Variable + (FirstClass ? ")" : ")*");
        }
    }
}
