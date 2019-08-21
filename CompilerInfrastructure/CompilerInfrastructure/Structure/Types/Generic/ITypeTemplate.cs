/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CompilerInfrastructure.Utils.CoreExtensions;

namespace CompilerInfrastructure.Structure.Types.Generic {
    public interface ITypeTemplate<out T> : ISourceElement, IVisible, ISigned<TypeTemplate.Signature>, IReplaceableStructureElement<ITypeTemplate<T>> where T : IType {
        T BuildType(IReadOnlyList<ITypeOrLiteral> genericActualParameters);
        T BuildType(GenericParameterMap<IGenericParameter, ITypeOrLiteral> dic, IReadOnlyList<ITypeOrLiteral> genericActualParameters);
        /// <summary>
        /// Builds the type using the generic formalparameter as generic actualparameter.
        /// </summary>
        /// <returns></returns>
        T BuildType();

        /// <summary>
        /// Contains the generic type parameters
        /// </summary>
        /// <value>
        /// The header context.
        /// </value>
        ITypeTemplateContext HeaderContext {
            get;
        }
        /// <summary>
        /// Contains the method-, variable- and nested typedefs
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        ITypeTemplateContext Context {
            get;
        }
        Type.Specifier TypeSpecifiers {
            get;
        }
    }

    public static class TypeTemplate {
        [Serializable]
        public struct Signature : ISignature, IEquatable<Signature> {
            public Signature(string name, IReadOnlyList<IGenericParameter> genArgs) {
                Name = name;
                GenericFormalparameter = genArgs ?? List.Empty<IGenericParameter>();
            }
            public string Name {
                get;
            }
            public IReadOnlyList<IGenericParameter> GenericFormalparameter {
                get;
            }

            public override bool Equals(object obj) {
                return obj is Signature && Equals((Signature)obj);
            }

            public bool Equals(Signature other) {
                return
                    Name == other.Name &&
                    GenericFormalparameter.Count == other.GenericFormalparameter.Count &&
                    GenericFormalparameter.Zip(other.GenericFormalparameter).All(x => {
                        if (x.Item1 is GenericLiteralParameter)
                            return x.Item2 is GenericLiteralParameter;
                        else
                            return x.Item2 is GenericTypeParameter;
                    });
            }

            public override int GetHashCode() {
                return HashCode.Combine(Name, GenericFormalparameter.Aggregate(0, (x, y) => {
                    if (y is GenericLiteralParameter)
                        return x * x + 1;
                    else
                        return x * -x + 1;
                }));
            }
            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append("TEMPLATE ");
                sb.Append(Name);
                sb.Append("<");
                sb.AppendJoin(", ", GenericFormalparameter.Select(x => x.Name));
                sb.Append(">");
                return sb.ToString();
            }
            public static bool operator ==(Signature signature1, Signature signature2) => signature1.Equals(signature2);
            public static bool operator !=(Signature signature1, Signature signature2) => !(signature1 == signature2);
        }
    }
}
