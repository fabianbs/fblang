/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class ArrayInitializerExpression : ExpressionImpl, ICompileTimeEvaluable {
        readonly IExpression[] args;

        public ArrayInitializerExpression(Position pos, IType retTy, IExpression[] args, bool preferStackallocOnTypeInference = false) : base(pos) {
            this.args = args ?? Array.Empty<IExpression>();
            if (retTy is null) {
                ReturnType = InferredItemType(this.args);
                if (ReturnType.IsError()) {
                    if (!args.Any(x => x.ReturnType.IsError()))
                        "The type of the array-initializer expression cannot be inferred and must be specified explicitly".Report(pos);
                }
                else if (preferStackallocOnTypeInference && args.All(x => !x.ReturnType.IsVarArg()))
                    ReturnType = ReturnType.AsFixedSizedArray((uint) this.args.Length);
                else
                    ReturnType = ReturnType.AsArray();
            }
            else
                ReturnType = retTy;
        }
        public static IType InferredReturnType(Position pos, IReadOnlyCollection<IExpression> args, bool preferStackallocOnTypeInference = false, ErrorBuffer err = null) {
            var retTy = InferredItemType(args);
            if (retTy.IsError()) {
                if (!args.Any(x => x.ReturnType.IsError()))
                    err.Report("The type of the array-initializer expression cannot be inferred and must be specified explicitly", pos);
            }
            else if (preferStackallocOnTypeInference && args.All(x => !x.ReturnType.IsVarArg()))
                retTy = retTy.AsFixedSizedArray((uint) args.Count);
            else
                retTy = retTy.AsArray();
            return retTy;
        }
        public IExpression[] Elements => args;
        public static IType InferredItemType(IEnumerable<IExpression> args) {
            return Type.MostSpecialCommonSuperType(args.Select(x => x.IsUnpack(out IType itemTy) ? itemTy : x.ReturnType));
        }
        public override IType ReturnType {
            get;
        }
        bool? compileTimeEvaluable=null;
        public bool IsCompileTimeEvaluable {
            get {
                if (compileTimeEvaluable is null) {
                    compileTimeEvaluable = Elements.All(x => x.IsCompileTimeEvaluable());
                }
                return compileTimeEvaluable.Value;
            }
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(args);
        public override IEnumerable<IExpression> GetExpressions() => args;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ArrayInitializerExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                args.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray()
            );
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            var nwElems = Elements.SelectMany(x => {
                if (x.TryReplaceMacroParameters(args, out var nwEl)) {
                    changed = true;
                    return nwEl;
                }
                return new[] { x };
            }).ToArray();
            if (changed) {
                expr = new[] { new ArrayInitializerExpression(Position.Concat(args.Position), null, nwElems, ReturnType.IsValueType()) };
                return true;
            }
            expr = new[] { this };
            return false;
        }

        public ILiteral Evaluate(ref EvaluationContext context) {
            var elems = Vector<ILiteral>.Reserve((uint) Elements.Length);
            foreach (var el in Elements) {
                if (el.TryEvaluate(out var res)) {
                    elems.Add(res);
                }
                else {
                    compileTimeEvaluable = false;
                    return null;
                }
            }
            if (ArrayLiteral.TryCreate(Position, elems.AsArray(), ReturnType, out var ret)) {
                compileTimeEvaluable = true;
                return context.AssertFact(this, ret);
            }
            else {
                compileTimeEvaluable = false;
                return null;
            }
        }

    }
    [Serializable]
    public class ArrayLiteral : ArrayInitializerExpression, ILiteral {

        public bool IsPositiveValue { get; }
        public IType MinimalType { get; }
        public IType BaseType { get; }
        public bool TypecheckNecessary { get; }

        private ArrayLiteral(Position pos, IType retTy, IExpression[] args, bool preferStackallocOnTypeInference = false)
            : base(pos, retTy, args, preferStackallocOnTypeInference) {
        }
        public static bool TryCreate(Position pos, IExpression[] ilist, IType retTy, out ArrayLiteral ret) {
            if (ilist.All(x => x is ILiteral)) {
                ret = new ArrayLiteral(pos,
                    retTy,
                    ilist);
                return true;
            }
            ret = null;
            return false;
        }
        public ILiteral this[Position pos, long index] {
            get {
                if (index >= 0 && index < Elements.LongLength) {
                    return Elements[index] as ILiteral;
                }
                else {
                    $"The index {index} is out of the range [0, {Elements.LongLength}) of the array".Report(pos);
                    return null;
                }
            }
        }
        public ILiteral this[Position pos, ulong index] {
            get {
                if (index < (ulong) Elements.LongLength) {
                    return Elements[index] as ILiteral;
                }
                else {
                    $"The index {index} is out of the range [0, {Elements.LongLength}) of the array".Report(pos);
                    return null;
                }
            }
        }
        public ILiteral this[Position pos, BigInteger index] {
            get {
                if (index >= 0 && index < Elements.LongLength) {
                    return Elements[(ulong) index] as ILiteral;
                }
                else {
                    $"The index {index} is out of the range [0, {Elements.LongLength}) of the array".Report(pos);
                    return null;
                }
            }
        }
        public IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public bool TryReplaceMacroParameters(MacroCallParameters args, out SwitchStatement.IPattern ret) => throw new NotImplementedException();
        public bool ValueEquals(ILiteral other) => throw new NotImplementedException();
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent)
            => this;
        SwitchStatement.IPattern SwitchStatement.IPattern.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
    }
}
