using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    /// <summary>
    /// Wrappertype for dealing with namespaces in contexts
    /// </summary>
    /// <seealso cref="CompilerInfrastructure.TypeImpl" />
    [Serializable]
    public class NamespaceType : TypeImpl {
        readonly LazyDictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent), IType> genericCache;

        public NamespaceType(Position pos, string name, IType nestedIn, IContext innerCtx, Visibility vis = Visibility.Public, IContext definedIn = null) {
            Signature = new Type.Signature(name, null);

            Context = new SimpleTypeContext(innerCtx.Module, CompilerInfrastructure.Context.DefiningRules.None, null, innerCtx, pos);
            Visibility = vis;
            Position = pos;
            Parent = nestedIn;
            DefinedIn = definedIn ?? Context.Module;
            genericCache = new LazyDictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent), IType>(
                x => ReplaceImpl(x.genericActualParameter, x.curr, x.parent)
            );
        }
        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context {
            get;
        }
        public override Type.Specifier TypeSpecifiers {
            get => Type.Specifier.Abstract | Type.Specifier.NoInheritance;
        }
        public override Visibility Visibility {
            get;
        }
        public override Position Position {
            get;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();
        public IType Parent {
            get;
        }

        public override IType NestedIn {
            get => Parent;
        }
        public override IContext DefinedIn { get; }

        public override bool IsNestedIn(IType other) => other == Parent;
        public override bool IsSubTypeOf(IType other) => other == this || other.IsError() || other.IsTop();
        public override bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            return IsSubTypeOf(other);
        }

        public override void PrintPrefix(TextWriter tw) { }
        public override void PrintSuffix(TextWriter tw) { }
        public override void PrintValue(TextWriter tw) {
            tw.Write(Signature.ToString());
        }

        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return genericCache[(genericActualParameter, curr, parent)];
        }
        public IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new NamespaceType(Position,
                Signature.Name,
                Parent.Replace(genericActualParameter, curr, parent),
                Context.Replace(genericActualParameter, curr, parent),
                Visibility
            );
        }
    }
}
