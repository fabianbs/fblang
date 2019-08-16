using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class AwaitableType : AggregateType {
        
        static readonly LazyDictionary<IType, AwaitableType> cache = new LazyDictionary<IType, AwaitableType>(x => new AwaitableType(x));
        private AwaitableType(IType elem) {
            ItemType = elem;
            Signature = new Type.Signature($"async {elem.Signature.Name}", null);
            Context = new SimpleTypeContext(elem.Context.Module, CompilerInfrastructure.Context.DefiningRules.Methods) { Type = this };
            BasicMethod wait;
            Context.DefineMethod(wait = new BasicMethod(default, "wait", Visibility.Public, elem) { ReturnType = elem, NestedIn = Context });
            wait.Specifiers = Method.Specifier.Final | Method.Specifier.Internal;
        }
        public override IType ItemType {
            get;
        }
        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context {
            get;
        }
        public override Type.Specifier TypeSpecifiers {
            get;
        } = Type.Specifier.Awaitable;
        public override Visibility Visibility {
            get => ItemType.Visibility;
        }
        public override Position  Position {
            get => ItemType.Position;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();
        
        public override bool IsNestedIn(IType other) => false;
        public override bool IsSubTypeOf(IType other) => IsSubTypeOf(other, out _);
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (this == other) {
                difference = 0;
                return true;
            }
            if (other.IsError() || ItemType.IsError()) {
                difference = 0;
                return true;
            }
            difference = int.MaxValue;
            return false;
        }
        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(ItemType.Replace(genericActualParameter, curr, parent));
        }
        public static AwaitableType Get(IType elem) {
            return cache[elem];
        }

        public override void PrintPrefix(TextWriter tw) {
            ItemType.PrintPrefix(tw);
        }
        public override void PrintValue(TextWriter tw) {
            tw.Write("async ");
            ItemType.PrintValue(tw);
        }
        public override void PrintSuffix(TextWriter tw) {
            ItemType.PrintSuffix(tw);
        }
    }
}
