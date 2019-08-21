/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static CompilerInfrastructure.Utils.CoreExtensions;
using RTMethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using RTMethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;


namespace CompilerInfrastructure {
    public interface IType : IVisible, ISourceElement, ITypeOrLiteral, IRangeScope, IReplaceableStructureElement<IType>, ISigned<Type.Signature>, IPrintable {
        new IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        new ITypeContext Context {
            get;
        }
        Type.Specifier TypeSpecifiers {
            get;
        }
        IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        }
        bool IsSubTypeOf(IType other);
        bool IsSubTypeOf(IType other, out int difference);
        bool IsNestedIn(IType other);
        IType NestedIn {
            get;
        }
        IContext DefinedIn {
            get;
        }
        bool ImplementsInterface(IType intf);
    }
    [Serializable]
    public abstract class TypeImpl : IType {
        public abstract Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public abstract ITypeContext Context {
            get;
        }
        public abstract Type.Specifier TypeSpecifiers {
            get;
        }
        public abstract Visibility Visibility {
            get;
        }
        public abstract Position Position {
            get;
        }
        public abstract IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        }
        IContext IRangeScope.Context => Context;

        public abstract IType NestedIn {
            get;
        }
        public abstract IContext DefinedIn {
            get;
        }

        /* protected virtual IEnumerable<IASTNode> Children() {
             //DOLATER richtig so??
             foreach (var x in Context.Variables) {
                 yield return x.Value;
             }
             foreach (var x in Context.Methods) {
                 yield return x.Value;
             }
             foreach (var x in Context.Types) {
                 yield return x.Value;
             }
         }
         public virtual IRefEnumerator<IASTNode> GetEnumerator() {
             return RefEnumerator.ConcatMany(Children());
         }*/
        public abstract bool IsNestedIn(IType other);
        public abstract bool IsSubTypeOf(IType other);
        public abstract bool IsSubTypeOf(IType other, out int difference);
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public abstract IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual bool ImplementsInterface(IType intf) => ImplementingInterfaces.Contains(intf);
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }

        public override string ToString() {
            return this.PrintString();
        }

        public abstract void PrintPrefix(TextWriter tw);
        public abstract void PrintValue(TextWriter tw);
        public abstract void PrintSuffix(TextWriter tw);
    }
    [Serializable]
    public abstract class ModifyingTypeImpl : IType {
        public abstract Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public abstract ITypeContext Context {
            get; set;
        }
        public abstract Type.Specifier TypeSpecifiers {
            get; set;
        }
        public abstract Visibility Visibility {
            get;
        }
        public abstract Position Position {
            get;
        }
        public abstract IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        }
        IContext IRangeScope.Context => Context;

        public abstract IType NestedIn {
            get;
        }
        public abstract IContext DefinedIn {
            get;
        }

        public override string ToString() {
            return this.PrintString();
        }

        protected virtual IEnumerable<IASTNode> Children() {
            //DOLATER richtig so??
            foreach (var x in Context.Variables) {
                yield return x.Value;
            }
            foreach (var x in Context.Methods) {
                yield return x.Value;
            }
            foreach (var x in Context.Types) {
                yield return x.Value;
            }
        }
        /* public virtual IRefEnumerator<IASTNode> GetEnumerator() {
             return RefEnumerator.ConcatMany(Children());
         }*/
        public abstract bool IsNestedIn(IType other);
        public abstract bool IsSubTypeOf(IType other);
        public abstract bool IsSubTypeOf(IType other, out int difference);
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public abstract IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual bool ImplementsInterface(IType intf) => ImplementingInterfaces.Contains(intf);
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }

        public abstract void PrintPrefix(TextWriter tw);
        public abstract void PrintValue(TextWriter tw);
        public abstract void PrintSuffix(TextWriter tw);
    }
    /// <summary>
    /// Provides extension-methods for <see cref="IType"/>
    /// </summary>
    public static class TypeHelper {
        public static bool IsPrimitive(this IType tp) {
            return tp == null || tp.TypeSpecifiers.HasFlag(Type.Specifier.Primitive);
        }
        public static bool IsPrimitive(this IType tp, PrimitiveName name) {
            return tp.IsPrimitive() && tp.TryCast<PrimitiveType>(out var prim) && prim.PrimitiveName == name;
        }
        public static bool IsArray(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Array);
        }
        public static bool IsArrayOf(this IType tp, IType itemTy) {
            if (tp.IsArray()) {
                return tp.Cast<AggregateType>().ItemType == itemTy;
            }
            return false;
        }
        public static bool IsArrayOf(this IType tp, out IType itemTy) {
            if (tp.IsArray()) {
                itemTy = tp.Cast<AggregateType>().ItemType;
                return true;
            }
            itemTy = null;
            return false;
        }
        public static bool IsArraySlice(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.ArraySlice);
        }
        public static bool IsFixedSizedArray(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.FixedSizedArray);
        }
        public static bool IsNativePointer(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.NativePointer);
        }
        public static bool IsAbstract(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Abstract);
        }
        public static bool IsInterface(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Interface);
        }
        public static bool IsImmutable(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Immutable);
        }
        public static bool IsConstant(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Constant);
        }
        public static bool IsUnique(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Unique);
        }
        public static bool IsActor(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Actor);
        }
        public static bool IsVarArg(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.VarArg);
        }
        public static bool CanBeInherited(this IType tp) {
            return tp != null && !tp.TypeSpecifiers.HasFlag(Type.Specifier.NoInheritance);
        }
        public static bool IsByRef(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.ByRef);
        }
        public static bool IsByRef(this IType tp, out ByRefType brt) {
            if (IsByRef(tp)) {
                brt = tp.Cast<ByRefType>();
                return true;
            }
            brt = null;
            return false;
        }
        public static bool IsByConstRef(this IType tp) {
            return tp.IsByRef() && tp.IsConstant() && tp.TryCastWhere<RefConstrainedType>(out _, rct => !rct.ReferenceCapability.CanWrite());
        }
        public static bool IsByConstRef(this IType tp, out ByRefType brt) {
            if (tp.IsByRef() && tp.IsConstant() && tp.TryCastWhere<RefConstrainedType>(out var rct, r => !r.ReferenceCapability.CanWrite())) {
                brt = rct.Cast<ByRefType>();
                return true;
            }
            brt = null;
            return false;
        }
        public static bool IsByMutableRef(this IType tp) {
            return tp.IsByRef() && !tp.TryCastWhere<RefConstrainedType>(out _, rct => !rct.ReferenceCapability.CanWrite());
        }
        public static bool IsTag(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Tag);
        }
        public static bool IsTransition(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Transition);
        }
        public static bool IsNumericType(this IType tp) {
            return tp is PrimitiveType prim && prim.IsNumeric;
        }
        public static bool IsIntegerType(this IType tp) {
            return tp.IsPrimitive() && tp.TryCast<PrimitiveType>(out var prim) && prim.IsInteger;
        }
        public static bool IsUnsignedNumericType(this IType tp) {
            return tp.IsPrimitive() && tp.TryCast<PrimitiveType>(out var prim) && prim.IsUnsignedInteger;
        }
        public static bool IsFloatingPointType(this IType tp) {
            return tp.IsPrimitive() && tp.TryCast<PrimitiveType>(out var prim) && prim.IsFloatingPoint;
        }
        public static bool IsString(this IType tp) {
            return tp == PrimitiveType.String;
        }
        public static bool IsVoid(this IType tp) {
            return tp is null || tp == PrimitiveType.Void;
        }
        public static bool IsAwaitable(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Awaitable);
        }
        public static bool IsImport(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.Import);
        }
        public static bool IsValueType(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.ValueType);
        }
        public static bool IsNullType(this IType tp) {
            return tp is PrimitiveType prim && prim.PrimitiveName == PrimitiveName.Null;
        }
        public static bool IsNotNullable(this IType tp) {
            return tp != null && tp.TypeSpecifiers.HasFlag(Type.Specifier.NotNullable);
        }

        public static IType AsArray(this IType tp) {
            return ArrayType.Get(tp);
        }
        public static ByRefType AsByRef(this IType tp) {
            return ByRefType.Get(tp);
        }
        public static SpanType AsSpan(this IType tp) {
            return SpanType.Get(tp);
        }
        public static VarArgType AsVarArg(this IType tp) {
            return VarArgType.Get(tp);
        }
        public static FixedSizedArrayType AsFixedSizedArray(this IType tp, uint length) {
            return FixedSizedArrayType.Get(tp, length);
        }
        public static FixedSizedArrayType AsFixedSizedArray(this IType tp, ILiteral length) {
            return FixedSizedArrayType.Get(tp, length);
        }
        public static AwaitableType AsAwaitable(this IType tp) {
            return AwaitableType.Get(tp);
        }
        public static bool IsError(this IType tp) {
            return tp == ErrorType.Instance;
        }
        public static bool IsTop(this IType tp) {
            return tp != null && tp.UnWrapAll() == TopType.Instance;
        }
        public static bool IsBot(this IType tp) {
            return tp != null && tp.UnWrapAll() == BottomType.Instance;
        }
        public static bool IsTopOrBot(this IType tp) {
            if (tp is null)
                return false;
            var uwt = tp.UnWrapAll();
            return uwt == TopType.Instance || uwt == BottomType.Instance;
        }
        public static RefConstrainedType AsImmutable(this IType tp) {
            return RefConstrainedType.Get(tp, ReferenceCapability.val);
        }
        public static RefConstrainedType AsUnique(this IType tp) {
            return RefConstrainedType.Get(tp, ReferenceCapability.iso);
        }
        public static RefConstrainedType AsConstant(this IType tp) {
            return RefConstrainedType.Get(tp, ReferenceCapability.box);
        }
        public static RefConstrainedType AsTransition(this IType tp) {
            return RefConstrainedType.Get(tp, ReferenceCapability.trn);
        }
        public static RefConstrainedType AsTag(this IType tp) {
            return RefConstrainedType.Get(tp, ReferenceCapability.tag);
        }
        public static IType AsValueType(this IType tp) {
            return ReferenceValueType.Get(tp, true);
        }
        public static IType AsReferenceType(this IType tp) {
            return ReferenceValueType.Get(tp, false);
        }
        public static IType AsNotNullable(this IType tp) {
            return NotNullableType.Get(tp);
        }
        public static IType UnWrap(this IType tp) {
            if (tp is ModifierType mtp)
                return mtp.UnderlyingType;
            return tp;
        }

        public static IType UnWrapAll(this IType tp) {
            while (tp is ModifierType mtp)
                tp = mtp.UnderlyingType;
            return tp;
        }
        public static bool OverloadsOperator(this IType tp, OverloadableOperator op, out IEnumerable<IDeclaredMethod> overloads,
            SimpleMethodContext.VisibleMembers vis = SimpleMethodContext.VisibleMembers.Both) {
            var suffix = op.OperatorName();
            var name = "operator " + suffix;
            bool
                unary = op.IsUnary(),
                binary = op.IsBinary(),
                ternary = op.IsTernary();

            IEnumerable<IDeclaredMethod> inst, stat;
            if (vis.HasFlag(SimpleMethodContext.VisibleMembers.Instance)) {

                IEnumerable<IDeclaredMethod> instMets = tp.Context.InstanceContext.MethodsByName(name);
                IEnumerable<IDeclaredMethod> instTms = tp.Context.InstanceContext.MethodTemplatesByName(name);

                inst = instMets.Concat(instTms).Where(x => x.Specifiers.HasFlag(Method.Specifier.OperatorOverload));
                if (unary)
                    inst = inst.Where(x => x.Arguments.Length == 0);
                else if (binary)
                    inst = inst.Where(x => x.Arguments.Length == 1);
                else if (ternary)
                    inst = inst.Where(x => x.Arguments.Length == 2);
            }
            else
                inst = Enumerable.Empty<IDeclaredMethod>();
            if (vis.HasFlag(SimpleMethodContext.VisibleMembers.Static)) {
                IEnumerable<IDeclaredMethod> statMets = tp.Context.StaticContext.MethodsByName(name);
                IEnumerable<IDeclaredMethod> statTms = tp.Context.StaticContext.MethodTemplatesByName(name);

                stat = statMets.Concat(statTms).Where(x => x.Specifiers.HasFlag(Method.Specifier.OperatorOverload));
                if (unary)
                    stat = stat.Where(x => x.Arguments.Length == 1);
                else if (binary)
                    stat = stat.Where(x => x.Arguments.Length == 2);
                else if (ternary)
                    stat = stat.Where(x => x.Arguments.Length == 3);
            }
            else
                stat = Enumerable.Empty<IDeclaredMethod>();

            overloads = inst.Concat(stat);
            return overloads.Any();
        }
        public static string FullName(this IType tp) {
            if (tp is null)
                return null;
            if (tp.IsError())
                return "<error-type>";
            if (tp.IsTop())
                return "<any-type>";
            if (tp.NestedIn != null) {
                return tp.NestedIn.FullName() + "." + tp.Signature;
            }
            else
                return tp.Signature.ToString();
        }

        [RTMethodImpl(RTMethodImplOptions.AggressiveInlining)]
        public static T Cast<T>(this IType tp) where T : IType {
            if (tp is null)
                throw new InvalidCastException("Cannot cast null to " + typeof(T).FullName);
            else if (tp is T t)
                return t;
            else {
                var uw = tp.UnWrap();
                if (uw is T tt)
                    return tt;
                else if (uw != tp)
                    return Cast<T>(uw);
                else
                    throw new InvalidCastException($"Cannot cast {tp} to {typeof(T).FullName}");
            }
        }
        [RTMethodImpl(RTMethodImplOptions.AggressiveInlining)]
        public static bool TryCast<T>(this IType tp, out T ret) where T : IType {
            if (tp is null) {
                ret = default;
                return false;
            }
            else if (tp is T t) {
                ret = t;
                return true;
            }
            else {
                var uw = tp.UnWrap();
                if (uw is T tt) {
                    ret = tt;
                    return true;
                }
                else if (uw != tp) {
                    return TryCast(uw, out ret);
                }
                else {
                    ret = default;
                    return false;
                }
            }
        }
        [RTMethodImpl(RTMethodImplOptions.AggressiveInlining)]
        public static bool TryCastWhere<T>(this IType tp, out T ret, Func<T, bool> predicate) {
            if (tp is null) {
                ret = default;
                return false;
            }
            else if (tp is T t && (predicate is null || predicate(t))) {
                ret = t;
                return true;
            }
            else {
                var uw = tp.UnWrap();
                if (uw is T tt && (predicate is null || predicate(tt))) {
                    ret = tt;
                    return true;
                }
                else if (uw != tp) {
                    return TryCastWhere(uw, out ret, predicate);
                }
                else {
                    ret = default;
                    return false;
                }
            }
        }
        public static bool MayRelyOnGenericParameters(this IType @this) {
            if (@this is IGenericParameter)
                return true;
            if (@this.Signature.BaseGenericType != null) {
                foreach (var arg in @this.Signature.GenericActualArguments) {
                    if (arg is IGenericParameter)
                        return true;
                    else if (arg is IType ty && ty.MayRelyOnGenericParameters())
                        return true;
                }
            }
            if (@this.NestedIn != null) {
                return @this.NestedIn.MayRelyOnGenericParameters();
            }
            return false;
        }
    }
    /// <summary>
    /// Provides nested classes and static methods for <see cref="IType"/>
    /// </summary>
    public static class Type {
        [Serializable]
        public class Signature : ISignature, IEquatable<Signature>, ICloneable {
            public Signature(string name, IReadOnlyList<ITypeOrLiteral> genericActualArguments) : this(name, genericActualArguments, false) {

            }
            internal Signature(string name, IReadOnlyList<ITypeOrLiteral> genericActualArguments, bool allowEmptyName) {
                if (!allowEmptyName && string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("A typename must not be empty", nameof(name));

                Name = name;
                GenericActualArguments = EquatableCollection.FromIList(genericActualArguments);
            }
            public virtual string Name {
                get;
            }
            public EquatableCollection<ITypeOrLiteral> GenericActualArguments {
                get;
            }
            object ICloneable.Clone() => Clone();
            public Signature Clone() {
                return new Signature(Name, GenericActualArguments) { BaseGenericType = BaseGenericType };
            }
            public override bool Equals(object obj) => Equals(obj as Signature);
            public bool Equals(Signature other) =>
                other != null &&
                Name == other.Name &&
                EqualityComparer<EquatableCollection<ITypeOrLiteral>>.Default.Equals(GenericActualArguments, other.GenericActualArguments);
            public override int GetHashCode() => HashCode.Combine(Name, GenericActualArguments);

            public static bool operator ==(Signature signature1, Signature signature2) => EqualityComparer<Signature>.Default.Equals(signature1, signature2);
            public static bool operator !=(Signature signature1, Signature signature2) => !(signature1 == signature2);
            public override string ToString() {
                var sb = new StringBuilder();

                sb.Append(Name);
                if (GenericActualArguments.Any()) {
                    sb.Append('<');

                    sb.Append(string.Join(", ", GenericActualArguments.Select(x => {
                        if (x is IType tp) {
                            return tp.Signature.ToString();
                        }
                        else if (x is ILiteral lit) {
                            return lit.ToString();
                        }
                        return "<ERROR>";
                    })));

                    sb.Append('>');
                }

                return sb.ToString();
            }

            public ITypeTemplate<IType> BaseGenericType {
                get; set;
            }
        }
        [Serializable]
        [Flags]
        public enum Specifier {
            None = 0x00,
            Array = 0x01 | NoInheritance,
            FixedSizedArray = 0x02 | Array | ValueType,
            NativePointer = 0x04 | Primitive,
            ArraySlice = 0x08 | NotNullable,
            Unique = 0x10,
            Constant = 0x20,
            Immutable = 0x40 | Constant,
            NoInheritance = 0x80,
            Abstract = 0x100,
            Interface = 0x200 | Abstract,
            Actor = 0x400,
            Primitive = 0x800 | NoInheritance | Immutable,
            ByRef = 0x1000,
            VarArg = 0x2000,
            Enum = 0x4000 | Immutable | NoInheritance,
            Awaitable = 0x8000,
            Transition = 0x10000,
            Tag = 0x20000,
            Import = 0x40000,
            Function = 0x80000,
            NotNullable = 0x100000,
            ValueType = 0x200000 | NotNullable,
        }
        public static IType GetPrimitive(string name) {
            if (Enum.TryParse<PrimitiveName>(name, out var pname)) {
                return GetPrimitive(pname);
            }
            return Error;
        }
        public static PrimitiveType GetPrimitive(PrimitiveName name) {
            return PrimitiveType.FromName(name);
        }
        public static IType Error => ErrorType.Instance;
        public static IType Top => TopType.Instance;
        public static IType Bot => BottomType.Instance;
        public static IType MostSpecialCommonSuperType(IType t1, IType t2) {
            if (t1.IsTop())
                return t2;
            if (t2.IsTop())
                return t1;
            if (Type.IsAssignable(t1, t2, out int diff, true)) {
                return diff > 0 ? t2 : t1;
            }
            if (t1.IsByRef() || t1.IsVarArg())
                t1 = (t1 as IWrapperType).ItemType;
            if (t2.IsByRef() || t2.IsVarArg())
                t2 = (t2 as IWrapperType).ItemType;

            if (t1 is IHierarchialType h1 && t2 is IHierarchialType h2) {
                Stack<IType> parT1 = new Stack<IType>(), parT2 = new Stack<IType>();

                while (!(h1 is null)) {
                    h1 = h1.SuperType;
                    parT1.Push(h1);
                }
                while (!(h2 is null)) {
                    h2 = h2.SuperType;
                    parT2.Push(h2);
                }
                if (parT1.Any() && parT2.Any()) {
                    t1 = parT1.Pop();
                    t2 = parT2.Pop();
                    var tret = Type.Error;
                    while (Util.CompareSelect(t1, t2, ref tret) && parT1.TryPop(out t1) && parT2.TryPop(out t2)) {

                    }
                    return tret;
                }
            }
            else if (t1 is PrimitiveType prim1 && t2 is PrimitiveType prim2 && prim1.IsNumeric && prim2.IsNumeric) {
                for (var tp = PrimitiveName.Byte; tp < PrimitiveName.String; tp++) {
                    var superTy = PrimitiveType.FromName(tp);
                    if (Type.IsAssignable(t1, superTy) && Type.IsAssignable(t2, superTy)) {
                        return superTy;
                    }
                }
            }
            return Type.Error;
        }
        public static IType MostSpecialCommonSuperType(IEnumerable<IType> tps) {
            if (tps is null || !tps.Any()) {
                return Type.Error;
            }
            return tps.Aggregate(Type.Top, (x, y) => MostSpecialCommonSuperType(x, y));
        }
        public static IType MostSpecialCommonSuperType(params IType[] tps) {
            if (tps is null || tps.Length == 0) {
                return Type.Error;
            }
            IType retTy = tps[0];
            for (int i = 1; i < tps.Length && !retTy.IsError(); ++i) {//DOLATER Maybe divide and conquer (due to Type.Error propagation)
                retTy = MostSpecialCommonSuperType(retTy, tps[i]);
            }
            return retTy;
        }
        public static bool IsAssignable(IType objTy, IType varTy, out int diff, bool allowNegativeDiff = false) {
            diff = 0;
            if (objTy is null)
                return false;
            if (varTy is null || varTy.IsTop() || varTy.IsError() || objTy.IsBot())
                return true;

            if (objTy.IsPrimitive(PrimitiveName.Null))
                return !varTy.IsNotNullable();
            if (varTy.IsValueType() != objTy.IsValueType() && !varTy.IsPrimitive() && !objTy.IsPrimitive())
                return varTy.IsValueType() && varTy.IsByRef() && IsAssignable(objTy, varTy.UnWrap().UnWrap());
            if (varTy.IsUnique() && !objTy.IsUnique())
                return false;

            var uwVarTy = varTy.IsByRef() ? varTy.UnWrap() : varTy;
            var uwObjTy = objTy.IsByRef() ? objTy.UnWrap() : objTy;
            var uwaVarTy = uwVarTy.UnWrapAll();
            var uwaObjTy = uwObjTy.UnWrapAll();

            if (uwVarTy.IsValueType() && !uwVarTy.IsPrimitive()) {// no polymorphism fo r value-types, except for string/bool conversion
                if (uwObjTy == uwVarTy)
                    return true;
            }
            else if (uwaObjTy.IsSubTypeOf(uwaVarTy, out diff) && (diff >= 0 || allowNegativeDiff)) {
                if (!objTy.IsSubTypeOf(varTy))
                    diff++;
                return true;
            }

            if (uwVarTy.IsString()) {
                if (uwaObjTy.OverloadsOperator(OverloadableOperator.String, out _)) {
                    diff = 1;
                    return true;
                }
            }
            else if (uwVarTy.IsPrimitive(PrimitiveName.Bool)) {
                if (uwaObjTy.OverloadsOperator(OverloadableOperator.Bool, out _)) {
                    diff = 1;
                    return true;
                }
            }

            return false;
        }
        public static bool IsAssignable(IType objTy, IType varTy) => IsAssignable(objTy, varTy, out _);
    }
}
