/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CompilerInfrastructure.Utils.CoreExtensions;

namespace CompilerInfrastructure.Structure {
    public interface IMethodTemplate<out T> : ISourceElement, IVisible, IDeclaredMethod, ISigned<MethodTemplate.Signature>, IReplaceableStructureElement<IMethodTemplate<T>> where T : IMethod {
        T BuildMethod(IList<ITypeOrLiteral> genericActualParameters);
        T BuildMethod(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameters);
    }
    public static class MethodTemplate {
        public struct Signature : ISignature, IEquatable<Signature> {
            //DOLATER sig contains rettp and argtps
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

            public static bool operator ==(Signature signature1, Signature signature2) => signature1.Equals(signature2);
            public static bool operator !=(Signature signature1, Signature signature2) => !(signature1 == signature2);
        }
    }
}
