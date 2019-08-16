using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DefiningRules = CompilerInfrastructure.Context.DefiningRules;
namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class SpanType : AggregateType {
        [Serializable]
        public sealed class SpanContext : BasicContext {
            [Serializable]
            public sealed class SpanLength : VariableImpl {
                private SpanLength() : base(default, null) {
                    Signature = new Variable.Signature("length", PrimitiveType.UInt.Signature);
                }
                public override Variable.Signature Signature {
                    get;
                }
                public override Visibility Visibility {
                    get;
                } = Visibility.Public;

                public static SpanLength Instance {
                    get;
                } = new SpanLength();
                public override Variable.Specifier VariableSpecifiers {
                    get;
                } = Variable.Specifier.Final;
                public override IType Type {
                    get => PrimitiveType.SizeT;
                }

                protected override IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
                public override bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr) { vr = this; return false; }
            }
            [Serializable]
            class SpanIndex : PrimitiveOperatorOverload {
                readonly IType elemTp;
                //InstructionBox mBody;
                public SpanIndex(IType elemType) : base(OverloadableOperator.Indexer) {
                    elemTp = elemType;
                    Signature = new Method.Signature("operator[]", elemType.AsByRef(), PrimitiveType.SizeT);
                    Context = SimpleMethodContext.GetImmutable(elemTp.Context.Module);
                    Arguments = new[] { new BasicVariable(default, PrimitiveType.SizeT, Variable.Specifier.LocalVariable, "index", null) };
                }

                public override IType ReturnType {
                    get => elemTp;
                }
                public override Method.Signature Signature {
                    get;
                }
                public override SimpleMethodContext Context {
                    get;
                }
                public override InstructionBox Body {
                    get; set;
                }

                public override IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
                    var nwEl = elemTp.Replace(genericActualParameter, curr, parent);
                    if (nwEl != elemTp) {
                        return new SpanIndex(nwEl);//Don't replace context since it is always empty here
                    }
                    return this;
                }
            }
            [Serializable]
            class SpanRange : PrimitiveOperatorOverload {
                readonly SpanType span;
                //InstructionBox mBody;
                public SpanRange(SpanType span) : base(OverloadableOperator.RangedIndexer) {
                    this.span = span;
                    Signature = new Method.Signature($"operator{OverloadableOperator.RangedIndexer.OperatorName()}", span, PrimitiveType.UInt, PrimitiveType.UInt);
                    // span.Context is not yet initialized, so use span.ItemType.Context
                    Context = SimpleMethodContext.GetImmutable(span.ItemType.Context.Module);
                    Arguments = new[] {
                        new BasicVariable(default,PrimitiveType.UInt,Variable.Specifier.LocalVariable,"offset",null){ DefaultValue=Literal.UInt(0)},
                        new BasicVariable(default,PrimitiveType.UInt,Variable.Specifier.LocalVariable,"length",null){ DefaultValue=Literal.UInt(uint.MaxValue)}
                    };
                }
                public override IType ReturnType {
                    get => span;
                }
                public override Method.Signature Signature {
                    get;
                }
                public override SimpleMethodContext Context {
                    get;
                }
                public override InstructionBox Body {
                    get; set;
                }

                public override IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
                    var elemType = span.ItemType;
                    var nwElem = elemType.Replace(genericActualParameter, curr, parent);
                    if (nwElem != elemType) {
                        return new SpanRange(spanTp[nwElem]);
                    }
                    return this;
                }
            }
            public SpanContext(SpanType span) : base(span.ItemType.Context.Module, DefiningRules.None, true) {
                variables.Add(SpanLength.Instance.Signature, SpanLength.Instance);

                var arrind = new SpanIndex(span.ItemType);
                methods.Add(arrind.Signature, arrind);
                var arrrange = new SpanRange(span);
                methods.Add(arrrange.Signature, arrrange);
            }

        }
        static readonly LazyDictionary<IType, SpanType> spanTp = new LazyDictionary<IType, SpanType>(x => new SpanType(x));
        private SpanType(IType underlying)
            : this(underlying, "*") {
        }
        private protected SpanType(IType underlying, string customSuffix) {
            ItemType = underlying ?? "Unknown Element-Type for Span".ReportTypeError();
            Signature = new Type.Signature(ItemType.Signature.Name + customSuffix, null);
            Context = new SimpleTypeContext(
                ItemType.Context.Module,
                DefiningRules.None,
                new SpanContext(this),
                new BasicContext(ItemType.Context.Module, DefiningRules.None, true)
            );
        }

        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context {
            get;
        }
        public override Type.Specifier TypeSpecifiers {
            get;
        } = Type.Specifier.ArraySlice;
        public override Visibility Visibility {
            get => ItemType.Visibility;
        }
        public override Position Position {
            get;
        } = default;
        public override IType ItemType {
            get;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();

        public override bool IsNestedIn(IType other) => false;
        public override bool IsSubTypeOf(IType other) {
            return IsSubTypeOf(other, out _);
        }

        public override bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            if (other == this)
                return true;
            if (other.IsArraySlice()) {
                var agg = other.Cast<AggregateType>();
                if (agg.ItemType == ItemType)
                    return true;
                if (this.IsImmutable() && other.IsImmutable() && ItemType.IsSubTypeOf(agg.ItemType, out difference))
                    return true;
            }
            return false;
        }
        public static SpanType Get(IType elem) {
            return spanTp[elem];
        }

        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var elemTp = ItemType.Replace(genericActualParameter, curr, parent);

            return spanTp[elemTp];
        }
        public override string ToString() => this.PrintString();
        public override void PrintPrefix(TextWriter tw) {
            ItemType.PrintTo(tw);
        }
        public override void PrintValue(TextWriter tw) {
        }
        public override void PrintSuffix(TextWriter tw) {
            tw.Write("*");
        }
    }
}
