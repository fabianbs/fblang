/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CompilerInfrastructure.Structure {
    using System.IO;

    /// <summary>
    /// The basic implementation of the <see cref="IMethod"/> interface. For most purposes this is sufficient
    /// </summary>
    [Serializable]
    public class BasicMethod : IMethod, ISerializable {
        Method.Signature mSignature;

        private ValueLazy<string> tos;
        //InstructionBox mBody;
        // besser wäre mit friend class BasicMethodTemplate
        internal readonly Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, BasicMethod> genericCache
              = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, BasicMethod>();
        /// <summary>
        /// Initializes this method with the given properties. Do not forget to assign <see cref="Context"/>, <see cref="NestedIn"/> and <see cref="Arguments"/>
        /// </summary>
        /// <param name="pos">The source-code location, where this method is defined</param>
        /// <param name="name">The name of the method</param>
        /// <param name="vis">The visibility of the method</param>
        /// <param name="retType">The return-type (<c>null</c> will be treated as <c>void</c>)</param>
        /// <param name="argTps">The types of the formal parameters of this method</param>
        public BasicMethod(Position pos, string name, Visibility vis, IType retType, params IType[] argTps)
            : this(pos, name, vis, retType, (IReadOnlyList<IType>) argTps) {
            ReturnType = retType;
        }
        /// <summary>
        /// Initializes this method with the given properties. Do not forget to assign <see cref="Context"/>, <see cref="NestedIn"/> and <see cref="Arguments"/>
        /// </summary>
        /// <param name="pos">The source-code location, where this method is defined</param>
        /// <param name="name">The name of the method</param>
        /// <param name="vis">The visibility of the method</param>
        /// <param name="retType">The return-type (<c>null</c> will be treated as <c>void</c>)</param>
        /// <param name="argTps">The types of the formal parameters of this method</param>
        public BasicMethod(Position pos, string name, Visibility vis, IType retType, IReadOnlyList<IType> argTps) {
            Signature = new Method.Signature(name, retType ?? PrimitiveType.Void, argTps, null);
            Visibility = vis;
            Position = pos;
            ReturnType = retType ?? PrimitiveType.Void;
            tos = new ValueLazy<string>(ToStringInternal);
        }
        /// <summary>
        /// Initializes this method with the given properties. Do not forget to assign <see cref="Context"/> and <see cref="NestedIn"/>
        /// </summary>
        /// <param name="pos">The source-code location, where this method is defined</param>
        /// <param name="name">The name of the method</param>
        /// <param name="vis">The visibility of the method</param>
        /// <param name="retType">The return-type (<c>null</c> will be treated as <c>void</c>)</param>
        /// <param name="args">The the formal parameters of this method</param>
        /// <param name="body">The body of this method</param>
        /// <param name="internalName">The 'mangled' name of this method in the case, that this is an imported C-function</param>
        public BasicMethod(Position pos, string name, Visibility vis, IType retType, IReadOnlyList<IVariable> args, InstructionBox body = null, string internalName = null) {
            Signature = new Method.Signature(name, retType, args.Select(x => x.Type).ToArray(), null, internalName);
            Position = pos;
            Visibility = vis;
            ReturnType = retType ?? PrimitiveType.Void;
            Arguments = args.ToArray();
            if (body != null)
                Body = body;
            tos = new ValueLazy<string>(ToStringInternal);
        }

        protected BasicMethod(SerializationInfo info, StreamingContext context) {
            Context = info.GetT<SimpleMethodContext>(nameof(Context));
            Parent = info.GetT<IContext>(nameof(Parent));
            Overrides = info.GetT<IMethod>(nameof(Overrides));
            Body = info.GetT<InstructionBox>(nameof(Body));
            Visibility = info.GetT<Visibility>(nameof(Visibility));
            Position = info.GetT<Position>(nameof(Position));
            Specifiers = info.GetT<Method.Specifier>(nameof(Specifiers));
            ReturnType = info.GetT<IType>(nameof(ReturnType));
            Arguments = info.GetT<IVariable[]>(nameof(Arguments));
            NestedIn = info.GetT<IContext>(nameof(Arguments));
            Signature = info.GetT<Method.Signature>(nameof(Signature));
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(Context), Context);
            info.AddValue(nameof(Parent), Parent);
            info.AddValue(nameof(Overrides), Overrides);
            if (NestedIn is ITypeContext tcx && tcx.Type != null && tcx.Type.MayRelyOnGenericParameters() || Signature.BaseMethodTemplate != null) {
                info.AddValue(nameof(Body), Body);
            }
            else {
                info.AddValue(nameof(Body), new InstructionBox());
            }
            info.AddValue(nameof(Visibility), Visibility);
            info.AddValue(nameof(Position), Position);
            info.AddValue(nameof(Specifiers), Specifiers);
            info.AddValue(nameof(ReturnType), ReturnType);
            info.AddValue(nameof(Arguments), Arguments);
            info.AddValue(nameof(NestedIn), NestedIn);
            info.AddValue(nameof(Signature), Signature);
        }
        /// <summary>
        /// The signature of this method
        /// </summary>
        public ref Method.Signature Signature => ref mSignature;
        ISignature ISigned.Signature => mSignature;
        Method.Signature ISigned<Method.Signature>.Signature => mSignature;
        IContext IRangeScope.Context => Context;
        /// <summary>
        /// The context of this method
        /// </summary>
        public SimpleMethodContext Context {
            get;
            set;
        }
        /// <summary>
        /// The context, where this method is defined in
        /// </summary>
        public IContext Parent {
            get;
            set;
        } = null;
        /// <summary>
        /// The method, which is overridden by this method
        /// </summary>
        public IMethod Overrides {
            get;
            set;
        } = null;
        public InstructionBox Body {
            get;
        } = new InstructionBox();
        /// <summary>
        /// The visibility capability of this method
        /// </summary>
        public Visibility Visibility {
            get; set;
        }
        /// <summary>
        /// The source-code location, where this method is defined
        /// </summary>
        public Position Position {
            get;
        }
        Method.Specifier specifiers = Method.Specifier.None;
        /// <summary>
        /// The specifiers of this method
        /// </summary>
        public Method.Specifier Specifiers {
            get => specifiers;
            set {
                specifiers = value;
                foreach (var met in genericCache.Values) {
                    met.specifiers = value;
                }
            }
        }
        /// <summary>
        /// The return-type of this method
        /// </summary>
        public IType ReturnType {
            get;
            set;
        } = PrimitiveType.Void;
        /// <summary>
        /// The formal parameters of this method
        /// </summary>
        public IVariable[] Arguments {
            get;
            set;
        } = Array.Empty<IVariable>();
        /// <summary>
        /// The context, where this method is defined in
        /// </summary>
        public IContext NestedIn {
            get; set;
        }
        /// <summary>
        /// The method, which is overridden by this method
        /// </summary>
        public IDeclaredMethod OverrideTarget => Overrides;

        /*IEnumerable<IASTNode> Children() {
            //DOLATER Context?
            if (Context is SimpleMethodContext ctx) {
                foreach (var arg in ctx.Arguments) {
                    yield return arg;
                }
            }
            if (Body.HasValue)
                yield return Body.Instruction;
        }*/
        /*public IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.ConcatMany(Children());
        }
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        /// <summary>
        /// Applies generic parameter substitution to this method returning a new method, where the generics are substituted.
        /// This method does not modify this method-object
        /// </summary>
        public virtual IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                if (Signature.GenericActualArguments.Any()) {
                    var gen = ClassTypeTemplate.FromList(Signature.BaseMethodTemplate.Signature.GenericFormalparameter, Signature.GenericActualArguments)
                        .Then(genericActualParameter, curr, parent);
                    ret = (BasicMethod) Signature.BaseMethodTemplate.BuildMethod(gen);
                    if (genericCache.TryAdd(genericActualParameter, ret))
                        return ret;
                    return genericCache[genericActualParameter];
                    /*if (Signature.GenericActualArguments.Count <= genericActualParameter.Count
                        && Signature.GenericActualArguments.All(x => x is IGenericParameter gen && genericActualParameter.ContainsKey(gen))) {
                        ret = (BasicMethod)Signature.BaseMethodTemplate.BuildMethod(genericActualParameter);
                        if (Signature.GenericActualArguments.Count < genericActualParameter.Count) {
                            var dic = new Dictionary<IGenericParameter, ITypeOrLiteral>(genericActualParameter);
                            foreach (var gen in Signature.GenericActualArguments.OfType<IGenericParameter>()) {
                                dic.Remove(gen);
                            }
                            ret = (BasicMethod)ret.Replace(dic, curr, parent);
                        }
                        if (genericCache.TryAdd(genericActualParameter, ret))
                            return ret;
                        return genericCache[genericActualParameter];
                    }*/
                }
                var nwPar = NestedIn?.Replace(genericActualParameter, curr, parent);
                if (nwPar is ITypeContext tcx && NestedIn is ITypeContext mtcx)
                    tcx.Type = mtcx.Type?.Replace(genericActualParameter, curr, parent);
                /*ret = new BasicMethod(Position, Signature.Name, Visibility,
                    ReturnType.Replace(genericActualParameter, curr, parent),
                    Arguments.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray(),
                    Body?.Replace(genericActualParameter, curr, parent)) {
                    Overrides = Overrides?.Replace(genericActualParameter, curr, parent),
                    NestedIn = nwPar,
                    Specifiers = Specifiers
                };*/
                ret = new BasicMethodReplace(this,
                    ReturnType.Replace(genericActualParameter, curr, parent),
                    Arguments.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray(),
                    Body?.Replace(genericActualParameter, curr, parent),
                    genericActualParameter,
                    Signature.InternalName
                ) {
                    Overrides = Overrides?.Replace(genericActualParameter, curr, parent),
                    NestedIn = nwPar,
                    Specifiers = Specifiers,
                    Context = (SimpleMethodContext) Context?.Replace(genericActualParameter, curr, parent)
                };

                genericCache.TryAdd(genericActualParameter, ret);
                ret.genericCache.TryAdd(genericActualParameter, ret);
                //if (ret.Body != null)
                //    ret.Body = ret.Body.Replace(genericActualParameter, curr, parent);
            }
            return ret;
        }

        string ToStringInternal() {
            var sw = new StringWriter();
            mSignature.PrintPrefix(sw);
            if (NestedIn is ITypeContext tcx && tcx.Type != null) {
                sw.Write(tcx.Type);
                sw.Write("::");
            }
            mSignature.PrintValue(sw);
            mSignature.PrintSuffix(sw);
            return sw.ToString();
        }
        public override string ToString() {
            return tos.Value;
        }

        public virtual bool IsVariadic() => Arguments.Any() && Arguments.Last().Type.IsVarArg();
    }
    [Serializable]
    class BasicMethodReplace : BasicMethod {
        BasicMethod parentMet;

        GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter;

        public BasicMethodReplace(BasicMethod parMet, IType retType, IReadOnlyList<IVariable> args, InstructionBox body, GenericParameterMap<IGenericParameter, ITypeOrLiteral> gen, string internalName)
            : base(parMet.Position, parMet.Signature.Name, parMet.Visibility, retType, args, body, internalName) {
            parentMet = parMet;
            genericActualParameter = gen;
        }

        public override IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var parGen = this.genericActualParameter.Then(genericActualParameter, curr, parent, true);
            if (!parentMet.genericCache.TryGetValue(parGen, out var ret)) {
                ret = (BasicMethod) base.Replace(genericActualParameter, curr, parent);
                (ret as BasicMethodReplace).parentMet = parentMet;
                (ret as BasicMethodReplace).genericActualParameter = parGen;
                if (!parentMet.genericCache.TryAdd(parGen, ret))
                    ret = parentMet.genericCache[parGen];
                return ret;
            }
            return ret;
        }
    }
}
