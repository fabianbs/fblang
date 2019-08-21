/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using static CompilerInfrastructure.Context;

namespace CompilerInfrastructure.Structure.Types {

    /// <summary>
    /// A type providing a signature which matches all existing types as supertype
    /// </summary>
    /// <seealso cref="CompilerInfrastructure.IType" />
    [Serializable]
    public class TopType : IType {
        private protected TopType() {

        }
        public virtual Type.Signature Signature {
            get;
        } = new Type.Signature("", null, true);
        ISignature ISigned.Signature => Signature;
        public ITypeContext Context {
            get;
        } = SimpleTypeContext.GetImmutable(new Module());
        public Type.Specifier TypeSpecifiers {
            get;
        } = (Type.Specifier) ~0;
        public Visibility Visibility {
            get;
        } = Visibility.Public;
        public Position Position {
            get;
        } = default;
        IContext IRangeScope.Context => Context;
        public bool IsNestedIn(IType other) => true;
        public bool IsSubTypeOf(IType other) => true;
        internal static TopType Instance {
            get;
        } = new TopType();
        public IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = new List<IType>();
        public IType NestedIn {
            get => this;
        }
        public IContext DefinedIn => Context.Module;

        public virtual bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            return other == this;
        }

        /*public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }
        public bool ImplementsInterface(IType intf) => true;

        public void PrintPrefix(TextWriter tw) { }
        public void PrintValue(TextWriter tw) => tw.Write("<TOP>");
        public void PrintSuffix(TextWriter tw) { }
    }
    /// <summary>
    /// A type providing a signature which matches all existing types as subtype
    /// </summary>
    /// <seealso cref="CompilerInfrastructure.IType" />
    [Serializable]
    public class BottomType : IType {
        private protected BottomType() {

        }
        public virtual Type.Signature Signature {
            get;
        } = new Type.Signature("", null, true);
        ISignature ISigned.Signature => Signature;
        public ITypeContext Context {
            get;
        } = SimpleTypeContext.GetImmutable(new Module());
        public Type.Specifier TypeSpecifiers {
            get;
        } = (Type.Specifier) ~0;
        public Visibility Visibility {
            get;
        } = Visibility.Public;
        public Position Position {
            get;
        } = default;
        IContext IRangeScope.Context => Context;
        public bool IsNestedIn(IType other) => true;
        public bool IsSubTypeOf(IType other) => true;
        internal static BottomType Instance {
            get;
        } = new BottomType();
        public IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = new List<IType>();
        public IType NestedIn {
            get => this;
        }
        public IContext DefinedIn => Context.Module;

        public virtual bool IsSubTypeOf(IType other, out int difference) {
            difference = 0;
            return true;
        }

        /*public IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) => this;
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }
        public bool ImplementsInterface(IType intf) => true;

        public void PrintPrefix(TextWriter tw) { }
        public void PrintValue(TextWriter tw) => tw.Write("<BOTTOM>");
        public void PrintSuffix(TextWriter tw) { }
    }
}
