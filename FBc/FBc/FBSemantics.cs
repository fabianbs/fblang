/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    class FBSemantics : BasicSemantics {
        static FBSemantics instance = null;
        public override bool IsFunctional(IType tp, out FunctionType fnTy) {
            if (!base.IsFunctional(tp, out fnTy)) {
                if (tp.IsInterface()
                    && tp.OverloadsOperator(OverloadableOperator.FunctionCall, out var ov, SimpleMethodContext.VisibleMembers.Instance)
                    && ov.HasCount(1)) {
                    var fnCallOp = ov.First();
                    fnTy = new FunctionType(fnCallOp.Position, tp.Context.Module, tp.Signature.Name, fnCallOp.ReturnType, fnCallOp.Arguments.Select(x => x.Type).AsCollection(fnCallOp.Arguments.Length), Visibility.Public) {
                        NestedIn = tp.NestedIn
                    };
                    return true;
                }
            }
            else {
                return true;
            }
            fnTy = default;
            return false;
        }
        public static FBSemantics Instance {
            get {
                if (instance is null) {
                    instance = new FBSemantics();
                }
                return instance;
            }
        }
        public IReadOnlyCollection<IVariable> GetCaptures(LambdaExpression lambda, out IType captureThis, out IType captureBase, bool includeStaticCaptures = false) {
            if (lambda is null)
                throw new ArgumentNullException(nameof(lambda));
            captureBase = captureThis = null;
            if (!lambda.Body.HasValue) {
                return Array.Empty<IVariable>();
            }
            var body = lambda.Body.Instruction;
            //var args = lambda.Arguments/*.ToHashSet()*/;
            var ret = new HashSet<IVariable>();

            /*void GetCapturesInternal(IASTNode nod) {
                if (nod is VariableAccessExpression vra) {
                    if (!lambda.Context.LocalContext.Variables.ContainsKey(vra.Variable.Signature)) {
                        if (includeStaticCaptures || vra.Variable.IsLocalVariable() || vra.ParentExpression != null)
                            ret.Add(vra.Variable);
                    }
                }
                else {
                    foreach (var subNod in nod) {
                        GetCapturesInternal(subNod);
                    }
                }
            }*/
            // get captures
            void GetCapturesInternal(IExpression nod, out IType capThis, out IType capBase) {
                capThis = capBase = null;
                if (nod is VariableAccessExpression vra) {

                    if (!lambda.Context.LocalContext.Variables.ContainsKey(vra.Variable.Signature)) {
                        if (includeStaticCaptures || vra.Variable.IsLocalVariable() || vra.ParentExpression != null)
                            ret.Add(vra.Variable);
                    }
                }
                else if (nod is ThisExpression) {
                    capThis = nod.ReturnType;
                }
                else if (nod is BaseExpression) {
                    capBase = nod.ReturnType;
                }
                else {
                    foreach (var subNod in nod.GetExpressions()) {
                        GetCapturesInternal(subNod, out var scapThis, out var scapBase);
                        if (scapThis != null)
                            capThis = scapThis;
                        if (scapBase != null)
                            capBase = scapBase;
                    }
                }
            }
            foreach (var ex in body.GetExpressionFromStatements()) {
                GetCapturesInternal(ex, out var scapThis, out var scapBase);
                if (scapThis != null)
                    captureThis = scapThis;
                if (scapBase != null)
                    captureBase = scapBase;
            }
            //GetCapturesInternal(body);
            return //ret.AsReadOnly();
                ret;
        }
        public bool ContainVariable(IVariable vr, IEnumerable<IContext> ctxs) {
            //DOLATER Geht das auch effizienter?
            return ctxs.Any(x => x.LocalContext.Variables.ContainsKey(vr.Signature));
        }
        public override bool IsCallVirt(IMethod met, IExpression par) {
            if (par is BaseCaptureExpression)
                return false;
            return base.IsCallVirt(met, par);
        }
        bool ValidateRec(SwitchStatement.RecursivePattern rec, IType switchTy) {
            if (rec.BaseType.IsError())
                return true;
            else if (!rec.BaseType.IsTopOrBot() && !Type.IsAssignable(rec.BaseType, switchTy))
                return false;

            var tp = rec.BaseType = rec.BaseType.IsTopOrBot() ? switchTy : rec.BaseType;
            if (!tp.IsTopOrBot()) {
                if (tp.OverloadsOperator(OverloadableOperator.Deconstruct, out var dec)) {
                    rec.Deconstructor = BestFittingMethod(rec.Position, dec, rec.SubPatterns.Select(x => x.BaseType).AsCollection(rec.SubPatterns.Length), PrimitiveType.Void);
                    if (rec.Deconstructor.IsError())
                        return false;
                }
                else {
                    return $"The type {rec.BaseType} cannot occur as base-type in a recursive pattern, since it is not deconstructible. To make it deconstructible, overload the operator<-".Report(rec.Position, false);
                }
            }
            else {
                $"Cannot infer the type of the recursive pattern. Please specify the type explicitly".Report(rec.Position);
            }

            bool succ = true;
            //assert rec.deconstructor is method and not error
            for (int i = 0; i < rec.SubPatterns.Length; ++i) {
                var subTy = ArgumentType(rec.Deconstructor, out var lv, out var vr, (uint) i);
                if (lv != ReferenceKind.MutableLValue)
                    "This deconstructor cannot be applied, since its parameters are not all passed by mutable reference".Report(vr.Position);
                succ &= Validate(rec.SubPatterns[i], subTy);
            }
            return succ;
        }
        public bool Validate(SwitchStatement.IPattern pat, IType switchTy) {
            if (pat is SwitchStatement.RecursivePattern rec) {
                return ValidateRec(rec, switchTy);
            }
            else {
                if (pat is DeclarationExpression sdecl && sdecl.ReturnType.IsBot())
                    (sdecl.Variable as BasicVariable).Type = switchTy;
                else if (pat is SwitchStatement.Wildcard wc)
                    wc.BaseType = switchTy;
                else if (pat is ILiteral lit && (lit.ReturnType.IsFloatingPointType() || switchTy.IsFloatingPointType())) {
                    return "Floating-point numbers cannot be matched against literals due to rounding imprecision".Report(lit.Position, false);
                }
                return Type.IsAssignable(pat.BaseType, switchTy);
            }
        }
        public bool ValidateAll((SwitchStatement.IPattern, IContext)[] pat, IType switchTy, out ISet<IVariable> definedVars) {
            definedVars = Set.Empty<IVariable>();
            if (pat is null)
                return false;
            if (!pat.All(x => Validate(x.Item1, switchTy)))
                return false;

            if (pat.Length > 0) {
                var comp = new FunctionalEquiComparer<IVariable>((x, y) => x.Signature == y.Signature, x => x.Signature.GetHashCode());
                definedVars = pat[0].Item2.LocalVariables().ToHashSet(comp);
                for (int i = 1; i < pat.Length; ++i) {
                    definedVars.IntersectWith(pat[i].Item2.LocalVariables());
                }
            }
            return true;
        }

        public override IExpression CreateCall(Position pos, IType retTy, IType parentTy, IExpression parent, string name, ICollection<IExpression> args, ErrorBuffer err = null) {
            var ret = base.CreateCall(pos, retTy, parentTy, parent, name, args, err);
            if (ret is CallExpression ce && IsVariadicUnpackDistribution(ce.Callee, args, out var nwArgs)) {
                return new CallExpression(ce.Position, ce.ReturnType, ce.Callee, ce.ParentExpression, nwArgs) { IsCallVirt = ce.IsCallVirt };
            }
            return ret;
        }
        bool IsVariadicUnpackDistribution(IMethod callee, ICollection<IExpression> args, out IExpression[] expandedArgs) {
            if (callee.IsVariadic() && callee is BasicMethod met) {

                using (var it = args.GetEnumerator()) {
                    var nwArgs = Vector<IExpression>.Reserve(met.Arguments.Length);
                    bool distributeUnpack = false;
                    IExpression range = null;
                    int i;
                    for (i = 0; i < met.Arguments.Length && it.MoveNext(); ++i) {
                        if (it.Current.IsUnpack(out range)) {
                            if (!met.Arguments[i].Type.IsVarArg()) {
                                distributeUnpack = true;
                                break;
                            }
                        }

                        nwArgs.Add(it.Current);

                    }
                    if (distributeUnpack) {
                        uint j;
                        for (j = 0; i < met.Arguments.Length - 1; ++i, ++j) {
                            nwArgs.Add(new IndexerExpression(it.Current.Position, met.Arguments[i].Type, range, new[] { Literal.UInt(j) }));
                        }

                        
                        nwArgs.Add(new UnOp(it.Current.Position,
                            null,
                            UnOp.OperatorKind.UNPACK,
                            new RangedIndexerExpression(it.Current.Position,
                                null,
                                range,
                                Literal.UInt(j))));

                        while (it.MoveNext()) {
                            nwArgs.Add(it.Current);
                        }
                        expandedArgs = nwArgs.AsArray();

                        return true;
                    }
                }
            }
            expandedArgs = null;
            return false;
        }
        public CallExpression CreateCall(Position pos, IType retTy, IMethod met, IExpression parent, ICollection<IExpression> _args, bool isCallVirt = false) {
            if (!IsVariadicUnpackDistribution(met, _args, out var args))
                args = _args.ToArray();
            return new CallExpression(pos, retTy, met, parent, args) { IsCallVirt = false };
        }
    }

}

