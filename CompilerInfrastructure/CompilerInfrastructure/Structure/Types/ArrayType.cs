using CompilerInfrastructure.Contexts;
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
    public class ArrayType : AggregateType {
        [Serializable]
        public class ArrayContext : BasicContext {
            [Serializable]
            public class ArrayLength : VariableImpl {
                private ArrayLength() : base(default, null) {
                    Signature = new Variable.Signature("length", PrimitiveType.UInt.Signature);
                }
                public override Variable.Signature Signature {
                    get;
                }
                public override Visibility Visibility {
                    get;
                } = Visibility.Public;

                public static ArrayLength Instance {
                    get;
                } = new ArrayLength();
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
            public class ArrayIndex : PrimitiveOperatorOverload {
                readonly IType elemTp;
                //InstructionBox mBody;
                public ArrayIndex(IType elemType) : base(OverloadableOperator.Indexer) {
                    Signature = new Method.Signature("operator[]", elemType.AsByRef(), PrimitiveType.SizeT);
                    Context = SimpleMethodContext.GetImmutable(elemType.Context.Module);
                    elemTp = elemType;
                    Arguments = new[] { new BasicVariable(default, PrimitiveType.SizeT, Variable.Specifier.LocalVariable, "index", null) };
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
                public override IType ReturnType {
                    get => elemTp;
                }

                public override IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
                    var nwElem = elemTp.Replace(genericActualParameter, curr, parent);
                    if (nwElem != elemTp) {
                        return new ArrayIndex(nwElem);
                    }
                    return this;
                }
            }
            public ArrayContext(IType elemType) : base(elemType.Context.Module, DefiningRules.None, true) {
                variables.Add(ArrayLength.Instance.Signature, ArrayLength.Instance);

                var arrind = new ArrayIndex(elemType);
                methods.Add(arrind.Signature, arrind);
            }

        }
        static readonly LazyDictionary<IType, ArrayType> arrTps = new LazyDictionary<IType, ArrayType>(x => new ArrayType(x));
        readonly IType underlying;

        private ArrayType(IType underlying)
            : this(underlying, "[]") {
        }
        private protected ArrayType(IType underlying, string customSuffix) {
            this.underlying = underlying ?? "Unknown Element-type for Array".ReportTypeError();
            Signature = new Type.Signature(this.underlying.Signature.Name + customSuffix, null);
            Context = new SimpleTypeContext(
                this.underlying.Context.Module,
                DefiningRules.None,
                new ArrayContext(this.underlying),
                new BasicContext(this.underlying.Context.Module, DefiningRules.None, true)
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
        } = Type.Specifier.Array;
        public override Visibility Visibility {
            get => underlying.Visibility;
        }
        public override Position Position {
            get;
        } = default;
        public override IType ItemType {
            get => underlying;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();

        public override bool IsNestedIn(IType other) => false;
        public override bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            if (other.IsArray() || other.IsArraySlice()) {
                if (other.Cast<AggregateType>().ItemType == ItemType || other.IsError())
                    return true;
                if (this.IsImmutable() && other.IsImmutable() && other.TryCast<AggregateType>(out var agg)) {
                    return ItemType.IsSubTypeOf(agg.ItemType, out difference);
                }
            }
            return false;
        }
        public override bool IsSubTypeOf(IType other) => IsSubTypeOf(other, out _);

        public static IType Get(IType elemTp) {
            return elemTp.IsError() ? Type.Error : arrTps[elemTp];
        }


        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var nwElem = ItemType.Replace(genericActualParameter, curr, parent);
            return arrTps[nwElem];
        }

        public override void PrintPrefix(TextWriter tw) {
            ItemType.PrintTo(tw);
        }

        public override void PrintValue(TextWriter tw) {

        }

        public override void PrintSuffix(TextWriter tw) {
            tw.Write("[]");
        }
    }
}
