/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class EnumType : ModifyingTypeImpl, IHierarchialType {
        public EnumType(Position pos, string name, IType parent, Visibility vis, IContext definedIn) {
            Position = pos;
            Parent = parent;
            Signature = new Type.Signature(name, null);
            Visibility = vis;
            DefinedIn = definedIn;
        }

        public IList<(string itemName, IExpression initializationValue)> EnumItems {
            get;
        } = new List<(string, IExpression)>();
        public override Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public override ITypeContext Context {
            get;
            set;
        }
        public override Type.Specifier TypeSpecifiers {
            get;
            set;
        } = Type.Specifier.Enum;
        public override Visibility Visibility {
            get;
        }
        public override IContext DefinedIn { get; }
        public override Position Position {
            get;
        }
        public IType Parent {
            get;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();
        public IHierarchialType SuperType {
            get; set;
        }
        public override IType NestedIn {
            get => Parent;
        }

        public override bool IsNestedIn(IType other) => other == Parent;
        public override bool IsSubTypeOf(IType other) => other.IsError() || other == this;
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (other.IsError()) {
                difference = int.MaxValue;
                return true;
            }
            difference = 0;
            return other == this;
        }

        public override void PrintPrefix(TextWriter tw) { }
        public override void PrintSuffix(TextWriter tw) { }
        public override void PrintValue(TextWriter tw) {
            tw.Write(Signature.ToString());
        }
        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
    }
}
