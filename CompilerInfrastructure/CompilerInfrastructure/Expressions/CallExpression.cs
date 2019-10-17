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
using System.Text;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class CallExpression : ExpressionImpl, IEphemeralExpression, ISideEffectFulExpression {
        readonly IExpression[] content;
        public CallExpression(Position pos, IType retTy, IMethod met, IExpression parent, ICollection<IExpression> args) : base(pos) {
            Callee = met ?? "A method which is called must not be null".Report(pos, Method.Error);
            ReturnType = retTy ?? met?.ReturnType ?? PrimitiveType.Void;
            content = new IExpression[1 + (args?.Count ?? 0)];
            content[0] = parent;
            if (args != null)
                args.CopyTo(content, 1);
            IsEphemeral = EphemeralExpression.IsAnyEphemeralOrVariable(args.Append(parent));
            CalleeName = Callee.Signature.Name;
        }
        public CallExpression(Position pos, string calleeName, IEnumerable<IDeclaredMethod> possibleCallees, IExpression parent, ICollection<IExpression> args, IType expectedReturnType) : base(pos) {
            content = new IExpression[1 + (args?.Count ?? 0)];
            content[0] = parent;
            if (args != null)
                args.CopyTo(content, 1);
            IsEphemeral = true;
            PossibleCallees = possibleCallees ?? Enumerable.Empty<IMethod>();
            if (possibleCallees != null && possibleCallees.Any()) {
                if (!possibleCallees.HasCount(2)) {
                    Callee = possibleCallees.OfType<IMethod>().FirstOrDefault();
                    IsEphemeral = Callee != null;
                }
            }
            else {
                if (parent == null) {
                    // wird (hoffentlich) nicht auftreten
                    "The compiler cannot infer the callee for a ephemeral call without specified callee-candidates".Report(pos);
                }
            }
            CalleeName = calleeName ?? throw new ArgumentNullException(nameof(calleeName));
            ReturnType = expectedReturnType ?? Type.Top;
        }
        public string CalleeName {
            get;
        }
        public IEnumerable<IDeclaredMethod> PossibleCallees {
            get;
        }
        public bool IsEphemeral {
            get;
        }
        public bool IsCallVirt {
            get; set;
        } = false;
        public IExpression ParentExpression => content[0];
        public Span<IExpression> Arguments {
            get => new Span<IExpression>(content, 1, content.Length - 1);
        }
        public IMethod Callee {
            get;
        }
        public override IType ReturnType {
            get;
        }
        public bool MayHaveSideEffects {
            get => Callee.MayHaveSideEffects(out _);
        }
        public IEnumerable<(IExpression, IVariable)> ActualToFormalParameters() {
            if (!Callee.Arguments.Any())
                yield break;
            int i;
            for (i = 0; i < Callee.Arguments.Length - 1; ++i) {
                yield return (Arguments[i], Callee.Arguments[i]);
            }
            if (Callee.IsVariadic()) {
                var vararg = Callee.Arguments[i];
                for (; i < Arguments.Length; ++i) {
                    yield return (Arguments[i], vararg);
                }
            }
            else {
                yield return (Arguments[i], Callee.Arguments[i]);
            }
        }
        public IDictionary<IExpression, IVariable> MapActualToFormalParameters() {
            var ret = new Dictionary<IExpression, IVariable>();
            if (!Callee.Arguments.Any())
                //yield break;
                return ret;
            int i;
            for (i = 0; i < Callee.Arguments.Length - 1; ++i) {
                //yield return (Arguments[i], Callee.Arguments[i]);
                ret[Arguments[i]] = Callee.Arguments[i];
            }
            if (Callee.IsVariadic()) {
                var vararg = Callee.Arguments[i];
                for (; i < Arguments.Length; ++i) {
                    //yield return (Arguments[i], vararg);
                    ret[Arguments[i]] = vararg;
                }
            }
            else {
                //yield return (Arguments[i], Callee.Arguments[i]);
                ret[Arguments[i]] = Callee.Arguments[i];
            }
            return ret;
        }
        public MultiMap<IVariable, IExpression> MapFormalToActualParameters() {
            var ret = new MultiMap<IVariable, IExpression>();
            if (!Callee.Arguments.Any())
                return ret;
            int i;
            for (i = 0; i < Callee.Arguments.Length - 1; ++i) {
                ret.Add(Callee.Arguments[i], Arguments[i]);
            }
            if (Callee.IsVariadic()) {
                var vararg = Callee.Arguments[i];
                for (; i < Arguments.Length; ++i) {
                    ret.Add(vararg, Arguments[i]);
                }
            }
            else {
                ret.Add(Callee.Arguments[i], Arguments[i]);
            }
            return ret;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(content);
        public override IEnumerable<IExpression> GetExpressions() => content[0] is null ? content.Skip(1) : content;

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (PossibleCallees is null) {
                return new CallExpression(Position,
                    ReturnType.Replace(genericActualParameter, curr, parent),
                    Callee.Replace(genericActualParameter, curr, parent),
                    ParentExpression?.Replace(genericActualParameter, curr, parent),
                    content.Skip(1).Select(x => x.Replace(genericActualParameter, curr, parent)).AsCollection(content.Length - 1)
                ) {
                    IsCallVirt = IsCallVirt
                };
            }
            else {
                return new CallExpression(Position,
                    CalleeName,
                    PossibleCallees?.Select(x => {
                        if (x is IMethod m)
                            return m.Replace(genericActualParameter, curr, parent);
                        else if (x is IMethodTemplate<IMethod> tm)
                            return tm.Replace(genericActualParameter, curr, parent);
                        else
                            return x;
                    }),
                    ParentExpression?.Replace(genericActualParameter, curr, parent),
                    content.Skip(1).Select(x => x.Replace(genericActualParameter, curr, parent)).AsCollection(content.Length - 1),
                    ReturnType.Replace(genericActualParameter, curr, parent)
                ) {
                    IsCallVirt = IsCallVirt
                };
            }
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            if (IsEphemeral) {
                bool changed = false;

                if (ParentExpression != null && ParentExpression.TryReplaceMacroParameters(args, out var nwPar)) {
                    if (nwPar.Length != 1) {
                        "A method cannot be called on multiple expressions".Report(Position.Concat(args.Position));
                        nwPar = new[] { ParentExpression };
                    }
                    changed = true;
                }
                else {
                    nwPar = new[] { ParentExpression };
                }
                var nwArgs = content.Skip(1).SelectMany(x => {
                    if (x.TryReplaceMacroParameters(args, out var nwArg)) {
                        changed = true;
                        return nwArg;
                    }
                    return new[] { x };
                }).ToArray();
                if (changed) {
                    IMethod callee;
                    if (Callee is null) {
                        var mets = PossibleCallees != null && PossibleCallees.Any() ? PossibleCallees : nwPar[0]?.ReturnType?.Context?.InstanceContext?.MethodsByName(CalleeName);
                        callee = args.Semantics.BestFittingMethod(Position.Concat(args.Position), mets, nwArgs.Select(x => x.ReturnType).AsCollection(nwArgs.Length), ReturnType);
                        if (callee.IsError())
                            "The callee cannot be resolved".Report(Position.Concat(args.Position));
                    }
                    else
                        callee = Callee;
                    expr = new[] { new CallExpression(Position.Concat(args.Position), callee.ReturnType, callee, nwPar[0], nwArgs) {
                        IsCallVirt = IsCallVirt || args.Semantics.IsCallVirt(callee, nwPar[0])
                    } };

                    return true;
                }
            }
            expr = new[] { this };
            return false;
        }
    }
}
