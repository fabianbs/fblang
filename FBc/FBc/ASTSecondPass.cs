/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    /// <summary>
    /// recognize superclass and implementing interfaces(not check implementation)
    /// </summary>
    /// <seealso cref="FBc.FBlangBaseVisitor{object}" />
    class ASTSecondPass {
        protected readonly Stack<IContext> contextStack = new Stack<IContext>();

        public FBModule Module {
            get;
        }

        public ASTSecondPass(ASTFirstPass firstPassResult) {
            if (firstPassResult == null)
                throw new ArgumentNullException(nameof(firstPassResult));
            contextStack.Push(Module = firstPassResult.Module);
            AllTypes = firstPassResult.AllTypes;
            AllTypeTemplates = firstPassResult.AllTypeTemplates;
        }

        protected internal ASTSecondPass(FBModule mod, IEnumerable<(IType, FBlangParser.TypeDefContext)> allTypes, IEnumerable<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> allTypeTemplates) {
            Module = mod;
            contextStack.Push(mod);
            AllTypes = allTypes;
            AllTypeTemplates = allTypeTemplates;
        }
        public IEnumerable<(IType, FBlangParser.TypeDefContext)> AllTypes {
            get;
        }
        public IEnumerable<(ITypeTemplate<IType>, FBlangParser.TypeDefContext)> AllTypeTemplates {
            get;
        }
        public virtual void VisitAllTypes() {
            #region generic formal parameter
            foreach (var ttp in AllTypeTemplates) {
                contextStack.Push(ttp.Item1.Context);
                var typeCtx = ttp.Item2;
                if (typeCtx.classDef() != null) {
                    VisitGenericDef(typeCtx.classDef().genericDef(), ttp.Item1.Signature.GenericFormalparameter);
                }
                else if (typeCtx.voidDef() != null) {
                    VisitGenericDef(typeCtx.voidDef().genericDef(), ttp.Item1.Signature.GenericFormalparameter);
                }
                else if (typeCtx.intfDef() != null) {
                    VisitGenericDef(typeCtx.intfDef().genericDef(), ttp.Item1.Signature.GenericFormalparameter);
                }
                contextStack.Pop();
            }
            #endregion
            #region superclasses and implementing interfaces

            //set superclass and implementing interfaces
            foreach (var tp in AllTypes) {
                contextStack.Push(tp.Item1.Context);

                VisitTypeDef(tp.Item2, tp.Item1);

                contextStack.Pop();
            }
            foreach (var tp in AllTypeTemplates) {
                contextStack.Push(tp.Item1.Context);

                VisitTypeTemplateDef(tp.Item2, tp.Item1);

                contextStack.Pop();
            }
            #endregion

        }
        protected virtual bool VisitTypeDef([NotNull] FBlangParser.TypeDefContext context, IType tp) {
            if (context.classDef() != null)
                return VisitClassDef(context.classDef(), tp);
            if (context.voidDef() != null)
                return VisitVoidDef(context.voidDef(), tp);
            if (context.intfDef() != null)
                return VisitIntfDef(context.intfDef(), tp);

            // extension in 3rd pass

            // if (context.extensionDef() != null)
            //    return VisitExtensionDef(context.extensionDef(), tp);


            //enum already complete
            return true;
        }

        protected virtual bool VisitClassDef([NotNull] FBlangParser.ClassDefContext context, IType tp) {
            bool ret = true;
            if (context.typeQualifier() != null) {
                //resolve superclass
                var sig = VisitTypeQualifier(context.typeQualifier(), tp.Position.FileName);

                if (!contextStack.Peek().Types.TryGetValue(sig, out IType superTp)) {
                    if (sig.GenericActualArguments.Any()) {
                        var templates = contextStack.Peek().TypeTemplatesByName(sig.Name)
                                        .Where(x => x.Signature.GenericFormalparameter.Count == sig.GenericActualArguments.Count);
                        ITypeTemplate<IType> ttp = Module.Semantics.BestFittingTypeTemplate(context.typeQualifier().Position(tp.Position.FileName), templates, sig.GenericActualArguments);
                        if (ttp != null) {
                            superTp = ttp.BuildType(sig.GenericActualArguments);
                            ret = !ttp.Signature.GenericFormalparameter.OfType<GenericTypeParameter>().Any(x => x.SuperType != null || x.ImplementingInterfaces.Any());
                        }
                        else
                            superTp = Type.Error;
                    }
                    else {
                        $"The Superclass {sig} cannot be resolved".Report(context.typeQualifier().Position(tp.Position.FileName));
                    }
                }
                if (superTp != null && !superTp.IsError() && !(superTp is ClassType)) {
                    $"The type {superTp} cannot be a superclass".Report(context.typeQualifier().Position(tp.Position.FileName));
                }
                //assert tp is ClassType
                if (superTp != null && !superTp.IsError()) {
                    (tp as ClassType).Superclass = superTp as ClassType;//TODO Nested classes => supertp has higher priority, but parenttp is private visible
                    (tp as ClassType).Context = SimpleTypeContext.Merge(tp.Context.Module, new[] { tp.Context, Context.Constrained(superTp.Context, Visibility.Protected) }, Context.DefiningRules.All);
                }

                if (superTp.IsActor())// subclass is only then actor, if superclass is actor too
                    (tp as ClassType).TypeSpecifiers |= Type.Specifier.Actor;
                else
                    (tp as ClassType).TypeSpecifiers &= ~Type.Specifier.Actor;
            }
            ret &= ResolveImplementingInterfaces(context.intfList(), tp);
            return ret;
        }
        protected virtual bool ResolveImplementingInterfaces(FBlangParser.IntfListContext intfList, IType tp) {
            bool ret = true;
            if (intfList != null) {
                foreach (var intfSig in intfList.typeQualifier()) {
                    var sig = VisitTypeQualifier(intfSig, tp.Position.FileName);
                    if (!contextStack.Peek().Types.TryGetValue(sig, out IType intfTp)) {
                        if (sig.GenericActualArguments.Any()) {
                            var templates = contextStack.Peek().TypeTemplatesByName(sig.Name)
                                            .Where(x => x.Signature.GenericFormalparameter.Count == sig.GenericActualArguments.Count);
                            ITypeTemplate<IType> ttp = Module.Semantics.BestFittingTypeTemplate(intfSig.Position(tp.Position.FileName), templates, sig.GenericActualArguments);
                            if (ttp != null) {
                                intfTp = ttp.BuildType(sig.GenericActualArguments);
                                ret = !ttp.Signature.GenericFormalparameter.OfType<GenericTypeParameter>().Any(x => x.SuperType != null || x.ImplementingInterfaces.Any());
                            }
                            else
                                intfTp = Type.Error;
                        }
                        else {
                            $"The interface {sig} cannot be resolved".Report(intfSig.Position(tp.Position.FileName));

                        }
                    }
                    if (!(tp as ClassType).AddInterface(intfTp))
                        $"The type {intfTp.Signature} is no interface".Report(intfSig.Position(tp.Position.FileName));
                }
            }

            return ret;
        }
        protected virtual bool ResolveImplementingInterfaces(FBlangParser.IntfListContext intfList, ITypeTemplate<IType> ttp) {
            bool ret = true;
            if (intfList != null) {

                foreach (var intfSig in intfList.typeQualifier()) {
                    var sig = VisitTypeQualifier(intfSig, ttp.Position.FileName);
                    if (!contextStack.Peek().Types.TryGetValue(sig, out IType intfTp)) {
                        if (sig.GenericActualArguments.Any()) {
                            var templates = contextStack.Peek().TypeTemplatesByName(sig.Name)
                                            .Where(x => x.Signature.GenericFormalparameter.Count == sig.GenericActualArguments.Count);
                            ITypeTemplate<IType> _ttp = Module.Semantics.BestFittingTypeTemplate(intfSig.Position(ttp.Position.FileName), templates, sig.GenericActualArguments);
                            if (_ttp != null) {
                                intfTp = _ttp.BuildType(sig.GenericActualArguments);
                                ret = !_ttp.Signature.GenericFormalparameter.OfType<GenericTypeParameter>().Any(x => x.SuperType != null || x.ImplementingInterfaces.Any());
                            }
                            else
                                intfTp = Type.Error;
                        }
                        else {
                            $"The interface {sig} cannot be resolved".Report(intfSig.Position(ttp.Position.FileName));

                        }
                    }
                    if (!(ttp as ClassTypeTemplate).AddInterface(intfTp))
                        $"The type {intfTp.Signature} is no interface".Report(intfSig.Position(ttp.Position.FileName));
                }
            }

            return ret;
        }
        protected virtual bool VisitVoidDef([NotNull] FBlangParser.VoidDefContext context, IType tp) {
            return ResolveImplementingInterfaces(context.intfList(), tp);
        }
        protected virtual bool VisitIntfDef([NotNull] FBlangParser.IntfDefContext context, IType tp) {
            return ResolveImplementingInterfaces(context.intfList(), tp);
        }
        protected virtual bool VisitTypeTemplateDef([NotNull] FBlangParser.TypeDefContext context, ITypeTemplate<IType> tp) {

            contextStack.Push(tp.HeaderContext);
            if (context.classDef() != null)
                return VisitClassTemplateDef(context.classDef(), tp);
            if (context.voidDef() != null)
                return VisitVoidTemplateDef(context.voidDef(), tp);
            if (context.intfDef() != null)
                return VisitIntfTemplateDef(context.intfDef(), tp);
            contextStack.Pop();
            return true;
        }

        protected virtual bool VisitClassTemplateDef([NotNull]FBlangParser.ClassDefContext context, ITypeTemplate<IType> tp) {
            bool ret = true;
            if (context.typeQualifier() != null) {
                //resolve superclass
                var sig = VisitTypeQualifier(context.typeQualifier(), tp.Position.FileName);

                if (!contextStack.Peek().Types.TryGetValue(sig, out IType superTp)) {
                    if (sig.GenericActualArguments.Any()) {
                        var templates = contextStack.Peek().TypeTemplatesByName(sig.Name)
                                        .Where(x => x.Signature.GenericFormalparameter.Count == sig.GenericActualArguments.Count);
                        ITypeTemplate<IType> ttp = Module.Semantics.BestFittingTypeTemplate(context.typeQualifier().Position(tp.Position.FileName), templates, sig.GenericActualArguments);
                        if (ttp != null) {
                            superTp = ttp.BuildType(sig.GenericActualArguments);
                            ret = !ttp.Signature.GenericFormalparameter.OfType<GenericTypeParameter>().Any(x => x.SuperType != null || x.ImplementingInterfaces.Any());
                        }
                        else
                            superTp = Type.Error;
                    }
                    else {
                        $"The Superclass {sig} cannot be resolved".Report(context.typeQualifier().Position(tp.Position.FileName));
                    }
                }
                if (superTp != null && !superTp.IsError() && !(superTp is ClassType)) {
                    $"The type {superTp} cannot be a superclass".Report(context.typeQualifier().Position(tp.Position.FileName));
                }
                //assert tp is ClassType
                if (superTp != null && !superTp.IsError()) {
                    (tp as ClassTypeTemplate).SuperType = superTp as ClassType;
                    //TODO Nested classes => supertp has higher priority, but parenttp is private visible
                    (tp as ClassTypeTemplate).Context = SimpleTypeTemplateContext.Merge(tp.Context.Module, new IStructTypeContext[] { tp.Context, Context.Constrained(superTp.Context, Visibility.Protected) }, Context.DefiningRules.All);
                }
                if (superTp.IsActor())// subclass is only then actor, if superclass is actor too
                    (tp as ClassTypeTemplate).TypeSpecifiers |= Type.Specifier.Actor;
                else
                    (tp as ClassTypeTemplate).TypeSpecifiers &= ~Type.Specifier.Actor;
            }
            ret &= ResolveImplementingInterfaces(context.intfList(), tp);
            return ret;
        }
        protected virtual bool VisitVoidTemplateDef([NotNull]FBlangParser.VoidDefContext context, ITypeTemplate<IType> tp) {
            return ResolveImplementingInterfaces(context.intfList(), tp);
        }
        protected virtual bool VisitIntfTemplateDef([NotNull]FBlangParser.IntfDefContext context, ITypeTemplate<IType> tp) {
            return ResolveImplementingInterfaces(context.intfList(), tp);
        }
        protected virtual void VisitGenericDef([NotNull] FBlangParser.GenericDefContext context, IReadOnlyList<IGenericParameter> genArgs) {
            int i = 0;
            foreach (var x in context.genericFormalArglist().genericFormalArgument()) {
                VisitGenericFormalArgument(x, genArgs[i++]);
            }
        }
        protected virtual void VisitGenericFormalArgument([NotNull] FBlangParser.GenericFormalArgumentContext context, IGenericParameter genArg) {

            if (context.genericTypeParameter() != null) {
                VisitGenericTypeParameter(context.genericTypeParameter(), (GenericTypeParameter) genArg);
            }
            // generic literalparameter already finished
        }
        protected virtual GenericTypeParameter VisitGenericTypeParameter([NotNull] FBlangParser.GenericTypeParameterContext context, GenericTypeParameter genArg) {
            if (context.typeQualifier() != null) {
                var supertype = TypeFromSig(VisitTypeQualifier(context.typeQualifier(), genArg.Position.FileName), context.typeQualifier().Position(genArg.Position.FileName));
                if (supertype is IHierarchialType htp) {
                    genArg.SuperType = htp;
                }
                else
                    $"The type {supertype.Signature} cannot be a generic constraint".Report(context.typeQualifier().Position(genArg.Position.FileName));
            }
            if (context.intfList() != null) {
                foreach (var intf in context.intfList().typeQualifier()) {
                    var intfTp = TypeFromSig(VisitTypeQualifier(intf, genArg.Position.FileName), intf.Position(genArg.Position.FileName));
                    if (!genArg.AddInterface(intfTp))
                        $"The type {intfTp} is no interface and therefor cannot be specified as interface-constraint for a generic typeparameter".Report(intf.Position(genArg.Position.FileName));
                }
            }
            return genArg;
        }

        protected virtual IType RefConstrained(IType tp, string refCap) {
            switch (refCap) {
                case "immutable":
                    if (!tp.IsImmutable())
                        return tp.AsImmutable();
                    break;
                case "unique":
                    if (!tp.IsUnique())
                        return tp.AsUnique();
                    break;
                case "const":
                    if (!tp.IsConstant())
                        return tp.AsConstant();
                    break;
            }
            return tp;
        }
        protected virtual BigInteger BIFromBinary(System.ReadOnlySpan<char> binaryNumber) {
            BigInteger ret = BigInteger.Zero;
            while (binaryNumber.Length > 64) {
                ret <<= 64;
                ret += Convert.ToUInt64(binaryNumber.Slice(binaryNumber.Length - 64, 64).ToString(), 2);
                binaryNumber = binaryNumber.Slice(0, binaryNumber.Length - 64);
            }
            ret <<= binaryNumber.Length;

            if (binaryNumber.Length <= 32) {
                if (binaryNumber.Length <= 16) {
                    if (binaryNumber.Length <= 8) {
                        return ret + Convert.ToByte(binaryNumber.ToString(), 2);
                    }
                    else {
                        return ret + Convert.ToUInt16(binaryNumber.ToString(), 2);
                    }
                }
                else {
                    return ret + Convert.ToUInt32(binaryNumber.ToString(), 2);
                }
            }
            else {
                return ret + Convert.ToUInt64(binaryNumber.ToString(), 2);
            }

        }
        protected virtual ILiteral IntLiteral(Position pos, System.ReadOnlySpan<char> lit, bool negative, ErrorBuffer err = null, IType expectedReturnType = null) {
            System.ReadOnlySpan<char> litValue = lit;
            int @base = 10;
            #region prefixes
            if (lit.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase)) {
                @base = 16;
                litValue = litValue.Slice(2);
            }
            else if (lit.StartsWith("0b", StringComparison.CurrentCultureIgnoreCase)) {
                @base = 2;
                litValue = litValue.Slice(2);
            }
            #endregion

            #region explicit type
            if (litValue.EndsWith("L", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = $"{(negative ? "-" : "")}{litValue.Slice(0, litValue.Length - 1).ToString()}";
                try {
                    var res = Convert.ToInt64(litStr, @base);
                    return new LongLiteral(pos, res);
                }
                catch {
                    return err.Report($"The long-literal '{litStr}' is too {(negative ? "small" : "large")} for a long-value", pos, new LongLiteral(pos, -1));
                }
            }
            else if (litValue.EndsWith("S", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = $"{(negative ? "-" : "")}{litValue.Slice(0, litValue.Length - 1).ToString()}";
                try {
                    var res = Convert.ToInt16(litStr, @base);
                    return new ShortLiteral(pos, res);
                }
                catch {
                    return err.Report($"The short-literal '{litStr}' is too {(negative ? "small" : "large")} for a short-value", pos, new ShortLiteral(pos, -1));
                }
            }
            else if (litValue.EndsWith("B", StringComparison.CurrentCultureIgnoreCase)) {

                var litStr = $"{litValue.Slice(0, litValue.Length - 1).ToString()}";
                try {
                    var res = Convert.ToByte(litStr, @base);
                    if (negative) {
                        res = (byte) (256 - res);
                    }
                    return new ByteLiteral(pos, res);
                }
                catch {
                    return err.Report($"The byte-literal '{litStr}' is too {(negative ? "small" : "large")} for a byte-value", pos, new ByteLiteral(pos, 255));
                }
            }
            else if (litValue.EndsWith("U", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = $"{litValue.Slice(0, litValue.Length - 1).ToString()}";
                try {
                    var res = Convert.ToUInt32(litStr, @base);
                    if (negative) {
                        res = (uint) ((long) uint.MaxValue + 1 - res);
                    }
                    return new UIntLiteral(pos, res);
                }
                catch {
                    return err.Report($"The uint-literal '{litStr}' is too large for a uint-value", pos, new UIntLiteral(pos, uint.MaxValue));
                }
            }
            else if (litValue.EndsWith("UL", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = $"{litValue.Slice(0, litValue.Length - 2).ToString()}";
                try {
                    var res = Convert.ToUInt64(litStr, @base);
                    if (negative) {
                        res = ulong.MaxValue - (res - 1);
                    }
                    return new ULongLiteral(pos, res);
                }
                catch {
                    return err.Report($"The ulong-literal '{litStr}' is too large for a ulong-value", pos, new ULongLiteral(pos, ulong.MaxValue));
                }
            }
            else if (litValue.EndsWith("US", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = $"{litValue.Slice(0, litValue.Length - 2).ToString()}";
                try {
                    var res = Convert.ToUInt16(litStr, @base);
                    if (negative) {
                        res = (ushort) (ushort.MaxValue + 1 - res);
                    }
                    return new UShortLiteral(pos, res);
                }
                catch {
                    return err.Report($"The ushort-literal '{litStr}' is too large for a ushort-value", pos, new UShortLiteral(pos, ushort.MaxValue));
                }
            }
            else if (litValue.EndsWith("Z", StringComparison.CurrentCultureIgnoreCase)) {
                var litStr = litValue.Slice(0, litValue.Length - 1).ToString();
                try {
                    ulong res;
                    if (Environment.Is64BitOperatingSystem) {
                        res = Convert.ToUInt64(litStr, @base);
                        if (negative)
                            res = ulong.MaxValue - (res - 1);
                    }
                    else {
                        res = Convert.ToUInt32(litStr, @base);
                        if (negative)
                            res = uint.MaxValue - (res - 1);
                    }
                    return new SizeTLiteral(pos, res);
                }
                catch {
                    return err.Report($"The zint-literal '{litStr}' is too large for a {(Environment.Is64BitOperatingSystem ? "64" : "32")}Bit integer value", pos, new SizeTLiteral(pos, ulong.MaxValue));
                }
            }
            #endregion
            #region inferred type
            BigInteger value;
            if (!litValue.StartsWith("0")) {
                litValue = $"0{litValue.ToString()}";
            }
            if (@base == 10)
                value = BigInteger.Parse(litValue);
            else if (@base == 16)
                value = BigInteger.Parse(litValue, NumberStyles.HexNumber);
            else {
                value = BIFromBinary(litValue);
            }

            if (negative) {
                value = -value;
                if (value < -BigInteger.Pow(2, 127)) {
                    err.Report($"Die Ganzzahl {value} ist zu klein für ein biglong-Integerliteral", pos);
                    return new BigLongLiteral(pos, value);
                }
                else if (value < long.MinValue) {
                    return new BigLongLiteral(pos, value);
                }
                else {
                    long val = (long) value;
                    if (val < int.MinValue)
                        return new LongLiteral(pos, val);
                    else {
                        if (val < short.MinValue || expectedReturnType != PrimitiveType.Short)
                            return new IntLiteral(pos, (int) val);
                        else
                            return new ShortLiteral(pos, (short) val);
                    }
                }
            }
            else {
                if (value >= BigInteger.Pow(2, 128)) {
                    err.Report($"Die Ganzzahl {value} ist zu groß für ein ubiglong-Integerliteral", pos);
                    return new BigLongLiteral(pos, value, true);
                }
                else if (value >= BigInteger.Pow(2, 127)) {
                    return new BigLongLiteral(pos, value, true);
                }
                else if (value >= ulong.MaxValue) {
                    return new BigLongLiteral(pos, value, false);
                }
                else {
                    var val = (ulong) value;
                    if (val > long.MaxValue)
                        return new ULongLiteral(pos, val);
                    else if ((ulong) val > uint.MaxValue)
                        return new LongLiteral(pos, (long) val);
                    else if ((uint) val > int.MaxValue)
                        return new UIntLiteral(pos, (uint) val);
                    else {
                        PrimitiveName primName;
                        if (expectedReturnType is PrimitiveType prim) {
                            primName = prim.PrimitiveName;
                        }
                        else {
                            primName = PrimitiveName.Int;
                        }
                        if ((int) val > ushort.MaxValue || primName == PrimitiveName.Int)
                            return new IntLiteral(pos, (int) val);
                        else if ((ushort) val > short.MaxValue || primName == PrimitiveName.UShort)
                            return new UShortLiteral(pos, (ushort) val);
                        else if ((short) val > byte.MaxValue || primName == PrimitiveName.Short)
                            return new ShortLiteral(pos, (short) val);
                        else
                            return new ByteLiteral(pos, (byte) val);
                    }
                }
            }
            #endregion
        }
        protected virtual ILiteral FloatLiteral(Position pos, System.ReadOnlySpan<char> lit, bool negative, ErrorBuffer err = null, IType expectedReturnType = null) {
            static double ParseBaseDouble(ReadOnlySpan<char> hd, int bas) {
                double ret = 0;
                double shift = ulong.MaxValue + 1.0;
                while (hd.Length >= 16) {
                    var part = Convert.ToUInt64(hd.Slice(hd.Length - 16, bas).ToString(), 16);
                    ret = ret * shift + part;
                    hd = hd.Slice(0, hd.Length - 16);
                }
                return ret * (16 << hd.Length) + Convert.ToUInt64(hd.ToString(), bas);
            }

            static double ParseBaseExponent(ReadOnlySpan<char> x, int bas) {
                if (x.StartsWith("-") || x.StartsWith("+")) {
                    var ret = ParseBaseDouble(x.Slice(1), bas);
                    if (x[0] == '-')
                        ret = -ret;
                    return ret;
                }
                return ParseBaseDouble(x, bas);
            }
            bool forceFloat;
            if (lit.EndsWith("f", StringComparison.CurrentCultureIgnoreCase)) {
                forceFloat = true;
                lit = lit.Slice(0, lit.Length - 1);
            }
            else
                forceFloat = false;
            if (lit.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase)) {
                lit = lit.Slice(2);

                var dotPos = lit.IndexOf('.');
                var ePos = lit.IndexOfAny('e', 'E');
                double pre, post, exp;
                if (dotPos >= 0) {
                    pre = dotPos == 0 ? 0 : ParseBaseDouble(lit.Slice(0, dotPos), 16);
                    if (ePos > 0) {
                        post = ParseBaseDouble(lit.Slice(dotPos + 1, ePos - dotPos - 1), 16);
                        exp = ParseBaseExponent(lit.Slice(ePos + 1), 16);
                    }
                    else {
                        post = ParseBaseDouble(lit.Slice(dotPos + 1), 16);
                        exp = 0;
                    }
                }
                else {
                    if (ePos > 0) {
                        pre = ParseBaseDouble(lit.Slice(0, ePos), 16);
                        post = 0;
                        exp = ParseBaseExponent(lit.Slice(ePos + 1), 16);
                    }
                    else {
                        pre = ParseBaseDouble(lit, 16);
                        post = 0;
                        exp = 0;
                    }
                }
                try {
                    checked {
                        var ret = (pre + Math.Pow(16, (int) -(Math.Log(post, 16) + 1)) * post) * Math.Pow(10, exp);
                        if (negative)
                            ret = -ret;
                        if (forceFloat || expectedReturnType == PrimitiveType.Float)
                            return new FloatLiteral(pos, (float) ret);
                        else
                            return new DoubleLiteral(pos, ret);
                    }
                }
                catch {
                    return err.Report($"The floating-point literal '{lit.ToString()}' is too {(negative ? "small" : "large")} for a {(forceFloat ? "float" : "double")}-value"
                        , pos, (forceFloat ? (ILiteral) new FloatLiteral(pos, float.NaN) : new DoubleLiteral(pos, double.NaN)));
                }
            }
            else if (lit.StartsWith("0b", StringComparison.CurrentCultureIgnoreCase)) {
                lit = lit.Slice(2);

                var dotPos = lit.IndexOf('.');
                var ePos = lit.IndexOfAny('e', 'E');
                double pre, post, exp;
                if (dotPos >= 0) {
                    pre = dotPos == 0 ? 0 : ParseBaseDouble(lit.Slice(0, dotPos), 2);
                    if (ePos > 0) {
                        post = ParseBaseDouble(lit.Slice(dotPos + 1, ePos - dotPos - 1), 2);
                        exp = ParseBaseExponent(lit.Slice(ePos + 1), 2);
                    }
                    else {
                        post = ParseBaseDouble(lit.Slice(dotPos + 1), 2);
                        exp = 0;
                    }
                }
                else {
                    if (ePos > 0) {
                        pre = ParseBaseDouble(lit.Slice(0, ePos), 2);
                        post = 0;
                        exp = ParseBaseExponent(lit.Slice(ePos + 1), 1);
                    }
                    else {
                        pre = ParseBaseDouble(lit, 2);
                        post = 0;
                        exp = 0;
                    }
                }
                try {
                    checked {
                        var ret = (pre + Math.Pow(2, (int) -(Math.Log(post, 2) + 1)) * post) * Math.Pow(10, exp);
                        if (negative)
                            ret = -ret;
                        if (forceFloat)
                            return new FloatLiteral(pos, (float) ret);
                        else
                            return new DoubleLiteral(pos, ret);
                    }
                }
                catch {
                    return err.Report($"The floating-point literal '{lit.ToString()}' is too {(negative ? "small" : "large")} for a {(forceFloat ? "float" : "double")}-value"
                       , pos, (forceFloat ? (ILiteral) new FloatLiteral(pos, float.NaN) : new DoubleLiteral(pos, double.NaN)));
                }
            }
            else {
                if (forceFloat) {
                    var litStr = negative ? $"-{lit.ToString()}" : lit.ToString();

                    if (float.TryParse(lit, out var fret))
                        return new FloatLiteral(pos, fret);
                    else
                        return err.Report($"The floating-point number {litStr} is too {(negative ? "small" : "large")} for a float-value", pos, new FloatLiteral(pos, float.NaN));
                }
                else {

                    if (float.TryParse(lit, out var fret))
                        return new FloatLiteral(pos, fret);
                    if (!double.TryParse(lit, out var ret)) {
                        err.Report($"Die Fließkommazahl {lit.ToString()} ist zu groß für ein double-Literal", pos);
                    }
                    return new DoubleLiteral(pos, ret);
                }
            }
        }
        protected virtual IType TypeFromSig(Type.Signature sig, Position pos, ErrorBuffer err = null) {
            IType tp;
            if (sig.GenericActualArguments.Any()) {
                // pick right type-template
                var ttps = contextStack.Peek().TypeTemplatesByName(sig.Name).Where(x => x.Signature.GenericFormalparameter.Count == sig.GenericActualArguments.Count);
                var ttp = Module.Semantics.BestFittingTypeTemplate(pos, ttps, sig.GenericActualArguments);
                if (ttp != null) {
                    // generic constraint-check is done by GenericTypeParameter.Replace(...) which will be called by ttp.BuildType(...)
                    tp = ttp.BuildType(sig.GenericActualArguments);
                }
                else {

                    if (ttps.Any())// error is already reported
                        tp = Type.Error;
                    else
                        tp = err.Report($"The type {sig} is not defined in this context", pos, Type.Error);
                }

            }
            else {
                if (!contextStack.Peek().Types.TryGetValue(sig, out tp)) {
                    if (sig.Name == "zint") {
                        tp = PrimitiveType.SizeT;
                    }
                    else if (!Enum.TryParse<PrimitiveName>(sig.Name, true, out var prim)) {
                        tp = err.ReportTypeError($"Der Typ {sig} ist im aktuellen Kontext nicht definiert", pos);
                    }
                    else {
                        tp = PrimitiveType.FromName(prim);
                    }
                }
            }
            return tp;
        }
        void ApplyReferenceCapability([NotNull] ITerminalNode cap, ref IType tp, string fileName, ErrorBuffer err) {
            if (tp.IsPrimitive(PrimitiveName.Void)) {
                err.Report("A reference-capability cannot be applied to void", cap.Position(fileName));
            }
            else if (tp.IsValueType()) {
                err.Report("A reference-capability cannot be applied to a value type", cap.Position(fileName));
            }
            tp = RefConstrained(tp, cap.GetText());
        }
        void VisitTypeModifier([NotNull] FBlangParser.TypeModifierContext context, ref IType tp, string fileName, ErrorBuffer err) {
            if (context.Iterable() != null) {
                tp = IterableType.Get(tp);
            }
            else if (context.Iterator() != null) {
                tp = IteratorType.Get(tp);
            }
            if (context.Async() != null) {
                tp = tp.AsAwaitable();
            }
            if (context.ReferenceCapability() != null) {
                ApplyReferenceCapability(context.ReferenceCapability(), ref tp, fileName, err);
            }
        }
        void VisitTypeSpecifier([NotNull] FBlangParser.TypeSpecifierContext context, ref IType tp, ref bool toret, string fileName, ErrorBuffer err) {
            if (context.array() != null) {
                tp = tp.AsArray();
                if (context.ExclamationMark() != null) {
                    tp = tp.AsNotNullable();
                }
            }
            else if (context.assocArray() != null) {
                var keyTy = VisitTypeIdent(context.assocArray().typeIdent(), fileName);
                tp = IntegratedHashMap.GetOrCreateIntegratedHashMap(Module).BuildType(new[] {  keyTy, tp });
                if (context.ExclamationMark() != null) {
                    tp = tp.AsNotNullable();
                }
            }
            else if (context.fsArray() != null) {
                if (context.fsArray().IntLit() != null) {
                    // var num = IntLiteral(x.fsArray().IntLit().Position(), x.fsArray().IntLit().GetText(), false);
                    if (!uint.TryParse(context.fsArray().IntLit().GetText(), out var num)) {
                        $"The Literal {context.fsArray().IntLit().GetText()} is to big for an uint-literal. The maximum value is {uint.MaxValue}.".Report(context.fsArray().IntLit().Position(fileName));
                    }
                    tp = tp.AsFixedSizedArray(num);
                }
                else {
                    var genLit = LiteralParameterByName(context.fsArray().Ident().GetText());
                    if (genLit != null) {
                        tp = tp.AsFixedSizedArray(genLit);
                    }
                    else {
                        tp = tp.AsFixedSizedArray(0);
                        err.Report($"The identifier '{context.fsArray().Ident().GetText()}' is not bound to a generic literalparameter", context.fsArray().Ident().Position(fileName));
                        toret = false;
                    }
                }
            }
            if (context.ReferenceCapability() != null) {
                ApplyReferenceCapability(context.ReferenceCapability(), ref tp, fileName, err);
            }
            if (context.typeModifier() != null) {
                foreach (var x in context.typeModifier().Reverse()) {
                    VisitTypeModifier(x, ref tp, fileName, err);
                }
            }
        }
        protected virtual bool TryVisitTypeIdent([NotNull] FBlangParser.TypeIdentContext context, out IType ret, ErrorBuffer err, string fileName) {
            //var sig = VisitTypeQualifier(context.typeQualifier());
            Type.Signature sig;
            if (context.typeQualifier() != null) {
                if (!TryVisitTypeQualifier(context.typeQualifier(), out sig, err, fileName)) {
                    ret = Type.Error;
                    return false;
                }
            }
            else {
                sig = PrimitiveType.Void.Signature;
            }
            var tp = TypeFromSig(sig, context.Position(fileName), err);
            if (context.Async() != null) {
                tp = tp.AsAwaitable();
            }
            if (context.ReferenceCapability() != null) {
                ApplyReferenceCapability(context.ReferenceCapability(), ref tp, fileName, err);
            }
            if (context.typeModifier() != null) {
                foreach (var x in context.typeModifier().Reverse()) {
                    VisitTypeModifier(x, ref tp, fileName, err);
                }
            }
            if (context.ExclamationMark() != null) {
                tp = tp.AsNotNullable();
            }
            else if (context.Dollar() != null) {
                tp = tp.AsValueType();
            }
            bool toret = true;
            foreach (var x in context.typeSpecifier()) {
                VisitTypeSpecifier(x, ref tp, ref toret, fileName, err);
            }
            ret = tp;
            return toret;
        }
        protected virtual IType VisitTypeIdent([NotNull] FBlangParser.TypeIdentContext context, string fileName) {
            TryVisitTypeIdent(context, out var ret, null, fileName);
            return ret;
        }
        protected virtual IType TypeByName(ITerminalNode name, string fileName) {
            return TypeFromSig(new Type.Signature(name.GetText(), null), name.Position(fileName));
        }
        protected virtual Type.Signature VisitTypeQualifier([NotNull] FBlangParser.TypeQualifierContext context, string fileName) {
            TryVisitTypeQualifier(context, out var ret, null, fileName);
            return ret;
        }
        protected virtual bool TryVisitTypeQualifier([NotNull] FBlangParser.TypeQualifierContext context, out Type.Signature ret, ErrorBuffer err, string fileName) {
            string name = context.typeName().GetText();
            if (context.typeName().primitiveName() != null && context.genericActualParameters() != null) {
                err.Report($"Der primitive Typ {name} kann nicht parametrisiert werden", context.Position(fileName));
                ret = null;
                return false;
            }
            IReadOnlyList<ITypeOrLiteral> genArgs = null;
            if (context.genericActualParameters() != null) {
                genArgs = VisitGenericActualParameters(context.genericActualParameters(), fileName);
            }
            ret = new Type.Signature(name, genArgs);
            return true;
        }
        protected virtual GenericLiteralParameter LiteralParameterByName(string name) {
            foreach (var ctx in contextStack.OfType<SimpleTypeTemplateContext>()) {
                if (ctx.TypeTemplate != null) {
                    var candidates = ctx.TypeTemplate.Signature.GenericFormalparameter.Where(x => x.Name == name);
                    if (candidates.Any()) {
                        var ret = candidates.First();
                        if (ret is GenericLiteralParameter genLit)
                            return genLit;
                        else {
                            break;
                        }
                    }
                }
            }
            return null;
        }
        protected virtual IReadOnlyList<ITypeOrLiteral> VisitGenericActualParameters([NotNull] FBlangParser.GenericActualParametersContext context, string fileName) {
            var ret = new List<ITypeOrLiteral>();

            foreach (var x in context.genericActualParameter()) {
                if (x.typeIdent() != null) {
                    ret.Add(VisitTypeIdent(x.typeIdent(), fileName) as IType);
                }
                else if (x.Ident() != null) {
                    var genLit = LiteralParameterByName(x.Ident().GetText());
                    if (genLit != null)
                        ret.Add(genLit);
                    else
                        ret.Add(TypeByName(x.Ident(), fileName) as IType);
                }
                else if (x.IntLit() != null) {
                    ret.Add(IntLiteral(context.Position(fileName), x.IntLit().GetText(), x.Minus() != null));
                }
                else if (x.StringLit() != null) {
                    ret.Add(Literal.SolveStringEscapes(context.Position(fileName), x.StringLit().GetText(), 1, x.StringLit().GetText().Length - 2));
                }
                else if (x.BoolLit() != null) {
                    if (x.BoolLit().GetText() == "true")
                        ret.Add(Literal.True);
                    else
                        ret.Add(Literal.False);
                }
                else if (x.CharLit() != null) {
                    ret.Add(Literal.SolveCharEscapes(context.Position(fileName), x.CharLit().GetText(), 1, x.CharLit().GetText().Length - 2));
                }
                else if (x.FloatLit() != null) {
                    ret.Add(FloatLiteral(context.Position(fileName), x.IntLit().GetText(), x.Minus() != null));
                }
            }

            return ret;
        }
    }
}
