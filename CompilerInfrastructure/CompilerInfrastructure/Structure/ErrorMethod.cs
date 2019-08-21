/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    [Serializable]
    internal class ErrorMethod : IMethod {
        
        private ErrorMethod() {
        }
        public Method.Signature Signature {
            get;
        } = new Method.Signature("", Type.Error);
        ISignature ISigned.Signature => Signature;
        IContext IRangeScope.Context {
            get => Context;
        }
        public InstructionBox Body {
            get => InstructionBox.Error;
            set => throw new NotSupportedException();
        }
        public Method.Specifier Specifiers {
            get;
        } = (Method.Specifier)~0u;
        public Visibility Visibility {
            get;
        } = Visibility.Public;
        public Position  Position {
            get;
        } = default;
        public static ErrorMethod Instance {
            get;
        } = new ErrorMethod();
        public IType ReturnType {
            get => Type.Error;
        }
        public IVariable[] Arguments {
            get;
        } = Array.Empty<IVariable>();
        public IContext NestedIn {
            get => Context.Module;
        }
        public SimpleMethodContext Context {
            get;
        } = SimpleMethodContext.GetImmutable(new Module());
        public IDeclaredMethod OverrideTarget {
            get => Instance;
        }

        /*public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public IMethod Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        public bool IsVariadic() => true;
    }
}
