/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using static CompilerInfrastructure.Contexts.SimpleMethodContext;

namespace CompilerInfrastructure.Semantics {
    using Type = Structure.Types.Type;

    public partial class BasicSemantics : ISemantics {
        /* public Func<IMethod, IExpression, bool> IsCallVirt {
             get;
             set;
         } = (met, par) => met.IsVirtual()
                         && !met.Specifiers.HasFlag(Method.Specifier.Final)
                         && par != null
                         && !(par is BaseExpression)
                         && met.Visibility != Visibility.Private
                         && met.NestedIn is ITypeContext tcx
                         && tcx.Type != null
                         && tcx.Type.CanBeInherited();*/
        public virtual bool IsCallVirt(IMethod met, IExpression par) {
            return
                met.IsVirtual() &&
                !met.Specifiers.HasFlag(Method.Specifier.Final) &&
                par != null &&
                !(par is BaseExpression) &&
                met.Visibility != Visibility.Private &&
                met.NestedIn is ITypeContext tcx &&
                tcx.Type != null &&
                tcx.Type.CanBeInherited();
        }
        public virtual bool IsFunctional(IType tp, out FunctionType fnTy) {
            if (tp is FunctionType fn) {
                fnTy = fn;
                return true;
            }
            fnTy = null;
            return false;
        }

        public virtual Error BestFittingMethod(Position pos, IEnumerable<IMethod> mets, ICollection<IType> argTypes, IType retType, out IMethod ret) {
            if (mets is null || !mets.Any()) {
                ret = Method.Error;
                return Error.NoValidTarget;
            }
            if (retType is null)
                retType = Type.Top;
            var firstFilter = new SortedSet<(IMethod, int)>(new FunctionalComparer<(IMethod, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var met in mets) {
                if (met is null)
                    continue;
                if (IsCompatible(met, argTypes, retType, out int diff))
                    firstFilter.Add((met, diff));
            }
            if (!firstFilter.Any()) {
                //return err.Report($"The callee for the method call {mets.First().Signature.Name}({string.Join(", ", argTypes.Select(x => x.Signature))}) cannot be resolved", pos, Method.Error);
                ret = Method.Error;
                return Error.NoValidTarget;
            }


            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var _ret = it.Current;
            var ret2 = new List<IMethod>();
            while (it.MoveNext() && it.Current.Item2 == _ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                /*return err.Report(string.Format("The method-call is ambigious. Possible callees are: {0}",
                    string.Join(",\r\n", ret2.Concat(new[] { ret.Item1 }).Select(x => x.Signature))
                ), pos, Method.Error);*/
                ret = Method.Error;
                return Error.AmbiguousTarget;
            }
            else {
                ret = _ret.Item1;
                return Error.None;
            }
        }
        internal protected IEnumerable<IMethod> DidYouMean(IEnumerable<IMethod> mets, ICollection<IType> argTypes, IType retType) {
            var filter = new SortedSet<(IMethod, int)>(new FunctionalComparer<(IMethod, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var met in mets) {
                if (met is null)
                    continue;
                IsCompatible(met, argTypes, retType, out int diff);
                filter.Add((met, diff));
            }
            return filter.Select(x => x.Item1).Take(5);
        }
        internal protected IEnumerable<IMethodTemplate<T>> DidYouMean<T>(IEnumerable<IMethodTemplate<T>> mets, ICollection<IType> argTypes, IType retType) where T : IMethod {
            var filter = new SortedSet<(IMethodTemplate<T>, int)>(new FunctionalComparer<(IMethodTemplate<T>, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var met in mets) {
                if (met is null)
                    continue;
                IsCompatible(met, argTypes, retType, out _, out int diff);
                filter.Add((met, diff));
            }
            return filter.Select(x => x.Item1).Take(5);
        }
        public virtual IMethod BestFittingMethod(Position pos, IEnumerable<IMethod> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null) {

            if (mets is null || !mets.Any())
                return Method.Error;
            if (retType is null)
                retType = Type.Top;
            var firstFilter = new SortedSet<(IMethod, int)>(new FunctionalComparer<(IMethod, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var met in mets) {
                if (met is null)
                    continue;
                if (IsCompatible(met, argTypes, retType, out int diff))
                    firstFilter.Add((met, diff));
            }
            if (!firstFilter.Any()) {

                // determine the nearest method to call
                var dym = DidYouMean(mets, argTypes, retType);
                return err.Report($"The callee for the method call {(retType.IsTop() ? "" : retType.ToString())} {mets.First().Signature.Name}({string.Join(", ", argTypes.Select(x => x.Signature))}) cannot be resolved. Did you mean {(!dym.Any() ? "" : string.Join(", or ", dym))}", pos, Method.Error);
            }
            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var ret = it.Current;
            var ret2 = new List<IMethod>();
            while (it.MoveNext() && it.Current.Item2 == ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                return err.Report(string.Format("The method-call is ambiguous. Possible callees are: {0}",
                    string.Join(",\r\n", ret2.Concat(new[] { ret.Item1 }).Select(x => x.Signature))
                ), pos, Method.Error);
            }
            else {
                return ret.Item1;
            }
        }
        public virtual Error BestFittingMethod<T>(Position pos, IEnumerable<IMethodTemplate<T>> mets, ICollection<IType> argTypes, IType retType, out IMethod ret) where T : IMethod {
            if (mets is null || !mets.Any()) {
                ret = Method.Error;
                return Error.NoValidTarget;
            }
            if (retType is null)
                retType = Type.Top;
            var firstFilter = new SortedSet<(IMethod, int)>(new FunctionalComparer<(IMethod, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var tm in mets) {
                if (tm is null)
                    continue;
                if (IsCompatible(tm, argTypes, retType, out IMethod met, out int diff))
                    firstFilter.Add((met, diff));
            }
            if (!firstFilter.Any()) {
                ret = Method.Error;
                return Error.NoValidTarget;
                //return err.Report($"The callee for the method call {mets.First().Signature.Name}({string.Join(", ", argTypes.Select(x => x.Signature))}) cannot be resolved", pos, Method.Error);
            }
            using SortedSet<(IMethod, int)>.Enumerator it = firstFilter.GetEnumerator();
            it.MoveNext();
            var _ret = it.Current;
            var ret2 = new List<IMethod>();
            while (it.MoveNext() && it.Current.Item2 == _ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                /*return err.Report(string.Format("The method-call is ambigious. Possible callees are: {0}",
                    string.Join(",\r\n", ret2.Concat(new[] { ret.Item1 }).Select(x => x.Signature))
                ), pos, Method.Error);*/
                ret = Method.Error;
                return Error.AmbiguousTarget;
            }
            else {
                ret = _ret.Item1;
                return Error.None;
            }
        }
        public virtual IMethod BestFittingMethod<T>(Position pos, IEnumerable<IMethodTemplate<T>> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null) where T : IMethod {
            if (mets is null || !mets.Any())
                return Method.Error;
            if (retType is null)
                retType = Type.Top;
            var firstFilter = new SortedSet<(IMethod, int)>(new FunctionalComparer<(IMethod, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var tm in mets) {
                if (tm is null)
                    continue;
                if (IsCompatible(tm, argTypes, retType, out IMethod met, out int diff))
                    firstFilter.Add((met, diff));
            }
            if (!firstFilter.Any()) {

                //DOLATER determine the nearest method to call
                var dym = DidYouMean(mets, argTypes, retType);
                return err.Report($"The callee for the method call {mets.First().Signature.Name}({string.Join(", ", argTypes.Select(x => x.Signature))}) cannot be resolved. Did you mean {(!dym.Any() ? "" : string.Join(", or ", dym))}", pos, Method.Error);

            }
            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var ret = it.Current;
            var ret2 = new List<IMethod>();
            while (it.MoveNext() && it.Current.Item2 == ret.Item2) {
                ret2.Add(it.Current.Item1);
            }
            if (ret2.Any()) {
                return err.Report(string.Format("The method-call is ambigious. Possible callees are: {0}",
                    string.Join(",\r\n", ret2.Concat(new[] { ret.Item1 }).Select(x => x.Signature))
                ), pos, Method.Error);
            }
            else {
                return ret.Item1;
            }
        }
        public virtual IMethod BestFittingMethod(Position pos, IEnumerable<IDeclaredMethod> mets, ICollection<IType> argTypes, IType retType, ErrorBuffer err = null) {
            var met = BestFittingMethod(pos, mets.OfType<IMethod>(), argTypes, retType, err);
            var cmet = BestFittingMethod(pos, mets.OfType<IMethodTemplate<IMethod>>(), argTypes, retType, err);
            if (!met.IsError()) {
                if (!cmet.IsError()) {
                    IsCompatible(met, argTypes, retType, out int metDiff);
                    IsCompatible(cmet, argTypes, retType, out int cmetDiff);
                    if (metDiff <= cmetDiff)
                        return met;
                    else
                        return cmet;
                }
                else {
                    return met;
                }
            }
            else {
                return cmet;
            }
        }
        public virtual ITypeTemplate<IType> BestFittingTypeTemplate(Position pos, IEnumerable<ITypeTemplate<IType>> types, IReadOnlyList<ITypeOrLiteral> genArgs, ErrorBuffer err = null) {
            if (types is null || !types.Any())
                return null;
            var firstFilter = new SortedSet<(ITypeTemplate<IType>, int)>(new FunctionalComparer<(ITypeTemplate<IType>, int)>((x, y) => x.Item2 - y.Item2));
            foreach (var ttp in types) {
                if (ttp is null)
                    continue;
                var diffs = new List<int>(genArgs.Count);
                int i = 0;
                foreach (var arg in genArgs) {
                    var genFormArg = ttp.Signature.GenericFormalparameter[i];
                    if (genFormArg is GenericLiteralParameter genLit) {
                        if (arg is ILiteral lit) {
                            // if numeric => subtyping, else exact type
                            // care about unsigned !!
                            if (genLit.ReturnType.IsNumericType() && lit.ReturnType.IsSubTypeOf(genLit.ReturnType, out int diff) && diff >= 0) {
                                if (genLit.IsUnsigned && lit.IsPositiveValue || !genLit.IsUnsigned) {
                                    diffs.Add(diff);
                                }
                                else
                                    break;
                            }
                            else {
                                if (genLit.ReturnType == lit.ReturnType) {
                                    diffs.Add(0);
                                }
                                else
                                    break;
                            }
                        }
                    }
                    else if (genFormArg is GenericTypeParameter genTp) {
                        if (arg is IType tp) {
                            int diff = short.MaxValue;//prefer constrained typeparameters
                            if (genTp.SuperType != null) {
                                if (!tp.IsSubTypeOf(genTp.SuperType, out diff)) {
                                    break;
                                }
                                foreach (var intf in genTp.ImplementingInterfaces) {
                                    if (!tp.ImplementsInterface(intf)) {
                                        break;
                                    }
                                }

                            }
                            diffs.Add(diff);
                        }
                    }
                    i++;
                }
                if (diffs.Count < genArgs.Count) {
                    continue;
                }
                firstFilter.Add((ttp, diffs.Aggregate((x, y) => x * x + y * y)));

            }
            if (!firstFilter.Any()) {
                return err.Report<ITypeTemplate<IType>>($"The type-template for the substitution {types.First().Signature.Name}<{string.Join(", ", genArgs)}> cannot be resolved", pos, null);
            }
            using var it = firstFilter.GetEnumerator();
            it.MoveNext();
            var ret = it.Current;
            var other = new List<ITypeTemplate<IType>>();
            while (it.MoveNext() && it.Current.Item2 == ret.Item2) {
                other.Add(it.Current.Item1);
            }
            if (other.Any())
                return err.Report<ITypeTemplate<IType>>(string.Format("The type-template specialization is ambigious. Possible templates are {0}",
                    string.Join(",\r\n", other.Concat(new[] { ret.Item1 }).Select(x => x.Signature))
                ), pos, null);

            return ret.Item1;
        }
        #region BestFittingMethod - Utilities
        public virtual bool IsCompatible(FunctionType actualFn, FunctionType formalFn, out int diff) {
            var formalArgs = actualFn.ArgumentTypes.Select(x => new BasicVariable(formalFn.Position, x, Variable.Specifier.LocalVariable, "arg", null)).ToArray();
            return IsCompatible(formalArgs, formalFn.ReturnType, actualFn.ArgumentTypes, actualFn.ReturnType, out diff);
        }
        public virtual bool IsCompatible(IVariable[] formalArgs, IType formalRetTy, ICollection<IType> actualArgTps, IType actualRetTy, out int diff) {
            const int MISSING_ARG_COST = 25;
            bool succ = true;
            diff = 0;
            // compatible arglists 
            if (actualArgTps.Count < formalArgs.Length - 1) {
                // too few arguments
                diff = (actualArgTps.Count - formalArgs.Length) * (actualArgTps.Count - formalArgs.Length) * MISSING_ARG_COST;
                succ = false;
            }

            if (!actualRetTy.IsTop()) {
                if (actualRetTy.IsSubTypeOf(formalRetTy, out var rtDiff)) {
                    diff += rtDiff * rtDiff;
                }
                else if (CanBePassedAsParameterTo(actualRetTy, formalRetTy, out var localDiff)) {
                    diff += localDiff * localDiff;
                }
                else {
                    diff = MISSING_ARG_COST;
                    succ = false;
                }
            }
            if (formalArgs.Length == 0)
                return actualArgTps.Count == 0;

            using (var actArgs = actualArgTps.GetEnumerator()) {

                for (int i = 0; i < formalArgs.Length - 1 && actArgs.MoveNext(); ++i) {
                    if (CanBePassedAsParameterTo(actArgs.Current, formalArgs[i].Type, out int localDiff)) {
                        diff += localDiff * localDiff;
                    }
                    else if (IsFunctional(actArgs.Current, out var actFnTy) && IsFunctional(formalArgs[i].Type, out var formFnTy) && IsCompatible(actFnTy, formFnTy, out int fnDiff)) {
                        diff += fnDiff * fnDiff;
                    }
                    else {
                        //diff = int.MaxValue;
                        diff += MISSING_ARG_COST;
                        succ = false;
                    }
                }

                if (formalArgs[^1].Type.IsVarArg()) {

                    var varArgTy = (formalArgs[^1].Type as IWrapperType).ItemType;
                    while (actArgs.MoveNext()) {
                        if (actArgs.Current.IsVarArg()) {
                            var itemTy = (actArgs.Current as IWrapperType).ItemType;
                            if (varArgTy.CanBeInherited() && itemTy.IsSubTypeOf(varArgTy, out int localDiff)) {
                                diff += localDiff * localDiff;
                            }
                            else if (varArgTy.CanBeInherited() && IsFunctional(varArgTy, out var vaFnTy) && IsFunctional(itemTy, out var itFnTy) && IsCompatible(itFnTy, vaFnTy, out int fnDiff)) {
                                diff += fnDiff * fnDiff;
                            }
                            else if (itemTy != varArgTy) {
                                // a wrong vararg-parameter is not that bad...
                                diff += (int) Math.Sqrt(MISSING_ARG_COST);
                                succ = false;
                            }
                        }
                        else {
                            if (actArgs.Current.IsSubTypeOf(varArgTy, out int localDiff)) {
                                diff += localDiff * localDiff;
                            }
                            else if (IsFunctional(varArgTy, out var vaFnTy) && IsFunctional(actArgs.Current, out var itFnTy) && IsCompatible(itFnTy, vaFnTy, out int fnDiff)) {
                                diff += fnDiff * fnDiff;
                            }
                            else {
                                // a wrong vararg-parameter is not that bad...
                                diff = (int) Math.Sqrt(MISSING_ARG_COST);
                                succ = false;
                            }
                        }
                    }
                    if (actualArgTps.Count > formalArgs.Length)//prefer non-variadic overloads
                        diff++;
                }
                else {
                    if (!actArgs.MoveNext()) {
                        // too few arguments
                        diff += MISSING_ARG_COST;
                        succ = false;
                    }
                    if (CanBePassedAsParameterTo(actArgs.Current, formalArgs[^1].Type, out int localDiff)) {
                        diff += localDiff * localDiff;
                    }
                    else if (IsFunctional(actArgs.Current, out var actFnTy) && IsFunctional(formalArgs[^1].Type, out var formFnTy) && IsCompatible(actFnTy, formFnTy, out int fnDiff)) {
                        diff += fnDiff * fnDiff;
                    }
                    else {
                        diff += MISSING_ARG_COST;
                        succ = false;
                    }
                    if (actArgs.MoveNext()) {
                        // too many arguments
                        diff += (actualArgTps.Count - formalArgs.Length) * (actualArgTps.Count - formalArgs.Length) * MISSING_ARG_COST;
                        succ = false;
                    }
                }
            }

            return succ;
        }
        public virtual bool IsCompatible(IMethod met, ICollection<IType> actualArgTps, IType actualRetTy, out int diff) {
            return IsCompatible(met.Arguments, met.ReturnType, actualArgTps, actualRetTy, out diff);
        }
        public virtual bool TryGetSubstitution(IType formalType, ITypeOrLiteral actualType, out IEnumerable<(IGenericParameter, ITypeOrLiteral)> ret) {
            ret = Enumerable.Empty<(IGenericParameter, ITypeOrLiteral)>();
            if (actualType == formalType || actualType is IType atp && atp.IsSubTypeOf(formalType)) {
                return true;
            }
            if (formalType is GenericTypeParameter genTp) {
                if (genTp.CanReplace(actualType)) {
                    ret = ret.Append((genTp, actualType));
                }
                else {
                    return false;
                }
            }
            else if (actualType is IType aty && formalType.Signature.BaseGenericType != null && aty.Signature.BaseGenericType != null) {
                if (formalType.Signature.BaseGenericType == aty.Signature.BaseGenericType) {
                    for (int i = 0; i < formalType.Signature.GenericActualArguments.Count; ++i) {
                        if (formalType.Signature.GenericActualArguments[i] is IType formTp) {
                            var actTp = aty.Signature.GenericActualArguments[i] as IType;
                            if (!TryGetSubstitution(formTp, actTp, out var nxt)) {
                                return false;
                            }
                            else {
                                /*ret = ret.Concat(nxt.Select(x => {
                                    return (x.Item1, (ITypeOrLiteral)formTp.Replace(new GenericParameterMap<IGenericParameter, ITypeOrLiteral>() { [x.Item1] = x.Item2 }, formTp.Context, formTp.Context));
                                }));*/
                                ret = ret.Concat(nxt);
                            }
                        }
                    }
                }
                else
                    return false;
            }
            return true;
        }
        public virtual bool CanSubstitute(IReadOnlyList<IGenericParameter> genFormalArgs, IVariable[] formalArgs, IType formalArgTy,
                                          ICollection<IType> actualArgTypes, IType actualRetTy, out GenericParameterMap<IGenericParameter, ITypeOrLiteral> dic) {
            if (formalArgs.Length - 1 > actualArgTypes.Count) {
                dic = null;
                return false;
            }
            dic = new Dictionary<IGenericParameter, ITypeOrLiteral>();

            static bool AddSubstitution(GenericParameterMap<IGenericParameter, ITypeOrLiteral> _dic, IEnumerable<(IGenericParameter, ITypeOrLiteral)> ret) {
                foreach (var kvp in ret) {
                    var gen = kvp.Item1;
                    var subs = kvp.Item2;
                    if (_dic.TryGetValue(gen, out var prevSubs)) {
                        if (prevSubs is IType genTy) {
                            var mergeTy = Type.MostSpecialCommonSuperType(genTy, subs as IType);
                            if (mergeTy.IsError())
                                return false;
                        }
                        else if (prevSubs is ILiteral genLit) {
                            //literal
                            if (!genLit.ValueEquals(subs as ILiteral)) {
                                return false;
                            }
                        }
                    }
                    else {
                        _dic.Add(gen, subs);
                    }
                }
                return true;
            }

            using (var actArgs = actualArgTypes.GetEnumerator()) {
                for (int i = 0; i < formalArgs.Length - 1 && actArgs.MoveNext(); ++i) {
                    if (TryGetSubstitution(formalArgs[i].Type, actArgs.Current, out var ret)) {
                        if (!AddSubstitution(dic, ret))
                            return false;
                    }
                    else
                        return false;
                }
                if (formalArgs[^1].Type.IsVarArg()) {
                    var varArgTy = (formalArgs[^1].Type as IWrapperType).ItemType;
                    while (actArgs.MoveNext()) {
                        if (actArgs.Current.IsVarArg()) {
                            var itemTy = (actArgs.Current as IWrapperType).ItemType;
                            if (TryGetSubstitution(varArgTy, itemTy, out var ret)) {
                                if (!AddSubstitution(dic, ret))
                                    return false;
                            }
                            else
                                return false;
                        }
                        else {
                            if (TryGetSubstitution(varArgTy, actArgs.Current, out var ret)) {
                                if (!AddSubstitution(dic, ret))
                                    return false;
                            }
                            else
                                return false;
                        }
                    }
                }
                else {
                    if (!actArgs.MoveNext())
                        return false;
                    if (TryGetSubstitution(formalArgs[^1].Type, actArgs.Current, out var ret)) {
                        if (!AddSubstitution(dic, ret))
                            return false;
                    }
                    else
                        return false;
                    if (actArgs.MoveNext())
                        return false;
                }
            }
            return dic.Count == genFormalArgs.Count;
        }
        public virtual bool IsCompatible<T>(IMethodTemplate<T> tm, ICollection<IType> actualArgTps, IType actualRetTy, out IMethod met, out int diff) where T : IMethod {
            if (CanSubstitute(tm.Signature.GenericFormalparameter, tm.Arguments, tm.ReturnType, actualArgTps, actualRetTy, out var dic)) {
                met = tm.BuildMethod(dic);
                return IsCompatible(met, actualArgTps, actualRetTy, out diff);
            }
            met = Method.Error;
            diff = int.MaxValue;
            return false;
        }

        public static bool IsRefAssignable(IType objTy, IType varTy, ReferenceKind lvalue) {
            if (Type.IsAssignable(objTy, varTy)) {
                switch (lvalue) {
                    case ReferenceKind.None:
                        return !varTy.IsByRef();
                    case ReferenceKind.RValue:
                        return true;
                    case ReferenceKind.ConstLValue:
                        return varTy.IsByConstRef();
                    case ReferenceKind.MutableLValue:
                        return varTy.IsByMutableRef();
                }
            }
            return false;
        }
        public static IType AsRefType(IType argTy, out ReferenceKind lvalue, ReferenceKind dflt = ReferenceKind.RValue) {
            if (argTy.IsByRef()) {
                if (argTy.IsByConstRef())
                    lvalue = ReferenceKind.ConstLValue;
                else
                    lvalue = ReferenceKind.MutableLValue;
                return argTy.Cast<ByRefType>().UnderlyingType;
            }
            else {
                lvalue = dflt;
                return argTy;
            }
        }
        public virtual bool MatchesArgument(IDeclaredMethod met, IType ty, ReferenceKind lvalue, uint argNo) {
            if (argNo >= met.Arguments.Length) {
                if (met.Arguments.Any() && met.Arguments.Last().Type.TryCast<VarArgType>(out var varArgTy)) {
                    var varTy = varArgTy.ItemType;
                    return IsRefAssignable(ty, varTy, lvalue);
                }
                return false;
            }
            else {
                var varTy = met.Arguments[argNo].Type;
                if (varTy.IsVarArg())
                    varTy = varTy.Cast<VarArgType>().ItemType;
                return IsRefAssignable(ty, varTy, lvalue);
            }
        }
        public static IType ArgumentType(IDeclaredMethod met, out ReferenceKind lvalue, uint argNo, Position? pos = null, ReferenceKind dfltLV = ReferenceKind.RValue, ErrorBuffer err = null) {
            return ArgumentType(met, out lvalue, out _, argNo, pos, dfltLV, err);
        }
        public static IType ArgumentType(IDeclaredMethod met, out ReferenceKind lvalue, out IVariable formalArgument, uint argNo, Position? pos = null, ReferenceKind dfltLV = ReferenceKind.RValue, ErrorBuffer err = null) {
            if (argNo >= met.Arguments.Length) {
                if (met.Arguments.Any() && met.Arguments.Last().Type.TryCast<VarArgType>(out var varArgTy)) {
                    var varTy = varArgTy.ItemType;
                    formalArgument = met.Arguments.Last();
                    return AsRefType(varTy, out lvalue, dfltLV);
                }
                lvalue = dfltLV;
                formalArgument = Variable.Error;
                return err.ReportTypeError($"The method {met} has only {met.Arguments.Length} parameters and therefore cannot access argument no {argNo + 1}", pos);
            }
            else {
                var varTy = (formalArgument = met.Arguments[argNo]).Type;
                if (varTy.IsVarArg())
                    varTy = varTy.Cast<VarArgType>().ItemType;
                return AsRefType(varTy, out lvalue, dfltLV);
            }
        }
        #endregion
        #region operator-overloads
        // solve operator-overloads
        protected virtual IExpression BinaryOperatorOverload(Position pos, IType retTy, IExpression lhs, OverloadableOperator ov, IExpression rhs, ErrorBuffer err) {
            if (lhs.ReturnType.OverloadsOperator(ov, out var mets, VisibleMembers.Instance)) {
                var callee = BestFittingMethod(pos, mets, new[] { rhs.ReturnType }, retTy, err);
                return new CallExpression(pos, retTy, callee, lhs, new[] { rhs }) { IsCallVirt = IsCallVirt(callee, lhs) };
            }
            else if (lhs.ReturnType.OverloadsOperator(ov, out mets, VisibleMembers.Static) | rhs.ReturnType.OverloadsOperator(ov, out var rhsMets, VisibleMembers.Static)) {
                var callee = BestFittingMethod(pos, mets.Concat(rhsMets), new[] { lhs.ReturnType, rhs.ReturnType }, retTy, err);
                return new CallExpression(pos, retTy, callee, null, new[] { lhs, rhs });
            }

            return err.Report($"The binary operator {ov} is not overloaded for operands of the types {lhs.ReturnType.Signature} and {rhs.ReturnType.Signature}", pos, Expression.Error);
        }
        protected virtual IExpression UnaryOperatorOverload(Position pos, IType retTy, IExpression subEx, OverloadableOperator ov, ErrorBuffer err) {
            if (subEx.ReturnType.OverloadsOperator(ov, out var mets, VisibleMembers.Instance)) {
                var callee = BestFittingMethod(pos, mets, Array.Empty<IType>(), retTy, err);
                return new CallExpression(pos, retTy, callee, subEx, Array.Empty<IExpression>()) {
                    IsCallVirt = IsCallVirt(callee, subEx)
                };
            }
            else if (subEx.ReturnType.OverloadsOperator(ov, out mets, VisibleMembers.Static)) {
                var callee = BestFittingMethod(pos, mets, new[] { subEx.ReturnType }, retTy, err);
                return new CallExpression(pos, retTy, callee, null, new[] { subEx });
            }

            return err.Report($"The unary operator {ov} is not overloaded for an operand of the type {subEx.ReturnType.Signature}", pos, Expression.Error);
        }

        protected virtual IExpression CreateAssignment(Position pos, IType retTy, IExpression lhs, BinOp.OperatorKind op, IExpression rhs, ErrorBuffer err = null) {
            // retTy is already Binop.InferredreturnType
            if (!Type.IsAssignable(rhs.ReturnType, lhs.ReturnType))
                $"A value of type {rhs.ReturnType.Signature} cannot be assigned to a variable of type {lhs.ReturnType.Signature}, since there is no implicit conversion between these types".Report(pos);
            return new BinOp(pos, retTy, lhs, op, rhs);
        }
        public virtual IExpression CreateBinOp(Position pos, IType retTy, IExpression lhs, BinOp.OperatorKind op, IExpression rhs, ErrorBuffer err = null) {
            retTy = BinOp.InferredReturnType(pos, op, lhs.ReturnType, rhs.ReturnType, retTy);
            if (retTy.IsString() && op == BinOp.OperatorKind.ADD) {
                if (lhs is StringLiteral slit && string.IsNullOrEmpty(slit.Value))
                    return rhs.ReturnType.IsString() ? rhs : new TypecastExpression(pos, rhs, PrimitiveType.String);
                else if (rhs is StringLiteral slit2 && string.IsNullOrEmpty(slit2.Value))
                    return lhs.ReturnType.IsString() ? lhs : new TypecastExpression(pos, lhs, PrimitiveType.String);
            }
            else if (retTy.IsString() && op == BinOp.OperatorKind.MUL) {
                if (lhs.ReturnType.IsString() && !rhs.ReturnType.IsSubTypeOf(PrimitiveType.UInt) || rhs.ReturnType.IsString() && !lhs.ReturnType.IsSubTypeOf(PrimitiveType.UInt)) {
                    err.Report("The factor of a string-multiplication must be an unsigned integer of at most 32Bit (uint)");
                }
            }
            if (!retTy.IsError() && retTy.IsPrimitive() || lhs.ReturnType.IsError() || rhs.ReturnType.IsError())
                return new BinOp(pos, retTy, lhs, op, rhs);
            else if (op.IsAssignment())
                return CreateAssignment(pos, retTy.IsError() ? null : retTy, lhs, op, rhs, err);
            else if (op.IsOverloadable(out var ov)) {
                return BinaryOperatorOverload(pos, retTy.IsError() ? null : retTy, lhs, ov, rhs, err);
            }
            else {
                err.Report($"The binary operator {op} is not defined for operands of the types {lhs.ReturnType.Signature} and {rhs.ReturnType.Signature}", pos);
            }
            return Expression.Error;
        }
        public virtual IExpression CreateUnOp(Position pos, IType retTy, UnOp.OperatorKind op, IExpression subEx, ErrorBuffer err = null) {
            retTy = UnOp.InferredReturnType(pos, op, subEx.ReturnType, retTy, err);
            var tp = subEx.ReturnType;
            switch (op) {
                case UnOp.OperatorKind.UNPACK when tp.IsArray() || tp.IsArraySlice() || tp.IsVarArg():
                case UnOp.OperatorKind.AWAIT when tp.IsAwaitable(): {
                    return new UnOp(pos, retTy, op, subEx);
                }
            }
            if (retTy.IsError() || op != UnOp.OperatorKind.UNPACK && op != UnOp.OperatorKind.AWAIT && retTy.IsPrimitive())
                return new UnOp(pos, retTy, op, subEx);
            else if (op.IsOverloadable(out var ov)) {
                return UnaryOperatorOverload(pos, retTy, subEx, ov, err);
            }
            else {
                err.Report($"The unary operator {op} is not defined for an operand of the type {subEx.ReturnType.Signature}", pos);
            }
            return Expression.Error;
        }
        public virtual IExpression CreateCall(Position pos, IType retTy, IType parentTy, IExpression parent, string name, ICollection<IExpression> args, ErrorBuffer err = null) {
            // call + fnCallOperatorOverload (is overload <=> name==null)
            CallExpression ret = null;
            if (name is null) {
                if (parent is null) {
                    if (parentTy is null)
                        return err.Report($"A type cannot be invoken like a method", pos, Expression.Error);
                    else
                        return err.Report($"The type {parentTy.Signature} cannot be invoked like a method", pos, Expression.Error);
                }
                else {
                    if (parentTy.IsTop()) {
                        ret = new CallExpression(pos, OverloadableOperator.FunctionCall.OperatorName(), null, parent, args, retTy);
                    }
                    else {

                        // call fnCallOperatorOverload
                        if (parentTy.OverloadsOperator(OverloadableOperator.FunctionCall, out var mets, VisibleMembers.Instance)) {
                            if (args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack)) {
                                ret = new CallExpression(pos, OverloadableOperator.FunctionCall.OperatorName(), mets, parent, args, retTy);
                            }
                            else {

                                var callee = BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Count), retTy, err);
                                ret = new CallExpression(pos, callee.ReturnType, callee, parent, args) {
                                    IsCallVirt = IsCallVirt(callee, parent)
                                };
                            }
                        }
                        else {
                            return err.Report($"The type {parentTy.Signature} does not overload the function-call operator", pos, Expression.Error);
                        }
                    }
                }
            }
            else {
                IMethod callee = null;
                if (parent is null) {
                    // call static-method
                    var mets = (parentTy.Context.StaticContext.MethodsByName(name) as IEnumerable<IDeclaredMethod>).Concat(parentTy.Context.StaticContext.MethodTemplatesByName(name));
                    if (args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack)) {
                        ret = new CallExpression(pos, name, mets, parent, args, retTy);
                    }
                    else
                        callee = BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Count), retTy, err);

                }
                else {
                    // call instance method
                    if (parentTy.IsTop()) {
                        ret = new CallExpression(pos, name, null, parent, args, retTy);
                    }
                    else {

                        var mets = (parentTy.Context.InstanceContext.MethodsByName(name) as IEnumerable<IDeclaredMethod>).Concat(parentTy.Context.InstanceContext.MethodTemplatesByName(name));
                        if (args.Any(x => x is ExpressionParameterAccess || x is ExpressionParameterPackUnpack)) {
                            ret = new CallExpression(pos, name, mets, parent, args, retTy);
                        }
                        else

                            callee = BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Count), retTy, err);
                    }
                }
                if (ret is null)
                    ret = new CallExpression(pos, callee.ReturnType, callee, parent, args) {
                        IsCallVirt = IsCallVirt(callee, parent)
                    };
            }

            if (!ret.IsError()) {
                int i = 0;
                foreach (var arg in args) {
                    if (arg is DefaultValueExpression dflt && dflt.ReturnType.IsTop()) {
                        dflt.ResetReturnType(ret.Callee.Arguments[i].Type);
                    }
                    i++;
                }
            }
            return ret;
        }

        public virtual IStatement CreateDeconstruction(Position pos, ICollection<IExpression> dest, IExpression range, ErrorBuffer err = null, IMethod met = null) {
            var parentTy = range?.ReturnType ?? Type.Error;
            Deconstruction.CheckConsistency(pos, dest, range, err, met);
            if (parentTy.IsArray() || parentTy.IsArraySlice() || parentTy.IsError()) {
                return new Deconstruction(pos, dest, range);
            }
            else {
                if (parentTy.OverloadsOperator(OverloadableOperator.Deconstruct, out var mets, VisibleMembers.Instance)) {
                    var callee = BestFittingMethod(pos, mets, dest.Select(x => x.ReturnType).AsCollection(dest.Count), PrimitiveType.Void, err);
                    return new ExpressionStmt(pos, new CallExpression(pos, callee.ReturnType, callee, range, dest) { IsCallVirt = IsCallVirt(callee, range) });
                }
                else {
                    if (!parentTy.IsError())
                        return err.Report($"The type {parentTy.Signature} does not overload the deconstruction-operator", pos, Statement.Error);
                    return Statement.Error;
                }
            }
        }

        public virtual IExpression CreateDefault(Position pos, IType ofType, ErrorBuffer err = null) {
            if (ofType.OverloadsOperator(OverloadableOperator.Default, out var dflt, VisibleMembers.Static)) {
                var callee = BestFittingMethod(pos, dflt, Array.Empty<IType>(), ofType, err);
                return new CallExpression(pos, ofType, callee, null, Array.Empty<IExpression>());
            }
            else {
                return new DefaultValueExpression(pos, ofType);
            }
        }

        public virtual IExpression CreateIndexer(Position pos, IType retTy, IType parentTy, IExpression parent, ICollection<IExpression> args, ErrorBuffer err = null) {
            if (parentTy.IsArray() || parentTy.IsArraySlice() || parentTy.IsError()) {
                if (args.Count == 2 && args.Last() is DefaultValueExpression dflt) {
                    dflt.ResetReturnType((parentTy.UnWrap() as AggregateType).ItemType);
                }
                return new IndexerExpression(pos, retTy, parent ?? err.Report("A not-existing object cannot be indexed", pos, Expression.Error), args);
            }
            else {
                if (parentTy.OverloadsOperator(OverloadableOperator.Indexer, out var mets, VisibleMembers.Instance)) {
                    var callee = BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Count), retTy, err);
                    var ret = new CallExpression(pos, callee.ReturnType, callee, parent ?? err.Report("A not-existing object cannot be indexed", pos, Expression.Error), args) {
                        IsCallVirt = IsCallVirt(callee, parent)
                    };
                    if (retTy != null && retTy.IsPrimitive(PrimitiveName.Void) && args.Any()) {
                        if (args.Last() is DefaultValueExpression dflt && dflt.ReturnType.IsTop()) {
                            var tp = ret.Callee.Arguments.Last().Type.IsVarArg()
                                ? (ret.Callee.Arguments.Last().Type as VarArgType).ItemType
                                : ret.Callee.Arguments.Last().Type;

                            dflt.ResetReturnType(tp);
                        }
                        return new IndexerSetOverload(pos, ret);
                    }
                    return ret;
                }
                else {
                    if (parentTy != null)
                        return err.Report($"The type {parentTy.Signature} does not overload the indexer-operator", pos, Expression.Error);
                    else
                        return err.Report("The indexer-operator is not overloaded for the given type", pos, Expression.Error);
                }
            }
        }

        public virtual IExpression CreateRangedIndexer(Position pos, IType retTy, IType parentTy, IExpression parent, IExpression offset, IExpression count, ErrorBuffer err = null) {
            if (parentTy.IsArray() || parentTy.IsArraySlice() || parentTy.IsError()) {
                return new RangedIndexerExpression(pos, retTy ?? Type.Error,
                    parent ?? err.Report("A not-existing object cannot be indexed by range", pos, Expression.Error),
                    offset, count);
            }
            else {
                if (parentTy.OverloadsOperator(OverloadableOperator.RangedIndexer, out var mets, VisibleMembers.Instance)) {
                    var args = new[] { offset, count };
                    var callee = BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Length), retTy, err);
                    return new CallExpression(pos, callee.ReturnType, callee, parent ?? err.Report("A not-existing object cannot be indexed by range", pos, Expression.Error), args) {
                        IsCallVirt = IsCallVirt(callee, parent)
                    };
                }
                else {
                    if (parentTy != null)
                        return err.Report($"The type {parentTy.Signature} does not overload the ranged indexer-operator", pos, Expression.Error);
                    else
                        return err.Report("The ranged indexer-operator is not overloaded for the given type", pos, Expression.Error);
                }
            }
        }

        #endregion

        public CFGNode CreateControlFlowGraph(IStatement root, Func<IStatement, bool> isTarget) {
            return new CFGCreator(Type.Top, new List<IStatement>(), err: new ErrorBuffer()).CreateCFG(root, new CFGNode(false), isTarget).start;
        }

        public CFGNode CreateControlFlowGraph(IStatement root, IType expectedReturnType, ICollection<IStatement> unreachableStatements, Func<IStatement, bool> isTarget, ErrorBuffer err = null) {
            return new CFGCreator(expectedReturnType ?? Type.Top, unreachableStatements ?? new List<IStatement>(), err: err).CreateCFG(root, new CFGNode(false), isTarget).start;
        }
        public CFGNode CreateControlFlowGraph(IStatement root, IType expectedReturnType, ICollection<IStatement> unreachableStatements, Func<IStatement, bool> isTarget, RelativeStatementPosition reportStmts, ErrorBuffer err = null) {
            var creator = new CFGCreator(expectedReturnType ?? Type.Top, unreachableStatements ?? new List<IStatement>(), reportStmts, err);
            var ret = creator.CreateCFG(root, new CFGNode(false), isTarget).start;
            return ret;
        }

        public virtual bool IsIteratorMethodName(string name) {
            return name == "tryGetNext" || name == "moveNext" || name == "next";
        }
        public virtual bool IsIterableMethodName(string name) {
            return name == "getIterator" || name == "iterator";
        }
        public virtual bool IsIterator(IType tp, IType over) {
            return tp.Context.InstanceContext.Methods.Values.Any(x => IsIteratorMethodName(x.Signature.Name) && x.ReturnType == PrimitiveType.Bool && x.Arguments.All(y => y.Type.IsByRef() && !y.IsFinal()));
        }
        public virtual bool IsIterator(IType tp, IType over, out IMethod tryGetNext) {
            tryGetNext = tp.Context.InstanceContext.Methods.Values.FirstOrDefault(x => IsIteratorMethodName(x.Signature.Name) && x.ReturnType == PrimitiveType.Bool && x.Arguments.Any() && x.Arguments.All(y => y.Type.IsByRef() && !y.IsFinal()) && over.IsSubTypeOf(x.Arguments.First().Type.UnWrap()));
            return tryGetNext != null;
        }
        public virtual bool IsIterable(IType tp, IType over) {
            return tp.Context.InstanceContext.Methods.Values.Any(x => IsIterableMethodName(x.Signature.Name) && IsIterator(x.ReturnType, over));
        }
        public virtual bool IsIterable(IType tp, IType over, out IMethod getIterator, out IMethod tryGetNext) {
            foreach (var x in tp.Context.InstanceContext.Methods.Values) {
                if (IsIterableMethodName(x.Signature.Name) && IsIterator(x.ReturnType, over, out tryGetNext)) {
                    getIterator = x;
                    return true;
                }
            }
            getIterator = null;
            tryGetNext = null;
            return false;
        }

        public virtual bool CanBePassedAsParameterTo(IType actualTy, IType formalTy, out int diff) => Type.IsAssignable(actualTy, formalTy, out diff);
        public virtual bool IsTriviallyIterable(IType tp, IType over) {
            if (tp.IsArray() || tp.IsArraySlice() || tp.IsVarArg()) {
                return over == tp.Cast<AggregateType>().ItemType;
            }
            return false;
        }
    }
}
