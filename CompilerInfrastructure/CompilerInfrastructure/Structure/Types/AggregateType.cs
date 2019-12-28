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
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public abstract class AggregateType : IWrapperType {
        protected private Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IType> genericCache
            = new Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IType>();
        public abstract IType ItemType {
            get;
        }
        public abstract Type.Signature Signature {
            get;
        }
        public abstract ITypeContext Context {
            get;
        }
        IContext IRangeScope.Context => Context;
        public abstract Type.Specifier TypeSpecifiers {
            get;
        }
        public abstract Visibility Visibility {
            get;
        }
        public abstract Position  Position {
            get;
        }
        public abstract IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        }
        public IType NestedIn {
            get => null;
        }
        public IContext DefinedIn => ItemType.DefinedIn;

        ISignature ISigned.Signature => Signature;

        public override string ToString() {
            var sw = new StringWriter();
            this.PrintTo(sw);
            return sw.ToString();
        }
        /*IEnumerable<IASTNode> Children() {
            foreach (var x in Context.Variables) {
                yield return x.Value;
            }
            foreach (var x in Context.Methods) {
                yield return x.Value;
            }
            foreach (var x in Context.Types) {
                yield return x.Value;
            }
        }*/
        /*public virtual IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.ConcatMany(Children());
        }*/
        public abstract bool IsNestedIn(IType other);
        public abstract bool IsSubTypeOf(IType other);
        public abstract bool IsSubTypeOf(IType other, out int difference);
       /* IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public virtual IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                return ret;
            }
            ret = ReplaceImpl(genericActualParameter, curr, parent);
            genericCache.TryAdd((genericActualParameter, curr), ret);
            return ret;
        }
        ITypeOrLiteral IReplaceableStructureElement<ITypeOrLiteral, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }
        protected abstract IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual bool ImplementsInterface(IType intf) => ImplementingInterfaces.Contains(intf);
        public abstract void PrintPrefix(TextWriter tw);
        public abstract void PrintValue(TextWriter tw);
        public abstract void PrintSuffix(TextWriter tw);
    }
}
