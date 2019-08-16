using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure {
    public interface ISignature {
        string Name {
            get;
        }
    }
    [Serializable]
    public struct StringSignature : ISignature, IEquatable<StringSignature> {
        public string Name {
            get;
        }
        public StringSignature(string nom) {
            Name = nom;
        }

        public override bool Equals(object obj) => obj is StringSignature signature && Equals(signature);
        public bool Equals(StringSignature other) => Name == other.Name;
        public override int GetHashCode() => 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);

        public static bool operator ==(StringSignature left, StringSignature right) => left.Equals(right);
        public static bool operator !=(StringSignature left, StringSignature right) => !(left == right);

        public static implicit operator StringSignature(string s) {
            return new StringSignature(s);
        }
        public static explicit operator string(StringSignature s) {
            return s.Name;
        }
        public override string ToString() => Name;
    }
}
