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
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure {
    /// <summary>
    /// The basic-implementation of <see cref="IMethodTemplate{T}"/>. For most purposes this should be sufficient.
    /// </summary>
    [Serializable]
    public class BasicMethodTemplate : IMethodTemplate<BasicMethod> {
        readonly Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), BasicMethodTemplate> genericCache
            = new Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), BasicMethodTemplate>();
        readonly LazyDictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, BasicMethod> builtMethods;
        //InstructionBox mBody;
        /// <summary>
        /// Initializes this method-template
        /// </summary>
        /// <param name="pos">The source-code location, where this method-template is defined</param>
        /// <param name="name">The name of this method-template</param>
        /// <param name="vis">The visibility</param>
        /// <param name="retType">The return-type; null will be treated as void</param>
        /// <param name="args">The formal parameters</param>
        /// <param name="genArgs">The generic parameters defined by this method-template</param>
        public BasicMethodTemplate(Position pos, string name, Visibility vis, IType retType, IReadOnlyList<IVariable> args, IReadOnlyList<IGenericParameter> genArgs) {
            Position = pos;
            Visibility = vis;
            Signature = new MethodTemplate.Signature(name, genArgs);
            ReturnType = retType ?? PrimitiveType.Void;
            Arguments = args?.ToArray() ?? Array.Empty<IVariable>();

            builtMethods = new LazyDictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, BasicMethod>(dic => {
                var ctx = (SimpleMethodContext)Context.Replace(dic, Context, Context);
                var argRepl = Array.ConvertAll(Arguments, x => {
                    return x.Replace(dic, Context, ctx);
                });
                var ret = new BasicMethod(Position,
                    Signature.Name,
                    Visibility,
                    ReturnType.Replace(dic, Context, ctx),
                    argRepl,
                    Body.Replace(dic, Context, ctx)
                    ) {
                    Specifiers = Specifiers,
                    Context = ctx
                    //Body = Body.Replace(dic, Context, ctx)
                };
                if (NestedIn is ITypeContext mtcx) {
                    var nwPar = mtcx.Replace(dic, Context, ctx);
                    if (nwPar is ITypeContext tcx) {
                        tcx.Type = mtcx.Type?.Replace(dic, Context, ctx);
                    }
                    ret.NestedIn = nwPar;
                }
                ret.genericCache.TryAdd(dic, ret);
                ret.Signature.BaseMethodTemplate = this;
                // Dictionary preserves insertion-order
                ret.Signature.GenericActualArguments = EquatableCollection.FromIList(dic.ToValueList());
                return ret;
            });
        }
        /// <summary>
        /// The source-code location, where this method-template is defined
        /// </summary>
        public Position Position {
            get;
        }
        /// <summary>
        /// The visibility-capability of this method-template
        /// </summary>
        public Visibility Visibility {
            get; set;
        }
        /// <summary>
        /// The signature of this method-template
        /// </summary>
        public MethodTemplate.Signature Signature {
            get; set;
        }
        ISignature ISigned.Signature => Signature;
        /// <summary>
        /// The specifiers of this method-template
        /// </summary>
        public Method.Specifier Specifiers {
            get; set;
        }
        /// <summary>
        /// The context, of this method-template
        /// </summary>
        public SimpleMethodContext Context {
            get; set;
        }
        /// <summary>
        /// The return-type of this method
        /// </summary>
        public IType ReturnType {
            get;
        }
        /// <summary>
        /// The formal parameters of this method
        /// </summary>
        public IVariable[] Arguments {
            get;
        }
        /// <summary>
        /// The body of this method-template
        /// </summary>
        public InstructionBox Body {
            get; set;
        } = new InstructionBox();
        /// <summary>
        /// The context, where this method-template is defined in
        /// </summary>
        public IContext NestedIn {
            get; set;
        }
        /// <summary>
        /// The method-template, which is overridden by this method-template
        /// </summary>
        public IMethodTemplate<IMethod> Overrides {
            get; set;
        } = null;
        /// <summary>
        /// The method-template, which is overridden by this method-template
        /// </summary>
        public IDeclaredMethod OverrideTarget {
            get => Overrides;
        }
        /// <summary>
        /// Builds a method by applying the substitution defined by the given generic-actualparameter list
        /// </summary>
        public BasicMethod BuildMethod(IList<ITypeOrLiteral> genericActualParameters) {
            var dic = new Dictionary<IGenericParameter, ITypeOrLiteral>();
            int count = Math.Min(Signature.GenericFormalparameter.Count, genericActualParameters?.Count ?? 0);
            SimpleMethodContext ctx;
            if (count > 0) {
                for (int i = 0; i < count; ++i) {
                    dic.Add(Signature.GenericFormalparameter[i], genericActualParameters[i]);
                }
                ctx = (SimpleMethodContext)Context.Replace(dic, Context, Context);
            }
            else {
                ctx = Context;
            }

            var ret = /*new BasicMethod(Position,
                Signature.Name,
                Visibility,
                ReturnType.Replace(dic, Context, ctx),
                ctx.Arguments
                ) {
                Specifiers = Specifiers,
                Body = Body?.Replace(dic, Context, ctx)
            };*/builtMethods[dic];
            return ret;
        }
        /// <summary>
        /// Builds a method by applying the given generic substitution
        /// </summary>
        /// <remarks>This method is very different from <see cref="Replace(GenericParameterMap{IGenericParameter, ITypeOrLiteral}, IContext, IContext)"/> as this applies
        /// to the generic parameters defined by this template and Replace applies to foreign generic
        /// parameters used in this method</remarks>
        public BasicMethod BuildMethod(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameters) {
            /*var ctx = (SimpleMethodContext)Context.Replace(genericActualParameters, Context, Context);
            var ret = new BasicMethod(Position,
                Signature.Name,
                Visibility,
                ReturnType.Replace(genericActualParameters, Context, ctx),
                ctx.Arguments
            ) {
                Specifiers = Specifiers,
                Body = Body?.Replace(genericActualParameters, Context, ctx)// Was, wenn Body zu diesem Zeitpunkt noch nicht vorhanden ist?
            };
            return ret;*/
            return builtMethods[genericActualParameters];
        }
        /// <summary>
        /// Applies generic parameter substitution to this method-template returning a new method-template, where the generics are substituted.
        /// This method does not modify this method-template.
        /// </summary>
        /// <remarks>This method is very different from <see cref="BuildMethod(GenericParameterMap{IGenericParameter, ITypeOrLiteral})"/> as this applies to foreign generic
        /// parameters used in this method and BuildMethod applies to the generic parameters defined by this template</remarks>
        public IMethodTemplate<BasicMethod> Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                ret = new BasicMethodTemplate(Position, Signature.Name, Visibility,
                    ReturnType.Replace(genericActualParameter, curr, parent),
                    Arguments.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray(),
                    Signature.GenericFormalparameter) {
                    Overrides = Overrides?.Replace(genericActualParameter, curr, parent)
                };

                genericCache.TryAdd((genericActualParameter, curr), ret);
                ret.genericCache.TryAdd((genericActualParameter, curr), ret);
                if (Body != null)
                    ret.Body = Body.Replace(genericActualParameter, curr, parent);
            }
            return ret;
        }

        /*public IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.ConcatMany(((IASTNode[])Arguments).Concat(new[] { Body.Instruction }.Where(x => x != null)));
        }

        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public override string ToString() => Signature.ToString();
    }
}
