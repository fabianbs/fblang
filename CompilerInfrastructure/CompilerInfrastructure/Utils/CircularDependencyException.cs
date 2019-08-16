using System;
using System.Runtime.Serialization;

namespace CompilerInfrastructure.Utils {
    [Serializable]
    internal class CircularDependencyException : Exception {
        public CircularDependencyException() {
        }

        public CircularDependencyException(string message) : base(message) {
        }

        public CircularDependencyException(string message, Exception innerException) : base(message, innerException) {
        }

        protected CircularDependencyException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}