/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CompilerInfrastructure.Utils.CoreExtensions;

namespace CompilerInfrastructure {
    /// <summary>
    /// Union of <see cref="IMethod"/>, <see cref="IMethodTemplate{T}"/> and <see cref="Structure.Macros.MacroFunction"/>
    /// </summary>
    public interface IDeclaredMethodBase : IPositional, IVisible {
        IContext NestedIn {
            get;
        }
        InstructionBox Body {
            get;
        }

    }
    /// <summary>
    /// Union of <see cref="IMethod"/> and <see cref="IMethodTemplate{T}"/>
    /// </summary>
    public interface IDeclaredMethod : IDeclaredMethodBase, ISigned {
        Method.Specifier Specifiers {
            get;
        }
        IVariable[] Arguments {
            get;
        }
        IType ReturnType {
            get;
        }
        SimpleMethodContext Context {
            get;
        }

        IDeclaredMethod OverrideTarget {
            get;
        }
    }
    /// <summary>
    /// The interface, which all method-classes should implement
    /// </summary>
    public interface IMethod : IDeclaredMethod, ISourceElement, IRangeScope<SimpleMethodContext>, IReplaceableStructureElement<IMethod>, ISigned<Method.Signature> {
        bool IsVariadic();
    }
    /// <summary>
    /// Auxiliary class that provides some utility functions as extension-methods to <see cref="IMethod"/>
    /// </summary>
    public static class MethodHelper {
        public static bool IsVirtual(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Virtual);
        }
        public static bool IsOverride(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Overrides);
        }
        public static bool IsAsync(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Async);
        }
        public static bool IsOperatorOverload(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.OperatorOverload);
        }
        public static bool IsReadonly(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Virtual);
        }
        public static bool IsAbstract(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Abstract);
        }
        public static bool IsError(this IMethod met) {
            return met == ErrorMethod.Instance;
        }
        public static bool IsNullOrError(this IMethod met) {
            return met is null || met == ErrorMethod.Instance;
        }

        public static bool IsStatic(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Static);
        }
        public static bool HasUniqueThis(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.UniqueThis);
        }
        public static bool IsInternal(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Internal);
        }
        public static bool IsExternal(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.External);
        }
        public static bool IsConstructor(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Constructor);
        }
        public static bool IsDestructor(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Destructor);
        }
        public static bool IsFinal(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Final);
        }
        public static bool IsCoroutine(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Coroutine);
        }
        public static bool IsImport(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Import);
        }

        public static bool IsBuiltin(this IMethod met) {
            return met != null && met.Specifiers.HasFlag(Method.Specifier.Builtin);
        }

        public static string FullName(this IMethod met) {
            if (met is null)
                return null;
            if (met.IsError())
                return "<error-method>";
            switch (met.NestedIn) {
                case ITypeContext tctx when tctx.Type != null:
                    return tctx.Type.FullName() + "." + met.Signature.QualifiedName();
                case ITypeTemplateContext ttctx when ttctx.TypeTemplate != null:
                    return ttctx.TypeTemplate.BuildType().FullName() + "." + met.Signature.QualifiedName();
                default:
                    return met.Signature.QualifiedName();
            }
        }
    }
    /// <summary>
    ///  Auxiliary class that provides some types and static methods, which are needed for using <see cref="IMethod"/>
    /// </summary>
    public static class Method {
        /// <summary>
        /// A method-signature
        /// </summary>
        [Serializable]
        public struct Signature : ISignature, IEquatable<Signature> {
            /// <summary>
            /// Initializes this method-signature with the given information
            /// </summary>
            /// <param name="name">The method-name</param>
            /// <param name="retType">The return-type</param>
            /// <param name="args">The formal parameter types</param>
            /// <param name="genericArgs">The generic actual-parameters (if there are none, then just pass null)</param>
            /// <param name="internalName">The 'mangled' name of the method in the case, that it is an imported C-function</param>
            public Signature(string name, IType retType, IReadOnlyList<IType> args, IReadOnlyList<ITypeOrLiteral> genericArgs, string internalName = null) {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                ReturnTypeSignature = retType ?? PrimitiveType.Void;
                ArgTypeSignatures = EquatableCollection.FromIList(args);
                GenericActualArguments = EquatableCollection.FromIList(genericArgs);
                BaseMethodTemplate = null;
                _internalName = internalName == name ? null : internalName;
            }
            /// <summary>
            /// Initializes this method-signature with the given information
            /// </summary>
            /// <param name="name">The method-name</param>
            /// <param name="retType">The return-type</param>
            /// <param name="args">The formal parameter types</param>
            public Signature(string name, IType retType, params IType[] args)
                : this(name, retType, args, null) {

            }
            string _internalName ;
            /// <summary>
            /// The 'mangled' name of the method in the case, that it is an imported C-function, else <see cref="Name"/>
            /// </summary>
            public string InternalName {
                get => _internalName ?? Name;
            }
            public bool TryGetInternalName(out string intnl) {
                if (_internalName is null) {
                    intnl = null;
                    return false;
                }
                intnl = _internalName;
                return true;
            }
            /// <summary>
            /// The method's name
            /// </summary>
            public string Name {
                get;
            }
            /// <summary>
            /// The method-template, where this method is based on in the case, that this method is a specialization of a method-template
            /// </summary>
            public IMethodTemplate<IMethod> BaseMethodTemplate {
                get; set;
            }
            /// <summary>
            /// The return-type
            /// </summary>
            /// <remarks>TODO: rename this to 'ReturnType'</remarks>
            public IType ReturnTypeSignature {
                get;
            }
            /// <summary>
            /// The formal parameter types
            /// </summary>
            /// <remarks>TODO: rename this to 'ArgTypes'</remarks>
            public EquatableCollection<IType> ArgTypeSignatures {
                get;
            }
            /// <summary>
            /// The generic actual parameters, in the case that this method is a specialization of a method-template
            /// </summary>
            public EquatableCollection<ITypeOrLiteral> GenericActualArguments {
                get; set;
            }

            public override bool Equals(object obj) => obj is Signature && Equals((Signature) obj);
            public bool Equals(Signature other) {
                return Name == other.Name
                    && EqualityComparer<IType>.Default.Equals(ReturnTypeSignature, other.ReturnTypeSignature)
                    && EqualityComparer<EquatableCollection<IType>>.Default.Equals(ArgTypeSignatures, other.ArgTypeSignatures)
                    && EqualityComparer<EquatableCollection<ITypeOrLiteral>>.Default.Equals(GenericActualArguments, other.GenericActualArguments);
            }

            public override int GetHashCode() => HashCode.Combine(Name, ReturnTypeSignature, ArgTypeSignatures, GenericActualArguments);

            public static bool operator ==(Signature signature1, Signature signature2) => signature1.Equals(signature2);
            public static bool operator !=(Signature signature1, Signature signature2) => !(signature1 == signature2);
            /// <summary>
            /// Builds a qualified name which includes the <see cref="GenericActualArguments"/> when there are some
            /// </summary>
            public string QualifiedName() {

                if (GenericActualArguments.Any()) {
                    var sb = new StringBuilder();
                    sb.Append(Name);
                    sb.Append('<');
                    sb.AppendJoin(", ", GenericActualArguments.Select(x => {
                        if (x is IType tp)
                            return tp.Signature.ToString();
                        else if (x is ILiteral lit)
                            return lit.ToString();
                        else
                            return "<ERROR>";
                    }));
                    sb.Append('>');
                    return sb.ToString();
                }
                else
                    return Name;
            }
            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append(ReturnTypeSignature);
                sb.Append(" ");
                //sb.Append(Name);
                sb.Append(QualifiedName());
                /*if (GenericActualArguments.Any()) {
                    sb.Append('<');
                    sb.AppendJoin(", ", GenericActualArguments.Select(x => {
                        if (x is IType tp)
                            return tp.Signature.ToString();
                        else if (x is ILiteral lit)
                            return lit.ToString();
                        else
                            return "<ERROR>";
                    }));
                    sb.Append('>');
                }*/
                sb.Append('(');
                sb.AppendJoin(", ", ArgTypeSignatures);
                sb.Append(')');
                return sb.ToString();
            }

        }
        /// <summary>
        /// A bitset, which contains additional specifiers/attributes of a method
        /// </summary>
        [Flags]
        [Serializable]
        public enum Specifier : ulong {
            /// <summary>
            /// Dummy
            /// </summary>
            FIRST = None,
            /// <summary>
            /// No additional information
            /// </summary>
            None = 0x00,
            /// <summary>
            /// This method may be overridden
            /// </summary>
            Virtual = 0x01,
            /// <summary>
            /// This method overrides another method
            /// </summary>
            Overrides = 0x02,
            /// <summary>
            /// This method runs asynchronously
            /// </summary>
            Async = 0x04,
            /// <summary>
            /// This method overloads an operator
            /// </summary>
            OperatorOverload = 0x08,
            /// <summary>
            /// This method is readonly (similar to 'const' method in C/C++)
            /// </summary>
            Readonly = 0x10,
            /// <summary>
            /// This method provides no implementation and must be overridden
            /// </summary>
            Abstract = 0x20 | Virtual,
            /// <summary>
            /// This method is a coroutine
            /// </summary>
            Coroutine = 0x40,
            /// <summary>
            /// This method must not be overridden (used for forbidding overriding of overriding methods)
            /// </summary>
            Final = 0x80,
            /// <summary>
            /// This method is static (needs no receiver-object)
            /// </summary>
            Static = 0x100,
            /// <summary>
            /// This instance-method does not create an alias of the this-pointer which lives longer, than the method-body
            /// </summary>
            UniqueThis = 0x200,
            /// <summary>
            /// The implementation of this method is provided in the language runtime-library
            /// </summary>
            Internal = 0x400,
            /// <summary>
            /// The implementation of this method is provided in some external C-library
            /// </summary>
            External = 0x800,
            /// <summary>
            /// This method is a constructor
            /// </summary>
            Constructor = 0x1000,
            /// <summary>
            /// This method is a destructor
            /// </summary>
            Destructor = 0x2000,
            /// <summary>
            /// This method is defined is an imported module
            /// </summary>
            Import = 0x4000,
            /// <summary>
            /// This method has no side-effects, which are visible from other places than the method-body
            /// </summary>
            SideEffectFree = 0x8000 | Readonly,
            /// <summary>
            /// This method has no side-effects and reads no memory, which is not allocated in the method-body
            /// </summary>
            Pure = SideEffectFree | 0x10000,
            /// <summary>
            /// This method either has not implementation at all (all calls will be replaced by specialized LLVM-IR) or 
            /// the compiler will generate the implementation of this method on its own
            /// </summary>
            Builtin = 0x20000,
            /// <summary>
            /// Dummy, which may be used for extending the specifiers in other modules
            /// </summary>
            LAST = Builtin,
            /// <summary>
            /// All specifiers
            /// </summary>
            ALL = ~0UL,
        }
        /// <summary>
        /// The error-method
        /// </summary>
        public static IMethod Error => ErrorMethod.Instance;
    }
    /// <summary>
    /// An abstract class providing some default-implementation for <see cref="IMethod"/>
    /// </summary>
    [Serializable]
    public abstract class MethodImpl : IMethod {
        public abstract Method.Signature Signature {
            get;
        }
        IContext IRangeScope.Context {
            get => Context;
        }
        public abstract SimpleMethodContext Context {
            get;
        }
        public abstract InstructionBox Body {
            get; set;
        }
        public abstract Method.Specifier Specifiers {
            get; protected set;
        }
        public abstract Visibility Visibility {
            get;
        }
        public abstract Position Position {
            get;

        }
        public virtual IType ReturnType {
            get;
            set;
        } = PrimitiveType.Void;
        public virtual IVariable[] Arguments {
            get;
            set;
        } = Array.Empty<IVariable>();
        public virtual IContext NestedIn {
            get => Context.Module;
        }
        public IDeclaredMethod OverrideTarget {
            get; set;
        }
        ISignature ISigned.Signature => Signature;

        protected IEnumerable<IASTNode> Children() {
            if (Context is SimpleMethodContext ctx) {
                foreach (var arg in ctx.Arguments) {
                    yield return arg;
                }
            }
            if (Body != null)
                yield return Body.Instruction;
        }
        /*public virtual IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.ConcatMany(Children());
        }
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public abstract IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public bool IsVariadic() => Arguments.Any() && Arguments.Last().Type.IsVarArg();
    }
}
