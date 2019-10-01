/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using static CompilerInfrastructure.Context;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class PrimitiveType : TypeImpl, ISerializable {

        class IntegerContext : SimpleTypeContext {
            //TODO Integer-Context
            public IntegerContext(Module mod, PrimitiveName name) : base(mod, DefiningRules.None) {
            }
        }

        class BooleanContext : SimpleTypeContext {
            //TODO Boolean-Context
            public BooleanContext(Module mod) : base(mod, DefiningRules.None) {
            }
        }
        class HandleContext : SimpleTypeContext {
            //TODO Handle-Context
            public HandleContext(Module mod) : base(mod, DefiningRules.None) {
            }
        }
        class FloatContext : SimpleTypeContext {
            //TODO Float-Context
            public FloatContext(Module mod, PrimitiveName name) : base(mod, DefiningRules.None) {
            }
        }
        class StringContext : SimpleTypeContext {
            //TODO String-Context
            public StringContext(Module mod) : base(mod, DefiningRules.None) {
                var len = new BasicVariable(default, FromNameInternal(PrimitiveName.SizeT), Variable.Specifier.Final, "length", null, Visibility.Public);
                DefineVariable(len);

                var index = new BasicMethod(default, "operator " + OverloadableOperator.Indexer.OperatorName(), Visibility.Public, FromNameInternal(PrimitiveName.Char), new[] {
                    new BasicVariable(default,FromNameInternal(PrimitiveName.SizeT),Variable.Specifier.LocalVariable,"index",null)
                }) {
                    Specifiers = Method.Specifier.Internal | Method.Specifier.OperatorOverload | Method.Specifier.Readonly | Method.Specifier.Pure
                };
                DefineMethod(index);
                /*var concat = new BasicMethod(default, "operator " + OverloadableOperator.Add.OperatorName(), Visibility.Public, FromNameInternal(PrimitiveName.String), new[] {
                    new BasicVariable(default, FromNameInternal(PrimitiveName.String), Variable.Specifier.LocalVariable,"other",null)
                },internalName:"strconcat") {
                    Specifiers = Method.Specifier.Internal | Method.Specifier.OperatorOverload | Method.Specifier.Readonly | Method.Specifier.Pure
                };
                DefineMethod(concat);
                var mulr = new BasicMethod(default, "operator " + OverloadableOperator.Mul.OperatorName(), Visibility.Public, FromNameInternal(PrimitiveName.String), new[] {
                    new BasicVariable(default, FromNameInternal(PrimitiveName.UInt), Variable.Specifier.LocalVariable,"factor",null)
                },internalName:"strmul") {
                    Specifiers = Method.Specifier.Internal | Method.Specifier.OperatorOverload | Method.Specifier.Readonly | Method.Specifier.Pure
                };
                DefineMethod(mulr);
                var mull = new BasicMethod(default, "operator " + OverloadableOperator.Mul.OperatorName(), Visibility.Public, FromNameInternal(PrimitiveName.String), new[] {
                    new BasicVariable(default, FromNameInternal(PrimitiveName.String),Variable.Specifier.LocalVariable,"_this",null),
                    new BasicVariable(default, FromNameInternal(PrimitiveName.UInt), Variable.Specifier.LocalVariable,"factor",null)
                }) {
                    Specifiers = Method.Specifier.Internal | Method.Specifier.OperatorOverload | Method.Specifier.Readonly | Method.Specifier.Pure | Method.Specifier.Static
                };
                DefineMethod(mull);*/
            }

        }
        static readonly LazyDictionary<PrimitiveName, PrimitiveType> prim
            = new LazyDictionary<PrimitiveName, PrimitiveType>(x =>
                new PrimitiveType(x)
            );
        private readonly PrimitiveName name;
        private ITypeContext context;
        static readonly Module mod = new Module();
        private PrimitiveType(PrimitiveName name) {

            Signature = new Type.Signature(name.ToString().ToLower(), null);
            if (name == PrimitiveName.Handle) {
                TypeSpecifiers |= Type.Specifier.NativePointer;
            }
            else if (name != PrimitiveName.Null) {
                TypeSpecifiers |= Type.Specifier.ValueType;
            }
            switch (name) {
                case PrimitiveName.Byte:
                case PrimitiveName.UShort:
                case PrimitiveName.SizeT:
                case PrimitiveName.UInt:
                case PrimitiveName.ULong:
                case PrimitiveName.UBigLong:
                    IsUnsignedInteger = true;
                    break;
                default:
                    IsUnsignedInteger = false;
                    break;
            }
            switch (name) {
                case PrimitiveName.Char:
                case PrimitiveName.Short:
                case PrimitiveName.Handle:
                case PrimitiveName.Int:
                case PrimitiveName.Long:
                case PrimitiveName.BigLong:
                    IsSignedInteger = true;
                    break;
                default:
                    IsSignedInteger = false;
                    break;
            }
            IsFloatingPoint = name == PrimitiveName.Float || name == PrimitiveName.Double;
            this.name = name;
            DefaultBitWidth = name.GetDefaultBitWidth();
        }
        protected PrimitiveType(SerializationInfo info, StreamingContext context) : this(info.GetT<PrimitiveName>(nameof(PrimitiveName))) {

        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(PrimitiveName), PrimitiveName);
        }

        public PrimitiveName PrimitiveName => name;
        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context => context;
        public override Type.Specifier TypeSpecifiers {
            get;
        } = Type.Specifier.Primitive;
        public override Visibility Visibility {
            get => Visibility.Public;
        }
        public override Position Position {
            get => default;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();
        public override bool IsNestedIn(IType other) => false;
        public override bool IsSubTypeOf(IType other) {
            if (other.IsError())
                return true;
            if (other == this)
                return true;
            if (other is PrimitiveType op) {
                switch (op.name) {
                    case PrimitiveName.Void:
                    case PrimitiveName.Null:
                    case PrimitiveName.Handle:
                    case PrimitiveName.Byte:
                    case PrimitiveName.Char:
                        return false;// is not this
                    case PrimitiveName.Bool://all primitives can be converted to bool
                        return true;
                    case PrimitiveName nom when nom < PrimitiveName.Float: {
                        if (((int) nom & 1) == 1) {// odd => signed integer; even => unsigned integer
                            return name < nom;
                        }
                        else {
                            return name < (nom - 1);// ushort is not subtype of short, uint not subtype of int, etc.
                        }
                    }
                    case PrimitiveName nom when nom >= PrimitiveName.Float: {
                        return name < nom;
                    }
                }
            }
            return false;
        }

        public override bool IsSubTypeOf(IType other, out int difference) {
            if (other.IsError()) {
                difference = int.MaxValue;
                return true;
            }
            if (other == this) {
                difference = 0;
                return true;
            }
            if (other is PrimitiveType op) {
                if (op.name < PrimitiveName.Float && op.name >= PrimitiveName.Short
                    && name < PrimitiveName.Float && name >= PrimitiveName.Short
                    && Math.Abs(op.name - name) == 1) {

                    difference = 0;
                    return false;
                }
                difference = op.name - name;
                return true;
            }
            difference = 0;
            return false;
        }

        // primitives
        public static PrimitiveType Void => FromName(PrimitiveName.Void);
        public static PrimitiveType Null => FromName(PrimitiveName.Null);
        public static PrimitiveType Handle = FromName(PrimitiveName.Handle);
        public static PrimitiveType Bool => FromName(PrimitiveName.Bool);
        public static PrimitiveType Byte => FromName(PrimitiveName.Byte);
        public static PrimitiveType Char => FromName(PrimitiveName.Char);
        public static PrimitiveType Short => FromName(PrimitiveName.Short);
        public static PrimitiveType UShort => FromName(PrimitiveName.UShort);
        public static PrimitiveType Int => FromName(PrimitiveName.Int);
        public static PrimitiveType UInt => FromName(PrimitiveName.UInt);
        public static PrimitiveType Long => FromName(PrimitiveName.Long);
        public static PrimitiveType ULong => FromName(PrimitiveName.ULong);
        public static PrimitiveType BigLong => FromName(PrimitiveName.BigLong);
        public static PrimitiveType UBigLong => FromName(PrimitiveName.UBigLong);
        public static PrimitiveType Float => FromName(PrimitiveName.Float);
        public static PrimitiveType Double => FromName(PrimitiveName.Double);
        public static PrimitiveType String => FromName(PrimitiveName.String);
        public static PrimitiveType SizeT => FromName(PrimitiveName.SizeT);

        public ushort DefaultBitWidth {
            get;
        }

        public bool IsUnsignedInteger {
            get;
        }
        public bool IsSignedInteger {
            get;
        }
        public bool IsInteger {
            get => IsUnsignedInteger || IsSignedInteger;
        }
        public bool IsNumeric {
            get => IsInteger || IsFloatingPoint;
        }
        public bool IsFloatingPoint {
            get;
        }
        public override IType NestedIn {
            get => null;
        }
        public override IContext DefinedIn => Context.Module;

        public override string ToString() => name.ToString();
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static PrimitiveType FromNameInternal(PrimitiveName name) {
            return prim[name];
        }
        public static PrimitiveType FromName(PrimitiveName name) {
            var ret = FromNameInternal(name);
            if (ret.context is null) {
                switch (name) {
                    case PrimitiveName.Null:
                    case PrimitiveName.Void:
                        ret.context = new SimpleTypeContext(mod, DefiningRules.None, new BasicContext(mod, DefiningRules.None, true), new BasicContext(mod, DefiningRules.None, true));
                        break;
                    case PrimitiveName.Byte:
                    case PrimitiveName.Char:
                    case PrimitiveName.Short:
                    case PrimitiveName.UShort:
                    case PrimitiveName.Int:
                    case PrimitiveName.SizeT:
                    case PrimitiveName.UInt:
                    case PrimitiveName.Long:
                    case PrimitiveName.ULong:
                    case PrimitiveName.BigLong:
                    case PrimitiveName.UBigLong:
                        ret.context = new IntegerContext(mod, name);
                        break;
                    case PrimitiveName.Float:
                    case PrimitiveName.Double:
                        ret.context = new FloatContext(mod, name);
                        break;
                    case PrimitiveName.String:
                        ret.context = new StringContext(mod);
                        break;
                }
            }
            return ret;
        }

        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public override void PrintPrefix(TextWriter tw) {
        }
        public override void PrintValue(TextWriter tw) {
            tw.Write(name.ToString().ToLower());
        }
        public override void PrintSuffix(TextWriter tw) {
        }
    }
    [Serializable]
    public enum PrimitiveName {
        Void,
        Null,
        Bool,
        Byte,
        Char,
        Short,
        UShort,
        Int,
        UInt,
        Handle,
        SizeT,
        Long,
        ULong,
        BigLong,
        UBigLong,
        Float,
        Double,
        String
    }
    public static class PrimitiveNameHelper {
        public static ushort GetDefaultBitWidth(this PrimitiveName name) {
            switch (name) {
                case PrimitiveName.Void:
                    return 0;
                case PrimitiveName.Null:
                case PrimitiveName.Handle:
                case PrimitiveName.SizeT:
                    return (ushort) (IntPtr.Size * 8);
                case PrimitiveName.Bool:
                case PrimitiveName.Byte:
                case PrimitiveName.Char:
                    return 8;
                case PrimitiveName.Short:
                case PrimitiveName.UShort:
                    return 16;
                case PrimitiveName.Int:
                case PrimitiveName.UInt:
                case PrimitiveName.Float:
                    return 32;
                case PrimitiveName.Long:
                case PrimitiveName.ULong:
                case PrimitiveName.Double:
                    return 64;
                case PrimitiveName.BigLong:
                case PrimitiveName.UBigLong:
                    return 128;

                case PrimitiveName.String:
                    return (ushort) (IntPtr.Size * 8 * 2);
            }
            throw new ArgumentException();
        }
    }
}
