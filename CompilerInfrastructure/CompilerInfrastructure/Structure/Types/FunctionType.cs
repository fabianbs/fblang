using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class FunctionType : IType, IEquatable<FunctionType> {
        readonly Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, FunctionType> genericCache
            = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, FunctionType>();
        public FunctionType(Position pos, Module mod, string name, IType retType, ICollection<IType> argTypes, Visibility vis) {
            ReturnType = retType ?? PrimitiveType.Void;
            ArgumentTypes = argTypes ?? Array.Empty<IType>();
            Context = SimpleTypeContext.GetImmutable(mod);
            Visibility = vis;
            Position = pos;
            Name = name;
            Signature = new Type.Signature($"{name}: {string.Join(", ", ArgumentTypes.Select(x => x.Signature.ToString()))} -> {ReturnType.Signature.ToString()}", null);
        }
        public string Name {
            get;
        }
        public IType ReturnType {
            get;
        }
        public ICollection<IType> ArgumentTypes {
            get;
        }
        public ITypeContext Context {
            get;
        }
        public Type.Specifier TypeSpecifiers {
            get;
        } = Type.Specifier.Function;
        public IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();
        public IType NestedIn {
            get; set;
        }
        public Visibility Visibility {
            get;
        }
        public Position Position {
            get;
        }
        public Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        IContext IRangeScope.Context {
            get => Context;
        }
        public IContext DefinedIn => Context.Module;

        public override bool Equals(object obj) => Equals(obj as FunctionType);
        public bool Equals(FunctionType other) => other != null && Name == other.Name && EqualityComparer<IType>.Default.Equals(ReturnType, other.ReturnType) && EqualityComparer<ICollection<IType>>.Default.Equals(ArgumentTypes, other.ArgumentTypes);
        public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();

        public override int GetHashCode() {
            var hashCode = 776822132;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<IType>.Default.GetHashCode(ReturnType);
            hashCode = hashCode * -1521134295 + EqualityComparer<ICollection<IType>>.Default.GetHashCode(ArgumentTypes);
            return hashCode;
        }

        public bool ImplementsInterface(IType intf) => false;
        public bool IsNestedIn(IType other) => other == NestedIn;
        public virtual bool IsSubTypeOf(IType other) => other == this;
        public virtual bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            return other == this;
        }

        public IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                ret = new FunctionType(Position,
                    Context.Module,
                    Name,
                    ReturnType.Replace(genericActualParameter, curr, parent),
                    ArgumentTypes.Select(x => x.Replace(genericActualParameter, curr, parent)).AsCollection(ArgumentTypes.Count),
                    Visibility
                );
            }
            return ret;
        }
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }

        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public static FunctionType FromMethod(IMethod method) {
            return new FunctionType(method.Position, (method as IDeclaredMethod).Context.Module, method.Signature.Name, method.ReturnType, method.Arguments.Select(x => x.Type).AsCollection(method.Arguments.Length), method.Visibility) { NestedIn = (method.NestedIn as ITypeContext)?.Type };
        }

        public void PrintValue(TextWriter tw) {
            ReturnType.PrintTo(tw);
            tw.Write(" ");
            tw.Write(Name);
            tw.Write("(");
            if (ArgumentTypes.Any()) {
                ArgumentTypes.First().PrintTo(tw);
                foreach (var argTp in ArgumentTypes.Skip(1)) {
                    tw.Write(", ");
                    argTp.PrintTo(tw);
                }
            }
            tw.Write(")");
        }
        public void PrintPrefix(TextWriter tw) { }
        public void PrintSuffix(TextWriter tw) { }
    }
}
