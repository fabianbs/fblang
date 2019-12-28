/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using static CompilerInfrastructure.Utils.CoreExtensions;
using System.Linq;

namespace CompilerInfrastructure {
    using Structure.Types;

    public interface IVariable : IVisible, ISourceElement, IReplaceableStructureElement<IVariable>, ISigned<Variable.Signature> {

        Variable.Specifier VariableSpecifiers {
            get;
        }
        IExpression DefaultValue {
            get;
        }
        IType Type {
            get;
        }
        IType DefinedInType {
            get;
        }
        bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr);
    }
    public static class VariableHelper {
        public static bool IsError(this IVariable vr) {
            return vr == Variable.Error;
        }
        public static bool IsStatic(this IVariable vr) {
            return vr != null && vr.VariableSpecifiers.HasFlag(Variable.Specifier.Static);
        }
        public static bool IsLocallyAllocated(this IVariable vr) {
            return vr != null && vr.Type.IsValueType();
        }
        public static bool IsVolatile(this IVariable vr) {
            return vr != null && vr.VariableSpecifiers.HasFlag(Variable.Specifier.Volatile);
        }
        public static bool IsLocalVariable(this IVariable vr) {
            return vr != null && vr.VariableSpecifiers.HasFlag(Variable.Specifier.LocalVariable);
        }
        public static bool IsNotNull(this IVariable vr) {
            return vr != null && vr.Type.IsNotNullable();
        }
        public static bool IsFinal(this IVariable vr) {
            return vr != null && vr.VariableSpecifiers.HasFlag(Variable.Specifier.Final);
        }
        public static bool IsNoCapture(this IVariable vr) {
            return vr != null && (vr.VariableSpecifiers.HasFlag(Variable.Specifier.NoCapture) || vr.Type.IsUnique() && vr.IsFinal());
        }

    }
    [Serializable]
    internal class ErrorVariable : IVariable {
        private ErrorVariable() {
        }
        public Variable.Signature Signature {
            get;
        } = new Variable.Signature("", Structure.Types.Type.Error.Signature);
        ISignature ISigned.Signature => Signature;
        public Variable.Specifier VariableSpecifiers {
            get;
        } = (Variable.Specifier) ~0;
        public IExpression DefaultValue {
            get;
        } = Expression.Error;
        public Visibility Visibility {
            get;
        } = Visibility.Public;
        public Position Position {
            get;
        } = default;

        /*public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public IVariable Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr) { vr = this; return false; }

        public static ErrorVariable Instance {
            get;
        } = new ErrorVariable();
        public IType Type {
            get => Structure.Types.Type.Error;
        }
        public IType DefinedInType {
            get => Structure.Types.Type.Error;
        }
    }
    [Serializable]
    public abstract class VariableImpl : IVariable {
        internal Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IVariable> genericCache
            = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IVariable>();
        readonly IExpression[] dflt = new IExpression[1];
        public VariableImpl(Position pos, IType definedIn) {
            Position = pos;
            DefinedInType = definedIn;
        }
        public VariableImpl(Position pos, Visibility vis, Type.Signature type, Variable.Specifier specs, string name, IType definedIn)
            : this(pos, definedIn) {
            Signature = new Variable.Signature(name, type ?? "A variable must have a type".Report(pos, Structure.Types.Type.Error.Signature));
            VariableSpecifiers = specs;
            Visibility = vis;
        }
        public virtual Variable.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public virtual Variable.Specifier VariableSpecifiers {
            get;
        }
        public virtual Visibility Visibility {
            get;
        }
        public virtual Position Position {
            get;
        }
        public virtual IExpression DefaultValue {
            get => dflt[0];
            set => dflt[0] = value;
        }
        public virtual IType Type {
            get; set;
        }
        public IType DefinedInType {
            get;
        }

        public virtual IRefEnumerator<IASTNode> GetEnumerator() {
            if (DefaultValue is null)
                return RefEnumerator.Empty<IASTNode>();
            else
                return RefEnumerator.FromArray<IASTNode>(dflt);
        }
        protected abstract IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual IVariable Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                ret = ReplaceImpl(genericActualParameter, curr, parent);

                var otherType = ret.Type;
                genericCache.TryAdd(genericActualParameter, ret);
                if (ret is VariableImpl vi) {
                    if (Type is IGenericParameter && otherType is IGenericParameter)
                        vi.genericCache = genericCache;
                    else
                        vi.genericCache.TryAdd(genericActualParameter, ret);
                }
                else if (ret is MutatingVariableImpl mvi) {
                    if (Type is IGenericParameter && otherType is IGenericParameter)
                        mvi.genericCache = genericCache;
                    else
                        mvi.genericCache.TryAdd(genericActualParameter, ret);
                }
            }
            return ret;
        }

        public abstract bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr);


        /* IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
    }
    [Serializable]
    public abstract class MutatingVariableImpl : IVariable {
        internal Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IVariable> genericCache
             = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IVariable>();
        readonly IExpression[] dflt = new IExpression[1];
        public MutatingVariableImpl(Position pos, IType definedIn) {
            Position = pos;
            DefinedInType = definedIn;
        }
        public MutatingVariableImpl(Position pos, Visibility vis, Type.Signature type, Variable.Specifier specs, string name, IType definedIn)
            : this(pos, definedIn) {
            Signature = new Variable.Signature(name, type ?? "A variable must have a type".Report(pos, Structure.Types.Type.Error.Signature));
            VariableSpecifiers = specs;
            Visibility = vis;
        }
        public virtual Variable.Signature Signature {
            get; set;
        }
        ISignature ISigned.Signature => Signature;
        public virtual Variable.Specifier VariableSpecifiers {
            get; set;
        }
        public virtual Visibility Visibility {
            get; set;
        }
        public virtual Position Position {
            get;
        }
        public virtual IExpression DefaultValue {
            get => dflt[0];
            set => dflt[0] = value;
        }
        public virtual IType Type {
            get; set;
        }
        public IType DefinedInType {
            get;
        }

        public virtual IRefEnumerator<IASTNode> GetEnumerator() {
            if (DefaultValue is null)
                return RefEnumerator.Empty<IASTNode>();
            else
                return RefEnumerator.FromArray<IASTNode>(dflt);
        }

        protected abstract IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual IVariable Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                ret = ReplaceImpl(genericActualParameter, curr, parent);

                var otherType = ret.Type;
                genericCache.TryAdd(genericActualParameter, ret);
                if (ret is VariableImpl vi) {
                    if (Type is IGenericParameter && otherType is IGenericParameter)
                        vi.genericCache = genericCache;
                    else
                        vi.genericCache.TryAdd(genericActualParameter, ret);
                }
                else if (ret is MutatingVariableImpl mvi) {
                    if (Type is IGenericParameter && otherType is IGenericParameter)
                        mvi.genericCache = genericCache;
                    else
                        mvi.genericCache.TryAdd(genericActualParameter, ret);
                }
            }
            return ret;
        }

        public abstract bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr);
        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
    }
    public static class Variable {
        [Serializable]
        public struct Signature : ISignature, IEquatable<Signature> {

            public Signature(string name, Type.Signature varType) {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                TypeSignature = varType ?? throw new ArgumentNullException(nameof(varType));
            }

            public string Name {
                get;
            }
            public Type.Signature TypeSignature {
                get;
            }

            public override bool Equals(object obj) => obj is Signature && Equals((Signature) obj);
            public bool Equals(Signature other) {
                return Name == other.Name && EqualityComparer<Type.Signature>.Default.Equals(TypeSignature, other.TypeSignature);
            }

            public override int GetHashCode() => HashCode.Combine(Name, TypeSignature);

            public static bool operator ==(Signature signature1, Signature signature2) => signature1.Equals(signature2);
            public static bool operator !=(Signature signature1, Signature signature2) => !(signature1 == signature2);

            public override string ToString() {
                return $"{TypeSignature} {Name}";
            }
        }
        [Flags]
        [Serializable]
        public enum Specifier {
            None = 0x00,
            Final = 0x01,
            Volatile = 0x04,
            LocalVariable = 0x08,
            Static = 0x10,
            NoCapture = 0x20,
        }
        public static IVariable Error => ErrorVariable.Instance;
        static readonly LazyDictionary<IType, IVariable> thisCache = new LazyDictionary<IType, IVariable>(ty=>{
            return new BasicVariable(default, ty, Specifier.Final, "this", ty);
        });
        public static IVariable This(IType ty) {
            return thisCache[ty];
        }
    }
}
