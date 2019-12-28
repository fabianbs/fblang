/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    using Type = Structure.Types.Type;

    public interface ILiteral : IExpression, ITypeOrLiteral, SwitchStatement.IPattern {
        bool IsPositiveValue {
            get;
        }
        bool ValueEquals(ILiteral other);
        IType MinimalType {
            get;
        }
    }
    public interface IALiteral : ILiteral {
        bool IsTrue { get; }
        IALiteral ValueCast(IType ty);
    }
    [Serializable]
    public abstract class ALiteral<T> : IALiteral, ICompileTimeEvaluable, IEquatable<ALiteral<T>> {
        public ALiteral(Position pos, T val) {
            Position = pos;
            Value = val;
        }
        public T Value {
            get;
        }
        public Position Position {
            get;
        }
        public abstract IType ReturnType {
            get;
        }
        public bool IsLValue(IMethod met) => false;
        public virtual IType MinimalType {
            get => ReturnType;
        }

        /*public virtual IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public IExpression Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return (ILiteral) Replace(genericActualParameter, curr, parent);
        }
        public override string ToString() => Value.ToString();
        public bool ValueEquals(ILiteral other) {
            return other is ALiteral<T> al && Equals(Value, al.Value);
        }

        public IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        public bool TryReplaceMacroParameters(MacroCallParameters args, out IExpression[] expr) {
            expr = new[] { this };
            return false;
        }

        public bool TryReplaceMacroParameters(MacroCallParameters args, out SwitchStatement.IPattern ret) {
            ret = this;
            return false;
        }
        SwitchStatement.IPattern SwitchStatement.IPattern.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public IEnumerable<IStatement> GetStatements() => throw new NotImplementedException();
        public ILiteral Evaluate(ref EvaluationContext context) => context.AssertFact(this, this);
        public abstract IALiteral ValueCast(IType ty);
        public override bool Equals(object obj) => Equals(obj as ALiteral<T>);
        public bool Equals(ALiteral<T> other) => other != null && EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override int GetHashCode() => -1937169414 + EqualityComparer<T>.Default.GetHashCode(Value);

        public abstract bool IsPositiveValue {
            get;
        }
        /// <summary>
        /// Never ask for NotNullable-DataflowFacts on literals
        /// </summary>
        public ISet<IVariable> NotNullableVars {
            get; set;
        }
        public IType BaseType => MinimalType;

        public bool TypecheckNecessary => false;

        public bool IsCompileTimeEvaluable => true;

        public virtual bool IsTrue => !Equals(Value, default);
    }
    public static class LiteralHelper {
        public static bool TryConvertToUInt(this ILiteral lit, out uint ret) {
            if (lit.ReturnType.IsNumericType() && lit.IsPositiveValue) {
                if (lit is ByteLiteral bt)
                    ret = bt.Value;
                else if (lit is CharLiteral cl)
                    ret = cl.Value;
                else if (lit is ShortLiteral sl)
                    ret = (uint) sl.Value;
                else if (lit is UShortLiteral us)
                    ret = us.Value;
                else if (lit is IntLiteral il)
                    ret = (uint) il.Value;
                else if (lit is UIntLiteral ui)
                    ret = ui.Value;
                else {
                    ret = 0;
                    return false;
                }
                return true;
            }
            ret = 0;
            return false;
        }
        public static bool IsTrue(this ILiteral lit) {
            if (lit is IALiteral alit) {
                return alit.IsTrue;
            }
            return false;
        }
        public static ILiteral ValueCast(this ILiteral lit, IType ty) {
            if (ty == lit.ReturnType)
                return lit;
            else if (lit is IALiteral alit)
                return alit.ValueCast(ty);
            else
                return null;
        }
        public static bool TryConvertToLong(this ILiteral lit, out long val) {
            switch (lit) {
                case BoolLiteral bl:
                    val = bl.Value ? 1 : 0;
                    return true;
                case ByteLiteral bl:
                    val = bl.Value;
                    return true;
                case CharLiteral cl:
                    val = cl.Value;
                    return true;
                case ShortLiteral sl:
                    val = sl.Value;
                    return true;
                case UShortLiteral usl:
                    val = usl.Value;
                    return true;
                case IntLiteral il:
                    val = il.Value;
                    return true;
                case UIntLiteral ul:
                    val = ul.Value;
                    return true;
                case LongLiteral ll:
                    val = ll.Value;
                    return true;
                default:
                    val = 0;
                    return false;
            }
        }
        public static bool TryConvertToULong(this ILiteral lit, out ulong val) {
            switch (lit) {
                case BoolLiteral bl:
                    val = bl.Value ? 1u : 0u;
                    return true;
                case UShortLiteral sl:
                    val = sl.Value;
                    return true;
                case UIntLiteral il:
                    val = il.Value;
                    return true;
                case ULongLiteral ll:
                    val = ll.Value;
                    return true;
                default:
                    val = 0;
                    return false;
            }
        }
        public static bool TryConvertToBigLong(this ILiteral lit, out BigInteger val) {
            switch (lit) {
                case BoolLiteral bl:
                    val = bl.Value ? 1 : 0;
                    return true;
                case ByteLiteral bl:
                    val = bl.Value;
                    return true;
                case CharLiteral cl:
                    val = cl.Value;
                    return true;
                case ShortLiteral sl:
                    val = sl.Value;
                    return true;
                case UShortLiteral usl:
                    val = usl.Value;
                    return true;
                case IntLiteral il:
                    val = il.Value;
                    return true;
                case UIntLiteral ul:
                    val = ul.Value;
                    return true;
                case LongLiteral ll:
                    val = ll.Value;
                    return true;
                case ULongLiteral ul:
                    val = ul.Value;
                    return true;
                case BigLongLiteral bll:
                    val = bll.Value;
                    return true;
                default:
                    val = 0;
                    return false;
            }
        }
        public static bool TryConvertToDouble(this ILiteral lit, out double val) {
            switch (lit) {
                case BoolLiteral bl:
                    val = bl.Value ? 1 : 0;
                    return true;
                case ByteLiteral bl:
                    val = bl.Value;
                    return true;
                case CharLiteral cl:
                    val = cl.Value;
                    return true;
                case ShortLiteral sl:
                    val = sl.Value;
                    return true;
                case UShortLiteral usl:
                    val = usl.Value;
                    return true;
                case IntLiteral il:
                    val = il.Value;
                    return true;
                case UIntLiteral ul:
                    val = ul.Value;
                    return true;
                case LongLiteral ll:
                    val = ll.Value;
                    return true;
                case ULongLiteral ul:
                    val = ul.Value;
                    return true;
                case BigLongLiteral bll:
                    val = (double) bll.Value;
                    return true;
                case FloatLiteral fl:
                    val = fl.Value;
                    return true;
                case DoubleLiteral dl:
                    val = dl.Value;
                    return true;
                default:
                    val = 0;
                    return false;
            }
        }
        public static ushort BitWidth(this ILiteral lit) {
            return (lit.ReturnType as PrimitiveType).DefaultBitWidth;
        }
    }
    public static class Literal {
        public static NullLiteral Null => NullLiteral.Instance;
        public static BoolLiteral True {
            get;
        } = new BoolLiteral(default, true);
        public static BoolLiteral False {
            get;
        } = new BoolLiteral(default, false);
        public static BoolLiteral Bool(bool b) => b ? True : False;
        public static StringLiteral SolveStringEscapes(Position pos, string text, int offset, int length) {
            return SolveStringEscapes(pos, text.AsSpan(offset, length));
        }
        public static StringLiteral SolveStringEscapes(Position pos, ReadOnlySpan<char> lit) {
            var sb = new StringBuilder(lit.Length);
            bool escape = false;
            for (int i = 0; i < lit.Length; ++i) {
                if (!escape) {
                    if (lit[i] == '\\')
                        escape = true;
                    else
                        sb.Append(lit[i]);
                }
                else {
                    escape = false;
                    switch (lit[i]) {
                        case '\\':
                            sb.Append('\\');
                            break;
                        case '"':
                            sb.Append('"');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'a':
                            sb.Append('\a');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        case 'u': {

                            if (i > lit.Length - 5) {
                                $"Ungültige Escapesequenz (Unicode muss genau 4 Hexadezimalziffern enthalten): \\{lit.Slice(i).ToString()}".Report(pos);
                            }//TODO avoid copying
                            else if (!int.TryParse(lit.Slice(i + 1, 4).ToString(), out int uniVal)) {
                                $"Ungültige Escapesequenz (Unicode darf nur aus Hexadezimalziffern bestehen): \\{lit.Slice(i).ToString()}".Report(pos);
                            }
                            else {
                                sb.Append(char.ConvertFromUtf32(uniVal));
                            }
                            break;
                        }
                        default:
                            sb.Append(lit[i]);
                            break;
                    }
                }
            }
            if (escape)
                "Ungültige Escapesequenz: Es muss mindestens ein Zeichen mit '\\' behandelt werden".Report(pos);
            return new StringLiteral(pos, sb.ToString());
        }
        public static CharLiteral SolveCharEscapes(Position pos, string text, int offset, int length) {
            return SolveCharEscapes(pos, text.AsSpan(offset, length));
        }
        public static CharLiteral SolveCharEscapes(Position pos, ReadOnlySpan<char> lit) {
            if (lit.Length == 1) {
                if (lit[0] == '\\') {
                    "Ungültige Escapesequenz: Es muss mindestens ein Zeichen mit '\\' behandelt werden".Report(pos);
                }
                return new CharLiteral(pos, lit[0]);
            }
            else if (lit.Length == 2 && lit[0] == '\\') {
                char ret;
                switch (lit[1]) {
                    case '\\':
                        ret = '\\';
                        break;
                    case '"':
                        ret = ('"');
                        break;
                    case 'n':
                        ret = ('\n');
                        break;
                    case 'r':
                        ret = ('\r');
                        break;
                    case 'a':
                        ret = ('\a');
                        break;
                    case 'b':
                        ret = ('\b');
                        break;
                    case 'f':
                        ret = ('\f');
                        break;
                    case 't':
                        ret = ('\t');
                        break;
                    case 'v':
                        ret = ('\v');
                        break;
                    case '0':
                        ret = '\0';
                        break;
                    case 'u':
                        ret = 'u';
                        $"Ungültige Escapesequenz (Unicode muss genau 4 Hexadezimalziffern enthalten): {lit.ToString()}".Report(pos);
                        break;
                    default:
                        ret = lit[0];
                        break;
                }
                return new CharLiteral(pos, ret);
            }
            else if (lit.Length == 6 && lit[0] == '\\' && lit[1] == 'u') {


                //TOOD avoid copying
                if (!int.TryParse(lit.Slice(2, 4).ToString(), out int uniVal)) {
                    $"Ungültige Escapesequenz (Unicode darf nur aus Hexadezimalziffern bestehen): {lit.ToString()}".Report(pos);
                    uniVal = 'u';
                }
                return new CharLiteral(pos, (char) uniVal);
            }
            else {
                $"Ungültiges Char-Literal: '{lit.ToString()}'".Report(pos);
                return new CharLiteral(pos, '\0');
            }
        }

        static readonly LazyDictionary<uint, UIntLiteral> uints
            = new LazyDictionary<uint, UIntLiteral>(x => new UIntLiteral(default, x));
        internal static UIntLiteral GetOrSetUInt(UIntLiteral ui) {
            if (uints.TryGetValue(ui.Value, out var ret))
                return ret;
            uints[ui.Value] = ui;
            return ui;
        }
        public static UIntLiteral UInt(uint n) {
            return uints[n];
        }
        static readonly LazyDictionary<int, IntLiteral> ints
            = new LazyDictionary<int, IntLiteral>(x => new IntLiteral(default, x));
        static StringLiteral emptyString = null;
        public static StringLiteral EmptyString {
            get {
                if (emptyString is null)
                    emptyString = new StringLiteral(default, "");
                return emptyString;
            }
        }
        public static IntLiteral Int(int n) {
            return ints[n];
        }

        public static bool TryGetZero(PrimitiveType ty, out ILiteral ret) {
            switch (ty.PrimitiveName) {
                case PrimitiveName.Handle:
                case PrimitiveName.Null:
                    ret = Null;
                    break;
                case PrimitiveName.Bool:
                    ret = False;
                    break;
                case PrimitiveName.Byte:
                    ret = new ByteLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.Char:
                    ret = new CharLiteral(("", 0, 0), '\0');
                    break;
                case PrimitiveName.Short:
                    ret = new ShortLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.UShort:
                    ret = new UShortLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.Int:
                    ret = Int(0);
                    break;
                case PrimitiveName.UInt:
                    ret = UInt(0);
                    break;
                case PrimitiveName.Long:
                    ret = new LongLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.ULong:
                    ret = new ULongLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.BigLong:
                    ret = new BigLongLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.UBigLong:
                    ret = new BigLongLiteral(("", 0, 0), 0, true);
                    break;
                case PrimitiveName.Float:
                    ret = new FloatLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.Double:
                    ret = new DoubleLiteral(("", 0, 0), 0);
                    break;
                case PrimitiveName.String:
                    ret = EmptyString;
                    break;
                default:
                    ret = default;
                    return false;
            }
            return true;
        }
        public static bool TryGetOne(PrimitiveType ty, out ILiteral ret) {
            switch (ty.PrimitiveName) {
                case PrimitiveName.Bool:
                    ret = True;
                    break;
                case PrimitiveName.Byte:
                    ret = new ByteLiteral(default, 1);
                    break;
                case PrimitiveName.Char:
                    ret = new CharLiteral(default, (char) 1);
                    break;
                case PrimitiveName.Short:
                    ret = new ShortLiteral(default, 1);
                    break;
                case PrimitiveName.UShort:
                    ret = new UShortLiteral(default, 1);
                    break;
                case PrimitiveName.Int:
                    ret = Int(1);
                    break;
                case PrimitiveName.UInt:
                    ret = UInt(1);
                    break;
                case PrimitiveName.Long:
                    ret = new LongLiteral(default, 1);
                    break;
                case PrimitiveName.ULong:
                    ret = new ULongLiteral(default, 1);
                    break;
                case PrimitiveName.BigLong:
                    ret = new BigLongLiteral(default, 1);
                    break;
                case PrimitiveName.UBigLong:
                    ret = new BigLongLiteral(default, 1, true);
                    break;
                case PrimitiveName.Float:
                    ret = new FloatLiteral(default, 1);
                    break;
                case PrimitiveName.Double:
                    ret = new DoubleLiteral(default, 1);
                    break;
                default:
                    ret = default;
                    return false;
            }
            return true;
        }
        public static ILiteral WithCommonType(IALiteral lit1, IALiteral lit2, Func<ILiteral, ILiteral, ILiteral> fn) {
            if (lit1 is null || lit2 is null || fn is null)
                return null;
            var joinTy = Type.MostSpecialCommonSuperType(lit1.ReturnType, lit2.ReturnType);
            lit1 = lit1.ValueCast(joinTy);
            lit2 = lit2.ValueCast(joinTy);
            if (lit1 is null || lit2 is null)
                return null;
            return fn(lit1, lit2);
        }
        public static ILiteral WithCommonType(IALiteral lit1, IALiteral lit2, Func<long, long, long> sfn, Func<ulong, ulong, ulong> ufn, Func<BigInteger, BigInteger, BigInteger> bfn, Func<double, double, double> dfn) {
            return WithCommonType(lit1, lit2, (x, y) => {
                if (lit1.TryConvertToLong(out var l1) && lit2.TryConvertToLong(out var l2)) {
                    if (sfn is null)
                        return null;
                    return new LongLiteral(x.Position, sfn(l1, l2) & ((1L << x.BitWidth()) - 1)).ValueCast(x.ReturnType);
                }
                else if (lit1.TryConvertToULong(out var u1) && lit2.TryConvertToULong(out var u2)) {
                    if (ufn is null)
                        return null;
                    return new ULongLiteral(x.Position, ufn(u1, u2) & ((1uL << x.BitWidth()) - 1)).ValueCast(x.ReturnType);
                }
                else if (lit1.TryConvertToBigLong(out var b1) && lit2.TryConvertToBigLong(out var b2)) {
                    if (bfn is null)
                        return null;
                    return new BigLongLiteral(x.Position, bfn(b1, b2) & ((BigInteger.One << x.BitWidth()) - 1)).ValueCast(x.ReturnType);
                }
                else if (lit1.TryConvertToDouble(out var d1) && lit2.TryConvertToDouble(out var d2)) {
                    if (bfn is null)
                        return null;
                    return new DoubleLiteral(x.Position, dfn(d1, d2)).ValueCast(x.ReturnType);
                }
                else
                    return null;
            });
        }
        public static ILiteral WithCommonType(IALiteral lit1, IALiteral lit2, Func<long, long, bool> sfn, Func<ulong, ulong, bool> ufn, Func<BigInteger, BigInteger, bool> bfn, Func<double, double, bool> dfn) {
            return WithCommonType(lit1, lit2, (x, y) => {
                if (lit1.TryConvertToLong(out var l1) && lit2.TryConvertToLong(out var l2)) {
                    if (sfn is null)
                        return null;
                    return Bool(sfn(l1, l2));
                }
                else if (lit1.TryConvertToULong(out var u1) && lit2.TryConvertToULong(out var u2)) {
                    if (ufn is null)
                        return null;
                    return Bool(ufn(u1, u2));
                }
                else if (lit1.TryConvertToBigLong(out var b1) && lit2.TryConvertToBigLong(out var b2)) {
                    if (bfn is null)
                        return null;
                    return Bool(bfn(b1, b2));
                }
                else if (lit1.TryConvertToDouble(out var d1) && lit2.TryConvertToDouble(out var d2)) {
                    if (dfn is null)
                        return null;
                    return Bool(dfn(d1, d2));
                }
                else
                    return null;
            });
        }
        public static ILiteral WithIntegerType(ILiteral lit, Func<long, ILiteral> sfn, Func<ulong, ILiteral> ufn, Func<BigInteger, ILiteral> bfn) {
            if (lit.TryConvertToLong(out var sl)) {
                if (sfn is null)
                    return null;
                return sfn(sl).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToULong(out var ul)) {
                if (ufn is null)
                    return null;
                return ufn(ul).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToBigLong(out var bl)) {
                if (bfn is null)
                    return null;
                return bfn(bl).ValueCast(lit.ReturnType);
            }
            else
                return null;
        }
        public static ILiteral WithRealType(ILiteral lit, Func<long, ILiteral> sfn, Func<ulong, ILiteral> ufn, Func<BigInteger, ILiteral> bfn, Func<double, ILiteral> dfn) {
            if (lit.TryConvertToLong(out var sl)) {
                if (sfn is null)
                    return null;
                return sfn(sl).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToULong(out var ul)) {
                if (ufn is null)
                    return null;
                return ufn(ul).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToBigLong(out var bl)) {
                if (bfn is null)
                    return null;
                return bfn(bl).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToDouble(out var dl)) {
                if (dfn is null)
                    return null;
                return dfn(dl).ValueCast(lit.ReturnType);
            }
            else
                return null;
        }
        public static ILiteral WithRealType(ILiteral lit, Func<long, long> sfn, Func<ulong, ulong> ufn, Func<BigInteger, BigInteger> bfn, Func<double, double> dfn) {
            if (lit.TryConvertToLong(out var sl)) {
                if (sfn is null)
                    return null;
                return new LongLiteral(lit.Position, sfn(sl)).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToULong(out var ul)) {
                if (ufn is null)
                    return null;
                return new ULongLiteral(lit.Position, ufn(ul)).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToBigLong(out var bl)) {
                if (bfn is null)
                    return null;
                return new BigLongLiteral(lit.Position, bfn(bl)).ValueCast(lit.ReturnType);
            }
            else if (lit.TryConvertToDouble(out var dl)) {
                if (dfn is null)
                    return null;
                return new DoubleLiteral(lit.Position, dfn(dl)).ValueCast(lit.ReturnType);
            }
            else
                return null;
        }
    }
    [Serializable]
    public class ByteLiteral : ALiteral<byte> {
        public ByteLiteral(Position pos, byte val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Byte;
        }
        public override bool IsPositiveValue {
            get => true;
        }

        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => this,
                PrimitiveName.Char => new CharLiteral(Position, (char) Value),
                PrimitiveName.Short => new ShortLiteral(Position, Value),
                PrimitiveName.UShort => new UShortLiteral(Position, Value),
                PrimitiveName.Int => new IntLiteral(Position, Value),
                PrimitiveName.UInt => new UIntLiteral(Position, Value),
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value),
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => new ULongLiteral(Position, Value),
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, "0x" + Value.ToString("XX")),
                _ => null,
            };
        }
    }
    [Serializable]
    public class BoolLiteral : ALiteral<bool> {
        public BoolLiteral(Position pos, bool val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Bool;
        }
        public override bool IsPositiveValue {
            get => true;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => this,
                PrimitiveName.Byte => new ByteLiteral(Position, Value ? (byte) 1 : (byte) 0),
                PrimitiveName.Char => new CharLiteral(Position, Value ? (char) 1 : '\0'),
                PrimitiveName.Short => new ShortLiteral(Position, Value ? (short) 1 : (short) 0),
                PrimitiveName.UShort => new UShortLiteral(Position, Value ? (ushort) 1 : (ushort) 0),
                PrimitiveName.Int => new IntLiteral(Position, Value ? 1 : 0),
                PrimitiveName.UInt => new UIntLiteral(Position, Value ? 1u : 0u),
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value ? 1u : 0u),
                PrimitiveName.Long => new LongLiteral(Position, Value ? 1 : 0),
                PrimitiveName.ULong => new ULongLiteral(Position, Value ? 1u : 0u),
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value ? 1 : 0),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value ? 1 : 0, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value ? 1 : 0),
                PrimitiveName.Double => new DoubleLiteral(Position, Value ? 1 : 0),
                PrimitiveName.String => new StringLiteral(Position, Value ? "true" : "false"),
                _ => null,
            };
        }
    }
    [Serializable]
    public class CharLiteral : ALiteral<char> {
        public CharLiteral(Position pos, char val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Char;
        }
        public override string ToString() => "'" + base.ToString() + "'";
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => this,
                PrimitiveName.Short => Value <= short.MaxValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => new UShortLiteral(Position, Value),
                PrimitiveName.Int => new IntLiteral(Position, Value),
                PrimitiveName.UInt => new UIntLiteral(Position, Value),
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value),
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => new ULongLiteral(Position, Value),
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class ShortLiteral : ALiteral<short> {
        public ShortLiteral(Position pos, short val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Short;
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => this,
                PrimitiveName.UShort => Value >= 0 ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => new IntLiteral(Position, Value),
                PrimitiveName.UInt => Value >= 0 ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => Value >= 0 ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => Value >= 0 ? new BigLongLiteral(Position, Value, true) : null,
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class UShortLiteral : ALiteral<ushort> {
        public UShortLiteral(Position pos, ushort val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.UShort;
        }
        public override bool IsPositiveValue {
            get => true;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => new CharLiteral(Position, (char) Value),
                PrimitiveName.Short => Value <= short.MaxValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => this,
                PrimitiveName.Int => new IntLiteral(Position, Value),
                PrimitiveName.UInt => new UIntLiteral(Position, Value),
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value),
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => new ULongLiteral(Position, Value),
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class IntLiteral : ALiteral<int> {
        public IntLiteral(Position pos, int val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Int;
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IType MinimalType {
            get {
                if (Value <= short.MaxValue && Value >= short.MinValue) {
                    if (Value <= sbyte.MaxValue && Value >= sbyte.MinValue)
                        return PrimitiveType.Char;
                    else
                        return PrimitiveType.Short;
                }
                else
                    return PrimitiveType.Int;
            }
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 && Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue && Value >= short.MinValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value >= 0 && Value <= ushort.MaxValue ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => this,
                PrimitiveName.UInt => Value >= 0 ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => Value >= 0 ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => Value >= 0 ? new BigLongLiteral(Position, Value, true) : null,
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class UIntLiteral : ALiteral<uint> {
        public UIntLiteral(Position pos, uint val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.UInt;
        }
        public override bool IsPositiveValue {
            get => true;
        }
        public override IType MinimalType {
            get {
                if (Value <= ushort.MaxValue) {
                    if (Value <= byte.MaxValue)
                        return PrimitiveType.Byte;
                    else
                        return PrimitiveType.UShort;
                }
                else
                    return PrimitiveType.UInt;
            }
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value <= ushort.MaxValue ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue ? new IntLiteral(Position, (int) Value) : null,
                PrimitiveName.UInt => this,
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value),
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => new ULongLiteral(Position, Value),
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class LongLiteral : ALiteral<long> {
        public LongLiteral(Position pos, long val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Long;
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 && Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue && Value >= short.MinValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value >= 0 ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue && Value >= int.MinValue ? Literal.Int((int) Value) : null,
                PrimitiveName.UInt => Value >= 0 && Value <= uint.MaxValue ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => new LongLiteral(Position, Value),
                PrimitiveName.ULong => Value >= 0 ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => Value >= 0 ? new BigLongLiteral(Position, Value, true) : null,
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class ULongLiteral : ALiteral<ulong> {
        public ULongLiteral(Position pos, ulong val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.ULong;
        }
        public override bool IsPositiveValue {
            get => true;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= (uint) short.MaxValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value <= ushort.MaxValue ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue ? new IntLiteral(Position, (int) Value) : null,
                PrimitiveName.UInt => this,
                PrimitiveName.SizeT => new SizeTLiteral(Position, Value),
                PrimitiveName.Long => Value <= long.MaxValue ? new LongLiteral(Position, (long) Value) : null,
                PrimitiveName.ULong => this,
                PrimitiveName.BigLong => new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => new BigLongLiteral(Position, Value, true),
                PrimitiveName.Float => new FloatLiteral(Position, Value),
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class SizeTLiteral : ULongLiteral {
        public SizeTLiteral(Position pos, ulong val) : base(pos, val) {
        }
        public override IType ReturnType => PrimitiveType.SizeT;
        public override IALiteral ValueCast(IType ty) => ty.IsPrimitive(PrimitiveName.SizeT)
            ? this : ty.IsPrimitive(PrimitiveName.ULong)
            ? new ULongLiteral(Position, Value)
            : base.ValueCast(ty);
    }
    [Serializable]
    public class BigLongLiteral : ALiteral<BigInteger> {
        private readonly bool unsigned;

        public BigLongLiteral(Position pos, BigInteger val, bool unsigned = false) : base(pos, val) {
            this.unsigned = unsigned;
            if (unsigned && val.Sign < 0) {
                throw new ArgumentException();
            }
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IType ReturnType {
            get => unsigned ? PrimitiveType.UBigLong : PrimitiveType.BigLong;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 && Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue && Value >= short.MinValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value >= 0 ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue && Value >= int.MinValue ? Literal.Int((int) Value) : null,
                PrimitiveName.UInt => Value >= 0 && Value <= uint.MaxValue ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 && Value <= ulong.MaxValue ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => Value <= long.MaxValue ? new LongLiteral(Position, (long) Value) : null,
                PrimitiveName.ULong => Value >= 0 && Value <= ulong.MaxValue ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => !unsigned ? this : new BigLongLiteral(Position, Value),
                PrimitiveName.UBigLong => unsigned ? this : Value >= 0 ? new BigLongLiteral(Position, Value, true) : null,
                PrimitiveName.Float => new FloatLiteral(Position, (float) Value),
                PrimitiveName.Double => new DoubleLiteral(Position, (double) Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class FloatLiteral : ALiteral<float> {
        public FloatLiteral(Position pos, float val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Float;
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 && Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue && Value >= short.MinValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value >= 0 ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue && Value >= int.MinValue ? Literal.Int((int) Value) : null,
                PrimitiveName.UInt => Value >= 0 && Value <= uint.MaxValue ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 && Value <= ulong.MaxValue ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => Value <= long.MaxValue ? new LongLiteral(Position, (long) Value) : null,
                PrimitiveName.ULong => Value >= 0 && Value <= ulong.MaxValue ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => new BigLongLiteral(Position, (BigInteger) Value),
                PrimitiveName.UBigLong => Value >= 0 ? new BigLongLiteral(Position, (BigInteger) Value, true) : null,
                PrimitiveName.Float => this,
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class DoubleLiteral : ALiteral<double> {
        public DoubleLiteral(Position pos, double val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Double;
        }
        public override bool IsPositiveValue {
            get => Value >= 0;
        }
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            return prim.PrimitiveName switch
            {
                PrimitiveName.Bool => Literal.Bool(IsTrue),
                PrimitiveName.Byte => Value >= 0 && Value < 256 ? new ByteLiteral(Position, (byte) Value) : null,
                PrimitiveName.Char => Value >= 0 && Value <= char.MaxValue ? new CharLiteral(Position, (char) Value) : null,
                PrimitiveName.Short => Value <= short.MaxValue && Value >= short.MinValue ? new ShortLiteral(Position, (short) Value) : null,
                PrimitiveName.UShort => Value >= 0 ? new UShortLiteral(Position, (ushort) Value) : null,
                PrimitiveName.Int => Value <= int.MaxValue && Value >= int.MinValue ? Literal.Int((int) Value) : null,
                PrimitiveName.UInt => Value >= 0 && Value <= uint.MaxValue ? new UIntLiteral(Position, (uint) Value) : null,
                PrimitiveName.SizeT => Value >= 0 && Value <= ulong.MaxValue ? new SizeTLiteral(Position, (ulong) Value) : null,
                PrimitiveName.Long => Value <= long.MaxValue ? new LongLiteral(Position, (long) Value) : null,
                PrimitiveName.ULong => Value >= 0 && Value <= ulong.MaxValue ? new ULongLiteral(Position, (ulong) Value) : null,
                PrimitiveName.BigLong => new BigLongLiteral(Position, (BigInteger) Value),
                PrimitiveName.UBigLong => Value >= 0 ? new BigLongLiteral(Position, (BigInteger) Value, true) : null,
                PrimitiveName.Float => Value <= float.MaxValue && Value >= float.MinValue ? new FloatLiteral(Position, (float) Value) : null,
                PrimitiveName.Double => new DoubleLiteral(Position, Value),
                PrimitiveName.String => new StringLiteral(Position, Value.ToString()),
                _ => null,
            };
        }
    }
    [Serializable]
    public class StringLiteral : ALiteral<string> {
        public StringLiteral(Position pos, string val) : base(pos, val) {
        }

        public override IType ReturnType {
            get => PrimitiveType.String;
        }
        public string EscapeString() {
            var sb = new StringBuilder();

            foreach (var x in Value) {
                switch (x) {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\a':
                        sb.Append("\\a");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\v':
                        sb.Append("\\t");
                        break;
                    default: {
                        if (x >= 32 && x < 127) {
                            sb.Append(x);
                        }
                        else {
                            sb.Append("\\u");
                            sb.Append(((int) x).ToString("X4"));
                        }
                        break;
                    }
                }
            }

            return sb.ToString();
        }
        public override string ToString() => "\"" + EscapeString() + "\"";
        public override bool IsPositiveValue {
            get => true;
        }
        public override bool IsTrue => !string.IsNullOrEmpty(Value);
        public override IALiteral ValueCast(IType ty) {
            if (!(ty is PrimitiveType prim))
                return null;
            switch (prim.PrimitiveName) {
                case PrimitiveName.Bool:
                    return Literal.Bool(IsTrue);
                case PrimitiveName.Byte: {
                    return byte.TryParse(Value, out var res) ? new ByteLiteral(Position, res) : null;
                }
                case PrimitiveName.Char: {
                    return char.TryParse(Value, out var res) ? new CharLiteral(Position, res) : null;
                }
                case PrimitiveName.Short: {
                    return short.TryParse(Value, out var res) ? new ShortLiteral(Position, res) : null;
                }
                case PrimitiveName.UShort: {
                    return ushort.TryParse(Value, out var res) ? new UShortLiteral(Position, res) : null;
                }
                case PrimitiveName.Int: {
                    return int.TryParse(Value, out var res) ? new IntLiteral(Position, res) : null;
                }
                case PrimitiveName.UInt: {
                    return uint.TryParse(Value, out var res) ? new UIntLiteral(Position, res) : null;
                }
                case PrimitiveName.SizeT: {
                    return ulong.TryParse(Value, out var res) ? new SizeTLiteral(Position, res) : null;
                }
                case PrimitiveName.Long: {
                    return long.TryParse(Value, out var res) ? new LongLiteral(Position, res) : null;
                }
                case PrimitiveName.ULong: {
                    return ulong.TryParse(Value, out var res) ? new ULongLiteral(Position, res) : null;
                }
                case PrimitiveName.BigLong: {
                    return BigInteger.TryParse(Value, out var res) ? new BigLongLiteral(Position, res) : null;
                }
                case PrimitiveName.UBigLong: {
                    return BigInteger.TryParse(Value, out var res) && res.Sign >= 0 ? new BigLongLiteral(Position, res, true) : null;
                }
                case PrimitiveName.Float: {
                    return float.TryParse(Value, out var res) ? new FloatLiteral(Position, res) : null;
                }
                case PrimitiveName.Double: {
                    return double.TryParse(Value, out var res) ? new DoubleLiteral(Position, res) : null;
                }
                case PrimitiveName.String:
                    return this;
                default:
                    return null;
            }
        }
    }
    [Serializable]
    public class NullLiteral : ALiteral<DBNull> {
        private NullLiteral(Position pos) : base(pos, null) {
        }

        public override IType ReturnType {
            get => PrimitiveType.Null;
        }
        public static NullLiteral Instance {
            get;
        } = new NullLiteral(default);
        public override bool IsPositiveValue {
            get => true;
        }
        public override bool IsTrue => false;

        public override IALiteral ValueCast(IType ty) => ty.IsPrimitive(PrimitiveName.Bool)
            ? (IALiteral) Literal.False : ty.IsPrimitive(PrimitiveName.Null)
            ? this
            : null;
    }
}
