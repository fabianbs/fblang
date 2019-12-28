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
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Contexts {
    using Structure.Types;
    using DefResult = BooleanResult<CannotDefine>;
    [Serializable]
    class ContextReplace : IWrapperContext {
        [Serializable]
        internal struct ReplaceParameters : IEquatable<ReplaceParameters> {
            internal GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter;
            internal IContext curr;
            internal IContext parent;

            public override bool Equals(object obj) => obj is ReplaceParameters && Equals((ReplaceParameters)obj);
            public bool Equals(ReplaceParameters other) => EqualityComparer<GenericParameterMap<IGenericParameter, ITypeOrLiteral>>.Default.Equals(genericActualParameter, other.genericActualParameter) && EqualityComparer<IContext>.Default.Equals(curr, other.curr) && EqualityComparer<IContext>.Default.Equals(parent, other.parent);

            public override int GetHashCode() {
                var hashCode = 1281116461;
                hashCode = hashCode * -1521134295 + EqualityComparer<GenericParameterMap<IGenericParameter, ITypeOrLiteral>>.Default.GetHashCode(genericActualParameter);
                hashCode = hashCode * -1521134295 + EqualityComparer<IContext>.Default.GetHashCode(curr);
                hashCode = hashCode * -1521134295 + EqualityComparer<IContext>.Default.GetHashCode(parent);
                return hashCode;
            }
            public static bool IsFullInstantiation(IEnumerable<ITypeOrLiteral> genericActualParameter) {
                foreach (var genPar in genericActualParameter) {
                    if (genPar is IGenericParameter)
                        return false;
                    else if (genPar is IType ty && !IsFullInstantiation(ty.Signature.GenericActualArguments))
                        return false;
                }
                return true;

            }
            public bool IsFullInstantiation() {
                return IsFullInstantiation(genericActualParameter.Values);
            }
            public static bool operator ==(ReplaceParameters parameters1, ReplaceParameters parameters2) => parameters1.Equals(parameters2);
            public static bool operator !=(ReplaceParameters parameters1, ReplaceParameters parameters2) => !(parameters1 == parameters2);
        }
        [Serializable]
        class ReplaceDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
            where TKey:ISignature
            where TValue : IVisible, IReplaceableStructureElement<TValue>, ISigned<TKey>
                                                                                  {
            readonly IReadOnlyDictionary<TKey, TValue> underlying;
            readonly IDictionary<TKey, TValue> local;
            ReplaceParameters args;

            int underlyingCount;
            int localCount;
            public ReplaceDictionary(IReadOnlyDictionary<TKey, TValue> underlying, IDictionary<TKey, TValue> local, ReplaceParameters args) {
                this.underlying = underlying;
                this.local = local;
                this.args = args;
                underlyingCount = 0;
            }
            public TValue this[TKey key] {
                get {
                    if (TryGetValue(key, out var ret))
                        return ret;
                    return default;
                }
            }

            public IEnumerable<TKey> Keys {
                get => this.Select(x => x.Key);
            }
            public IEnumerable<TValue> Values {
                get => this.Select(x => x.Value);
            }
            public int Count {
                get {
                    DoReplaceIfNecessary();
                    return localCount;
                }
            }

            public bool ContainsKey(TKey key) => TryGetValue(key, out _);
            internal void DoReplaceIfNecessary() {
                //DOLATER Geht das auch effizienter?
                int nwUC;
                if ((nwUC = underlying.Count) > underlyingCount) {
                    underlyingCount = nwUC;
                    var nwTemplates = underlying.Keys.Except(local.Keys).ToArray();
                    foreach (var ky in nwTemplates) {
                        var ttp = underlying[ky];

                        var repl = ttp.Replace(args.genericActualParameter, args.curr, args.parent);
                        if (local.TryAdd(repl.Signature, repl)) {
                            localCount++;
                        }
                        /*else {
                            Console.Error.WriteLine($"{repl.Signature} was already defined in this context");
                        }*/
                    }

                }
            }
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {

                DoReplaceIfNecessary();
                return local.GetEnumerator();
            }

            public bool TryGetValue(TKey key, out TValue value) {
                DoReplaceIfNecessary();
                return local.TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        readonly IContext underlying;
        readonly BasicContext local;
        ReplaceParameters args;
        readonly bool isLocal;
        readonly Lazy<IContext> localContext;
        public ContextReplace(IContext underlying, GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent, Visibility outerVis = Visibility.Private)
            : this(underlying, new ReplaceParameters { genericActualParameter = genericActualParameter, curr = curr, parent = parent }) {

        }
        private ContextReplace(IContext underlying, ReplaceParameters rArgs, bool loc = false) {
            this.underlying = underlying;
            isLocal = loc;
            args = rArgs;

            local = new BasicContext(underlying.Module, underlying.DefiningRules, rArgs.IsFullInstantiation(), underlying.Position);

            Types = new ReplaceDictionary<Type.Signature, IType>(underlying.Types, local.types, args);
            TypeTemplates = new ReplaceDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>(underlying.TypeTemplates, local.typeTemplates, args);
            Methods = new ReplaceDictionary<Method.Signature, IMethod>(underlying.Methods, local.methods, args);
            Macros = new ReplaceDictionary<StringSignature, MacroFunction>(underlying.Macros, local.macros, args);
            MethodTemplates = new ReplaceDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>(underlying.MethodTemplates, local.methodTemplates, args);
            Variables = new ReplaceDictionary<Variable.Signature, IVariable>(underlying.Variables, local.variables, args);

            localContext = new Lazy<IContext>(isLocal ? (Func<IContext>)(() => this) : () => new ContextReplace(underlying.LocalContext, args, true));
            Identifiers = underlying.Identifiers;
        }

        public IReadOnlyDictionary<Type.Signature, IType> Types {
            get;
        }
        public IReadOnlyDictionary<TypeTemplate.Signature, ITypeTemplate<IType>> TypeTemplates {
            get;
        }
        public IReadOnlyDictionary<Method.Signature, IMethod> Methods {
            get;
        }
        public IReadOnlyDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>> MethodTemplates {
            get;
        }
        public IReadOnlyDictionary<StringSignature, MacroFunction> Macros {
            get;
        }
        public IReadOnlyDictionary<Variable.Signature, IVariable> Variables {
            get;
        }
        public IReadOnlyDictionary<StringSignature, object> Identifiers {
            get;
        }
        public bool CanDefineTypes {
            get => local.CanDefineTypes;
        }
        public bool CanDefineVariables {
            get => local.CanDefineVariables;
        }
        public bool CanDefineMethods {
            get => local.CanDefineMethods;
        }
        public Context.DefiningRules DefiningRules {
            get => local.DefiningRules;
        }
        public Module Module {
            get => underlying.Module;
        }
        public Position Position {
            get => underlying.Position;
        }
        public IContext Underlying => underlying;

        public string Name {
            get => underlying.Name;
        }
        public IContext LocalContext {
            get => localContext.Value;
        }

        public DefResult DefineMethod(IMethod met) => local.DefineMethod(met);
        public DefResult DefineMethodTemplate(IMethodTemplate<IMethod> met) => local.DefineMethodTemplate(met);
        public DefResult DefineMacro(MacroFunction m) => local.DefineMacro(m);
        public DefResult DefineType(IType tp) => local.DefineType(tp);
        public DefResult DefineTypeTemplate(ITypeTemplate<IType> tp) => local.DefineTypeTemplate(tp);
        public DefResult DefineVariable(IVariable vr) => local.DefineVariable(vr);
        public DefResult DefineIdentifier(string sig, object o) => underlying.DefineIdentifier(sig, o);

        public IEnumerable<IMethod> MethodsByName(string name) {
            (Methods as ReplaceDictionary<Method.Signature, IMethod>).DoReplaceIfNecessary();
            return local.MethodsByName(name);
        }

        public IEnumerable<IMethodTemplate<IMethod>> MethodTemplatesByName(string name) {
            (MethodTemplates as ReplaceDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>).DoReplaceIfNecessary();
            return local.MethodTemplatesByName(name);
        }
        public IEnumerable<MacroFunction> MacrosByName(string name) {
            (Macros as ReplaceDictionary<StringSignature, MacroFunction>).DoReplaceIfNecessary();
            return local.MacrosByName(name);
        }
        public IEnumerable<IType> TypesByName(string name) {
            (Types as ReplaceDictionary<Type.Signature, IType>).DoReplaceIfNecessary();
            return local.TypesByName(name);
        }

        public IEnumerable<ITypeTemplate<IType>> TypeTemplatesByName(string name) {
            (TypeTemplates as ReplaceDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>).DoReplaceIfNecessary();
            return local.TypeTemplatesByName(name);
        }

        public IEnumerable<IVariable> VariablesByName(string name) {
            (Variables as ReplaceDictionary<Variable.Signature, IVariable>).DoReplaceIfNecessary();
            return local.VariablesByName(name);
        }

        public IEnumerable<object> IdentifiersByName(string name) => underlying.IdentifiersByName(name);

        public IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ContextReplace(this, genericActualParameter, curr, parent);
        }
    }
}
