using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types.Generic {
    [Serializable]
    public class GenericTypeParameter : IGenericParameter, IHierarchialType {
        readonly List<IType> interfaces = new List<IType>();
        readonly IContext nestedIn;
        public GenericTypeParameter(Position pos, IContext nestedIn, string name, IHierarchialType supertype = null, params IType[] intfs) {
            Position = pos;
            this.nestedIn = nestedIn;
            Signature = new Type.Signature(name, null);
            SuperType = supertype;
            interfaces.AddRange(intfs);
            IEnumerable<ITypeContext> subctxs = intfs.Select(x => {
                if (!x.IsInterface()) {
                    $"Der Typ {x.Signature} kann nicht als Interface implementiert werden".Report(pos);
                }
                return x.Context;
            });
            if (supertype != null)
                subctxs = new[] { CompilerInfrastructure.Context.Constrained(supertype.Context, Visibility.Protected) }.Concat(subctxs);
            Context = SimpleTypeContext.Merge(nestedIn.Module, subctxs, CompilerInfrastructure.Context.DefiningRules.None);
            TypeSpecifiers = supertype != null ? supertype.TypeSpecifiers : Type.Specifier.None;
            Visibility = Visibility.Private;
        }
        public string Name => Signature.Name;
        public IHierarchialType SuperType {
            get;
            set;
        }
        public Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public ITypeContext Context {
            get;
        }
        IContext IRangeScope.Context => Context;
        public Type.Specifier TypeSpecifiers {
            get;
        }
        public Visibility Visibility {
            get;
        }
        public Position  Position {
            get;
        }
        public IReadOnlyCollection<IType> ImplementingInterfaces {
            get => interfaces;
        }
        public IType NestedIn {
            get => (nestedIn as ITypeContext)?.Type;
        }
        public IContext DefinedIn => nestedIn;
        public bool AddInterface(IType intf) {
            if (intf != null && intf.IsInterface()) {
                interfaces.Add(intf);
                return true;
            }
            return false;
        }
        public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public bool IsNestedIn(IType other) => other.Context == nestedIn;
        public bool IsSubTypeOf(IType other) {
            return other == this || SuperType != null && SuperType.IsSubTypeOf(other);
        }
        public bool IsSubTypeOf(IType other, out int difference) {
            if (other == this) {
                difference = 0;
                return true;
            }
            if (SuperType != null && SuperType.IsSubTypeOf(other, out difference)) {
                difference++;
                return true;
            }
            difference = 0;
            return false;
        }
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public bool CanReplace(ITypeOrLiteral subs) {
            if (subs is IType nwTp) {
                if (SuperType != null && !nwTp.IsSubTypeOf(SuperType)) {
                    return false;
                }
                foreach (var intf in ImplementingInterfaces) {
                    if (!nwTp.ImplementingInterfaces.Contains(intf)) {
                        return false;
                    }
                }
            }
            return true;
        }
        public IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (genericActualParameter.TryGetValue(this, out var ret)) {
                var nwTp = (IType)ret;
                if (SuperType != null && !nwTp.IsSubTypeOf(SuperType)) {
                    $"Die Ersetzung {Signature} zu {nwTp.Signature} ist aufgrund der Oberklasseneinschränkung nicht möglich".Report(curr.Position);
                }
                foreach (var intf in ImplementingInterfaces) {
                    if (!nwTp.ImplementingInterfaces.Contains(intf)) {
                        $"Der Typ {nwTp.Signature} implementiert das für die Ersetzung von {Signature} erforderliche Interface {intf.Signature} nicht".Report(curr.Position);
                    }
                }
                return nwTp;
            }
            return this;
        }

        public bool ImplementsInterface(IType intf) => ImplementingInterfaces.Contains(intf) || SuperType != null && SuperType.ImplementsInterface(intf);
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }

        public void PrintPrefix(TextWriter tw) { }
        public void PrintValue(TextWriter tw) {
            tw.Write(Name);
        }
        public void PrintSuffix(TextWriter tw) { }
        public override string ToString() => Signature.ToString();
    }
}
