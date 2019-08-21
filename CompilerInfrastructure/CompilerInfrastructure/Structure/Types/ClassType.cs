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
using System.IO;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Structure.Types {
    [Serializable]
    public class ClassType : ModifyingTypeImpl, IHierarchialType {
        //besser wäre mit friend class ClassTypeTemplate
        internal readonly Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, ClassType> genericCache
            = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, ClassType>();
        readonly HashSet<IType> interfaces = new HashSet<IType>();
        public ClassType(Position pos, string name, Visibility vis, IContext definedIn) : this(pos, name, vis, null, definedIn) {
        }
        protected internal ClassType(Position pos, string name, Visibility vis, IReadOnlyList<ITypeOrLiteral> genericArguments, IContext definedIn) {
            Signature = new Type.Signature(name, genericArguments);
            Position = pos;
            Visibility = vis;
            DefinedIn = definedIn;
        }
        public override IContext DefinedIn { get; }
        public override Type.Signature Signature {
            get;
        }
        ISignature ISigned.Signature => Signature;
        public override ITypeContext Context {
            get; set;
        }

        public override Type.Specifier TypeSpecifiers {
            get;
            set;
        } = Type.Specifier.None;
        public override Visibility Visibility {
            get;
        }
        public override Position Position {
            get;
        }
        public ClassType Superclass {
            get;
            set;
        } = null;
        public IContext Parent {
            get; set;
        } = null;
        public override IReadOnlyCollection<IType> ImplementingInterfaces {
            get => interfaces;
        }
        public IEnumerable<IType> AllImplementingInterfaces() {
            return Superclass is null
                ? ImplementingInterfaces
                : ImplementingInterfaces.Concat(Superclass.AllImplementingInterfaces());
        }
        public override bool ImplementsInterface(IType intf) {
            return interfaces.Contains(intf) || SuperType != null && SuperType.ImplementsInterface(intf);
        }
        public bool AddInterface(IType intf) {
            if (intf != null && intf.IsInterface()) {
                interfaces.Add(intf);
                return true;
            }
            return false;
        }
        public IHierarchialType SuperType => Superclass;

        public override IType NestedIn {
            get => (Parent as ITypeContext)?.Type;
        }

        public override bool IsNestedIn(IType other) {
            return other?.Context == Parent;
        }
        public override bool IsSubTypeOf(IType other) {
            return InternalSubtypeOf(other, out _);
        }
        bool InternalSubtypeOf(IType other, out int diff) {

            diff = 0;
            if (other is null)
                return Superclass is null;

            if (other.IsError()) {
                diff = int.MaxValue;
                return true;
            }


            for (var vgl = this; vgl != null; vgl = vgl.Superclass, diff++) {
                if (other == vgl)
                    return true;
            }
            diff = 0;
            return false;
        }
        public override bool IsSubTypeOf(IType other, out int difference) {
            if (InternalSubtypeOf(other, out difference))
                return true;
            else if (other is ClassType ctp && ctp.InternalSubtypeOf(this, out difference)) {
                difference = -difference;
                return true;
            }
            return false;
        }

        public override IType Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                if (Signature.BaseGenericType != null) {
                    var gen = ClassTypeTemplate.FromList(Signature.BaseGenericType.Signature.GenericFormalparameter, Signature.GenericActualArguments)
                        .Then(genericActualParameter, curr, parent);
                    ret = (ClassType)Signature.BaseGenericType.BuildType(gen, gen.ToValueList());
                    if (genericCache.TryAdd(genericActualParameter, ret))
                        return ret;
                    return genericCache[genericActualParameter];
                    /*if (Signature.GenericActualArguments.Count <= genericActualParameter.Count
                        && Signature.GenericActualArguments.All(x => x is IGenericParameter gen && genericActualParameter.ContainsKey(gen))) {
                        ret = (ClassType)Signature.BaseGenericType.BuildType(genericActualParameter, genericActualParameter.Values.ToList());

                        if (Signature.GenericActualArguments.Count < genericActualParameter.Count) {
                            var dic = new GenericParameterMap<IGenericParameter, ITypeOrLiteral>(genericActualParameter);
                            foreach (var gen in Signature.GenericActualArguments.OfType<IGenericParameter>()) {
                                dic.Remove(gen);
                            }
                            ret = (ClassType)ret.Replace(dic, curr, parent);
                        }

                        if (genericCache.TryAdd(genericActualParameter, ret))
                            return ret;
                        return genericCache[genericActualParameter];
                    }*/
                }
                ret = new ClassType(Position, Signature.Name, Visibility, Signature.GenericActualArguments.Select(x => {
                    if (x is IType genTp)
                        return genTp.Replace(genericActualParameter, curr, parent);
                    return x;
                }).ToArray(), DefinedIn) {
                    TypeSpecifiers = TypeSpecifiers,
                    Superclass = Superclass?.Replace(genericActualParameter, curr, parent) as ClassType

                };
                ret.Signature.BaseGenericType = Signature.BaseGenericType;
                genericCache.TryAdd(genericActualParameter, ret);
                ret.genericCache.TryAdd(genericActualParameter, ret);
                ret.Context = (ITypeContext)Context.Replace(genericActualParameter, curr, parent);
                ret.Context.Type = ret;
                ret.interfaces.AddRange(ImplementingInterfaces.Select(x => x.Replace(genericActualParameter, curr, parent)));
                ret.Parent = parent;

            }
            return ret;
        }
        public override string ToString() => Signature.ToString();
        public override void PrintPrefix(TextWriter tw) { }
        public override void PrintValue(TextWriter tw) {
            tw.Write(Signature.ToString());
        }
        public override void PrintSuffix(TextWriter tw) { }
    }
}
