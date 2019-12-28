using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    public class ReferenceType : TypeImpl {
        static LazyDictionary<(IType, bool), ReferenceType> cache
            = new LazyDictionary<(IType, bool), ReferenceType>((ty_nl) => new ReferenceType(ty_nl.Item1, ty_nl.Item2));
        IType underlying;
        bool nullable;
        internal ReferenceType(IType _underlying, bool _nullable = false) {
            nullable = _nullable;
            underlying = _underlying ?? Type.Bot;
            Signature = new Type.Signature("ref" + underlying.Signature.ToString(), null);
            Context = SimpleTypeContext.GetImmutable(_underlying.Context.Module);
            var specs= Type.Specifier.Ref | Type.Specifier.Abstract | Type.Specifier.NoInheritance | Type.Specifier.Tag;
            if (!nullable)
                specs |= Type.Specifier.NotNullable;
            TypeSpecifiers = specs;
            NestedIn = null;
            DefinedIn = Context.Module;
        }
        public IType UnderlyingType => underlying;
        public override Type.Signature Signature { get; }
        public override ITypeContext Context { get; }
        public override Type.Specifier TypeSpecifiers { get; }
        public override Visibility Visibility => underlying.Visibility;
        public override Position Position => underlying.Position;
        public override IReadOnlyCollection<IType> ImplementingInterfaces { get; } = List.Empty<IType>();
        public override IType NestedIn { get; }
        public override IContext DefinedIn { get; }

        public static ReferenceType Get(IType ty) {
            if (ty is ReferenceType rf)
                return rf.AsNotNullable();
            return cache[(ty, false)];
        }
        public ReferenceType AsNotNullable() {
            if (!nullable)
                return this;
            return cache[(underlying, true)];
        }
        public ReferenceType AsNullable() {
            if (nullable)
                return this;
            return cache[(underlying, false)];
        }
        public override bool IsNestedIn(IType other) => false;
        public override bool IsSubTypeOf(IType other) {
            if (other is ReferenceType rf) {
                if (nullable && !rf.nullable)
                    return false;
                if (rf.underlying.IsConstant())
                    return underlying.IsSubTypeOf(rf.underlying);
                else
                    return underlying == rf.underlying;
            }
            return false;
        }
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (other is ReferenceType rf) {
                if (nullable && !rf.nullable) {
                    difference = int.MaxValue;
                    return false;
                }
                bool ret;
                int diff=0;
                if (rf.underlying.IsConstant())
                    ret = underlying.IsSubTypeOf(rf.underlying, out diff);
                else
                    ret = underlying == rf.underlying;
                difference = 1 + diff;
                return ret;
            }
            difference = int.MaxValue;
            return false;
        }
        public override void PrintPrefix(TextWriter tw) => underlying.PrintPrefix(tw);
        public override void PrintSuffix(TextWriter tw) => underlying.PrintSuffix(tw);
        public override void PrintValue(TextWriter tw) {
            tw.Write("ref ");
            underlying.PrintValue(tw);
        }
        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var ret = Get(underlying.Replace(genericActualParameter, curr, parent));
            return nullable ? ret.AsNullable() : ret;
        }
    }
}
