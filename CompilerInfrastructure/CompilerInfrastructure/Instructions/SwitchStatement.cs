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
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    using Type = Structure.Types.Type;

    [Serializable]
    public class SwitchStatement : StatementImpl {
        public interface IPattern : IExpressionContainer, IStatementContainer {
            bool TryReplaceMacroParameters(MacroCallParameters args, out IPattern ret);
            IPattern Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
            IType BaseType { get; }
            bool TypecheckNecessary { get; }
        }
        [Serializable]
        public class Wildcard : IPattern {
            public IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
            public IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
            public IPattern Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
            public bool TryReplaceMacroParameters(MacroCallParameters args, out IPattern ret) { ret = this; return false; }
            public Wildcard() { }
            public IType BaseType { get; set; } = Type.Bot;

            public bool TypecheckNecessary => false;
        }
        [Serializable]
        public class RecursivePattern : ISourceElement, IPattern {
            public Position Position { get; }
            public IType TypeConstraint { get; set; }
            public IPattern[] SubPatterns { get; }
            public IType BaseType {
                get {
                    return TypeConstraint;
                }
                set {
                    TypeConstraint = value;
                }
            }

            public IMethod Deconstructor { get; set; }
            public bool TypecheckNecessary { get; set; }

            public RecursivePattern(Position pos, IType tp, IEnumerable<IPattern> sub) {
                Position = pos;
                TypeConstraint = tp ?? Type.Bot;
                SubPatterns = sub?.ToArray() ?? Array.Empty<IPattern>();
                TypecheckNecessary = !TypeConstraint.IsTop();
            }

            public IEnumerable<IExpression> GetExpressions() {
                foreach (var sub in SubPatterns) {
                    if (sub is IExpression ex)
                        yield return ex;
                    else {
                        foreach (var e in sub.GetExpressions())
                            yield return e;
                    }
                }
            }
            public IEnumerable<IStatement> GetStatements() {
                foreach (var sub in SubPatterns) {
                    if (sub is IStatement stmt)
                        yield return stmt;
                    else {
                        foreach (var st in sub.GetStatements())
                            yield return st;
                    }
                }
            }

            public bool TryReplaceMacroParameters(MacroCallParameters args, out IPattern ret) {
                bool changed = false;
                var nwSub = SubPatterns.Select(x => {
                    if (x.TryReplaceMacroParameters(args, out var nwx)) {
                        changed = true;
                        return nwx;
                    }
                    return x;
                }).ToArray();
                ret = changed ? new RecursivePattern(Position, TypeConstraint, nwSub) : (this);
                return changed;
            }

            public IPattern Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
                var nwSub = SubPatterns.Select(x => x.Replace(genericActualParameter, curr, parent));
                return new RecursivePattern(Position, TypeConstraint, nwSub);
            }
        }
        [Serializable]
        public class Case : ISourceElement, IStatementContainer, IExpressionContainer {
            readonly IStatement[] arrOnMatch;
            public Case(Position pos, IStatement onMatch, IPattern[] alternatives) {
                arrOnMatch = new[] { onMatch ?? "The body of a case in a switch-statement must not be null".Report(pos, Statement.Error) };

                Patterns = alternatives ?? Array.Empty<IPattern>();
                Position = pos;
            }

            public IPattern[] Patterns {
                get;
            }


            public IStatement OnMatch {
                get => arrOnMatch[0];
            }
            public virtual bool IsDefault => false;

            public Position Position {
                get;
            }

            /*public IRefEnumerator<IASTNode> GetEnumerator() => throw new NotImplementedException();
            IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();*/

            public IEnumerable<IStatement> GetStatements() {
                return Patterns.SelectMany(x => x.GetStatements()).Concat(arrOnMatch);
            }

            public IEnumerable<IExpression> GetExpressions() => Patterns.SelectMany(x => x.GetExpressions());
            public virtual bool TryReplaceMacroParameters(MacroCallParameters args, out Case ret) {
                bool changed = false;

                var nwPat = Patterns.Select(x => {
                    if (x.TryReplaceMacroParameters(args, out var nwX)) {
                        changed = true;
                        return nwX;
                    }
                    return x;
                }).ToArray();
                if (OnMatch.TryReplaceMacroParameters(args, out var nwBody))
                    changed = true;
                else
                    nwBody = OnMatch;

                if (changed) {
                    ret = new Case(Position.Concat(args.Position),
                        nwBody,
                        nwPat
                    );
                    return true;
                }

                ret = this;
                return false;
            }
        }
        [Serializable]
        public class DefaultCase : Case {
            public DefaultCase(Position pos, IStatement onMatch) : base(pos, onMatch, null) {
            }

            public sealed override bool IsDefault => true;
            public override bool TryReplaceMacroParameters(MacroCallParameters args, out Case ret) {
                if (OnMatch.TryReplaceMacroParameters(args, out var nwOnMatch)) {
                    ret = new DefaultCase(Position.Concat(args.Position), nwOnMatch);
                    return true;
                }
                ret = this;
                return false;
            }
        }

        Case[] cases;
        Lazy<bool> exhaustive;
        public SwitchStatement(Position pos, IExpression cond, params Case[] cases) : base(pos) {
            Condition = cond;
            this.cases = cases ?? Array.Empty<Case>();
            exhaustive = new Lazy<bool>(IsExhaustiveInternal);
        }
        public Case[] Cases {
            get => cases;
            set => cases = value ?? Array.Empty<Case>();
        }
        public IExpression Condition {
            get;
        }
        bool IsExhaustiveInternal() {
            if (cases.OfType<DefaultCase>().Any())
                return true;
            if (Condition.ReturnType.IsPrimitive() && Condition.ReturnType.TryCast<PrimitiveType>(out var prim)) {
                if (prim.PrimitiveName == PrimitiveName.Bool)
                    return cases.Sum(x => x.Patterns.Length) >= 2;
                else if (prim.PrimitiveName == PrimitiveName.Char || prim.PrimitiveName == PrimitiveName.Byte)
                    return cases.Sum(x => x.Patterns.Length) >= 256;
            }
            else if (Condition.ReturnType.IsEnum() & Condition.ReturnType.TryCast<EnumType>(out var enumTy)) {
                return cases.Sum(x => x.Patterns.Length) >= enumTy.EnumItems.Count;
            }

            return false;
        }
        public bool IsExhaustive() => exhaustive.Value;

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(Cases);
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new SwitchStatement(Position,
                Condition.Replace(genericActualParameter, curr, parent),
                Cases.Select(x => {
                    if (x is DefaultCase dflt) {
                        return new DefaultCase(dflt.Position, dflt.OnMatch.Replace(genericActualParameter, curr, parent));
                    }
                    return new Case(x.Position,
                        x.OnMatch.Replace(genericActualParameter, curr, parent),
                        x.Patterns.Select(y => y.Replace(genericActualParameter, curr, parent)).ToArray()
                    );
                }).ToArray());
        }

        public override IEnumerable<IStatement> GetStatements() {
            return cases.SelectMany(x => x.GetStatements());
        }
        public override IEnumerable<IExpression> GetExpressions() {
            return cases.SelectMany(x => x.GetExpressions()).Prepend(Condition);
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Condition.TryReplaceMacroParameters(args, out var nwCond)) {
                changed = true;
                if (nwCond.Length != 1) {
                    "A switch-statement cannot switch over a variable number of arguments".Report(Condition.Position.Concat(args.Position));
                    nwCond = new[] { Condition };
                }
            }
            else
                nwCond = new[] { Condition };

            var nwCases = cases.Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwCas)) {
                    changed = true;
                    return nwCas;
                }
                return x;
            }).ToArray();

            if (changed) {
                stmt = new SwitchStatement(Position.Concat(args.Position), nwCond[0], nwCases);
                return true;
            }
            stmt = this;
            return false;
        }
    }
}
