/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Type = CompilerInfrastructure.Structure.Types.Type;

namespace FBc {
    [Serializable]
    class IterableType : AggregateType {
        static readonly Dictionary<IType, IterableType> cache = new Dictionary<IType, IterableType>();
        private IterableType(IType itemTy) {
            ItemType = itemTy;
            Signature = new Type.Signature("iterable " + itemTy.Signature.Name, null);
            Context = new SimpleTypeContext(itemTy.Context.Module) { Type = this };
            Context.InstanceContext.DefineMethod(new BasicMethod(("", 0, 0), "getIterator", Visibility.Public, IteratorType.Get(itemTy), List.Empty<IVariable>()) { Specifiers = Method.Specifier.Abstract, NestedIn = this.Context });
        }

        public override IType ItemType {
            get;
        }
        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context {
            get;
        }
        public override Type.Specifier TypeSpecifiers {
            get => Type.Specifier.Interface;
        }
        public override Visibility Visibility {
            get => ItemType.Visibility;
        }
        public override Position Position {
            get => ItemType.Position;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();

        public override bool IsNestedIn(IType other) => other == this;
        public override bool IsSubTypeOf(IType other) => other == this;
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (other == this) {
                difference = 0;
                return true;
            }
            else {
                difference = int.MaxValue;
                return false;
            }
        }
        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(ItemType.Replace(genericActualParameter, curr, parent));
        }
        public static IType Get(IType itemTy) {
            if (!cache.TryGetValue(itemTy, out var ret)) {
                ret = new IterableType(itemTy);
                if (!cache.TryAdd(itemTy, ret))
                    ret = cache[itemTy];
            }
            return ret;
        }

        public override void PrintPrefix(TextWriter tw) {
            ItemType.PrintPrefix(tw);
        }
        public override void PrintValue(TextWriter tw) {
            tw.Write("iterable ");
            ItemType.PrintValue(tw);
        }
        public override void PrintSuffix(TextWriter tw) {
            ItemType.PrintSuffix(tw);
        }
    }
    [Serializable]
    class IteratorType : AggregateType {
        static readonly Dictionary<IType, IteratorType> cache = new Dictionary<IType, IteratorType>();
        private IteratorType(IType itemTy) {
            ItemType = itemTy;
            Signature = new Type.Signature("iterator " + itemTy.Signature.Name, null);
            Context = new SimpleTypeContext(itemTy.Context.Module) { Type = this };
            Context.InstanceContext.DefineMethod(new BasicMethod(("", 0, 0), "tryGetNext", Visibility.Public, PrimitiveType.Bool, new[] { new BasicVariable(("", 0, 0), itemTy.AsByRef(), Variable.Specifier.LocalVariable, "ret", null) }) { Specifiers = Method.Specifier.Abstract, NestedIn = this.Context });
        }

        public override IType ItemType {
            get;
        }
        public override Type.Signature Signature {
            get;
        }
        public override ITypeContext Context {
            get;
        }
        public override Type.Specifier TypeSpecifiers {
            get => Type.Specifier.Interface;
        }
        public override Visibility Visibility {
            get => ItemType.Visibility;
        }
        public override Position Position {
            get => ItemType.Position;
        }
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get;
        } = List.Empty<IType>();

        public override bool IsNestedIn(IType other) => other == this;
        public override bool IsSubTypeOf(IType other) => other == this;
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (other == this) {
                difference = 0;
                return true;
            }
            else {
                difference = int.MaxValue;
                return false;
            }
        }
        protected override IType ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Get(ItemType.Replace(genericActualParameter, curr, parent));
        }
        public static IType Get(IType itemTy) {
            if (!cache.TryGetValue(itemTy, out var ret)) {
                ret = new IteratorType(itemTy);
                if (!cache.TryAdd(itemTy, ret))
                    ret = cache[itemTy];
            }
            return ret;
        }

        public override void PrintPrefix(TextWriter tw) {
            ItemType.PrintPrefix(tw);
        }
        public override void PrintValue(TextWriter tw) {
            tw.Write("iterator ");
            ItemType.PrintValue(tw);
        }
        public override void PrintSuffix(TextWriter tw) {
            ItemType.PrintSuffix(tw);
        }
    }
}
