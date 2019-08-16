using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure {
    using DefResult = BooleanResult<CannotDefine>;
    [Serializable]
    public class HierarchialContext : IContext/*, IEnumerable<IContext>*/, ISerializable {
        [Serializable]
        class HierarchialDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, ISerializable/* where TValue : IVisible*/ where TKey : ISignature {
            readonly IEnumerable<IReadOnlyDictionary<TKey, TValue>> underlying;
            [NonSerialized]
            readonly SignatureMultiMap<TKey, TValue> cache = new SignatureMultiMap<TKey, TValue>();
            public HierarchialDictionary(IEnumerable<IReadOnlyDictionary<TKey, TValue>> underlying) {
                this.underlying = underlying.Where(x => x != null);
            }
            protected HierarchialDictionary(SerializationInfo info, StreamingContext context) {
                //cache = (SignatureMultiMap<TKey, TValue>)info.GetValue(nameof(cache), typeof(SignatureMultiMap<TKey, TValue>));
                underlying = (IReadOnlyDictionary<TKey, TValue>[]) info.GetValue(nameof(underlying), typeof(IReadOnlyDictionary<TKey, TValue>[]));
            }
            public void GetObjectData(SerializationInfo info, StreamingContext context) {
                //info.AddValue(nameof(cache), cache);
                info.AddValue(nameof(underlying), underlying.ToArray());
            }

            public TValue this[TKey key] {
                get {
                    if (TryGetValue(key, out var ret)) {
                        return ret;
                    }
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
                get => Keys.Count();
            }

            public bool ContainsKey(TKey key) => TryGetValue(key, out var _);

            public bool TryGetValue(TKey key, out TValue value) {
                if (cache.TryGetValue(key, out value))
                    return true;
                foreach (var dic in underlying) {
                    if (dic.TryGetValue(key, out value)) {
                        cache.Add(key, value);
                        return true;
                    }
                }
                value = default;
                return false;
            }
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
                var seen = new HashSet<TKey>();
                foreach (var x in cache) {
                    seen.Add(x.Key);
                    yield return x;
                }
                foreach (var dic in underlying) {
                    foreach (var kvp in dic) {
                        if (seen.Add(kvp.Key))
                            yield return kvp;
                    }
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerable<TValue> FilterByName(string name) {
                //TODO geht das auch effizienter?

                //return cache.FilterByName(name).Values
                //    .Concat(underlying.SelectMany(x => x.FilterByName(name)).Except(cache.FilterByName(name).Values));
                var seen = new HashSet<TKey>();
                foreach (var x in cache.FilterByName(name)) {
                    seen.Add(x.Key);
                    yield return x.Value;
                }
                foreach (var dic in underlying) {
                    foreach (var kvp in dic.FilterByName(name)) {
                        if (seen.Add(kvp.Key))
                            yield return kvp.Value;
                    }
                }
            }


        }
        protected private Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IContext> genericCache = new Dictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext), IContext>();
        protected private IEnumerable<IContext> contexts;
        private readonly Context.DefiningRules defRules;
        readonly HierarchialDictionary<Type.Signature, IType> types;
        readonly HierarchialDictionary<Method.Signature, IMethod> methods;
        readonly HierarchialDictionary<Variable.Signature, IVariable> variables;
        readonly HierarchialDictionary<TypeTemplate.Signature, ITypeTemplate<IType>> typeTemplates;
        readonly HierarchialDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>> methodTemplates;
        readonly HierarchialDictionary<StringSignature, MacroFunction> macros;
        readonly HierarchialDictionary<StringSignature, object> idents;
        readonly bool hasLocalContext;

        public HierarchialContext(Module module, IEnumerable<IContext> contexts, Context.DefiningRules defRules, bool onlyConsiderPublicValuesForSerialization) {
            this.defRules = defRules;
            Position = contexts.Any() ? contexts.First().Position : default;
            this.contexts = Flatten(module, contexts, defRules, Position, onlyConsiderPublicValuesForSerialization);
            types = new HierarchialDictionary<Type.Signature, IType>(this.contexts.Select(x => x.Types));
            methods = new HierarchialDictionary<Method.Signature, IMethod>(this.contexts.Select(x => x.Methods));
            variables = new HierarchialDictionary<Variable.Signature, IVariable>(this.contexts.Select(x => x.Variables));
            typeTemplates = new HierarchialDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>(this.contexts.Select(x => x.TypeTemplates));
            methodTemplates = new HierarchialDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>(this.contexts.Select(x => x.MethodTemplates));
            macros = new HierarchialDictionary<StringSignature, MacroFunction>(this.contexts.Select(x => x.Macros));
            idents = new HierarchialDictionary<StringSignature, object>(this.contexts.Select(x => x.Identifiers));
            hasLocalContext = defRules != Context.DefiningRules.None;
        }
        private protected HierarchialContext(Module mod, IEnumerable<IContext> contexts, bool dontCreateLocalContextOrNoneDefRule, bool onlyConsiderPublicValuesForSerialization) {
            if (contexts is null || !contexts.Any())
                throw new ArgumentException("There must be at least one parent-context for creating a context-hierarchy", nameof(contexts));
            this.defRules = contexts.First().DefiningRules;

            Position = contexts.Any() ? contexts.First().Position : default;
            this.contexts = Flatten(mod, contexts, Context.DefiningRules.None, Position, onlyConsiderPublicValuesForSerialization);
            types = new HierarchialDictionary<Type.Signature, IType>(this.contexts.Select(x => x.Types));
            methods = new HierarchialDictionary<Method.Signature, IMethod>(this.contexts.Select(x => x.Methods));
            variables = new HierarchialDictionary<Variable.Signature, IVariable>(this.contexts.Select(x => x.Variables));
            typeTemplates = new HierarchialDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>(this.contexts.Select(x => x.TypeTemplates));
            methodTemplates = new HierarchialDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>(this.contexts.Select(x => x.MethodTemplates));
            macros = new HierarchialDictionary<StringSignature, MacroFunction>(this.contexts.Select(x => x.Macros));
            idents = new HierarchialDictionary<StringSignature, object>(this.contexts.Select(x => x.Identifiers));
            hasLocalContext = false;
        }

        private static List<IContext> Flatten(Module mod, IEnumerable<IContext> contexts, Context.DefiningRules defRules, Position pos, bool onlyConsiderPublicValuesForSerialization) {
            var ret = new List<IContext>();
            if (defRules != Context.DefiningRules.None)
                ret.Add(new BasicContext(mod, defRules, onlyConsiderPublicValuesForSerialization, pos));
            foreach (var x in contexts) {
                // Don't flatten Type-Contexts or MethodContexts
                if (x.GetType() == typeof(HierarchialContext) && x is HierarchialContext hcx) {
                    foreach (var y in hcx.contexts)
                        ret.Add(y);
                }
                else
                    ret.Add(x);
            }
            return ret;
        }
        protected HierarchialContext(SerializationInfo info, StreamingContext context) {
            contexts = info.GetT<IContext[]>(nameof(contexts));
            types = new HierarchialDictionary<Type.Signature, IType>(this.contexts.Select(x => x.Types));
            methods = new HierarchialDictionary<Method.Signature, IMethod>(this.contexts.Select(x => x.Methods));
            variables = new HierarchialDictionary<Variable.Signature, IVariable>(this.contexts.Select(x => x.Variables));
            typeTemplates = new HierarchialDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>(this.contexts.Select(x => x.TypeTemplates));
            methodTemplates = new HierarchialDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>(this.contexts.Select(x => x.MethodTemplates));
            macros = new HierarchialDictionary<StringSignature, MacroFunction>(this.contexts.Select(x => x.Macros));
            idents = new HierarchialDictionary<StringSignature, object>(this.contexts.Select(x => x.Identifiers));
            hasLocalContext = false;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(contexts), contexts.ToArray());
        }
        public IContext LocalContext => contexts.First();
        public IReadOnlyDictionary<Type.Signature, IType> Types {
            get => types;
        }
        public IReadOnlyDictionary<Method.Signature, IMethod> Methods {
            get => methods;
        }
        public IReadOnlyDictionary<Variable.Signature, IVariable> Variables {
            get => variables;
        }
        public IReadOnlyDictionary<TypeTemplate.Signature, ITypeTemplate<IType>> TypeTemplates {
            get => typeTemplates;
        }
        public IReadOnlyDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>> MethodTemplates {
            get => methodTemplates;
        }
        public IReadOnlyDictionary<StringSignature, MacroFunction> Macros {
            get => macros;
        }

        public IReadOnlyDictionary<StringSignature, object> Identifiers {
            get;
        }
        public bool CanDefineTypes {
            get => defRules.HasFlag(Context.DefiningRules.Types);
        }
        public bool CanDefineVariables {
            get => defRules.HasFlag(Context.DefiningRules.Variables);
        }
        public bool CanDefineMethods {
            get => defRules.HasFlag(Context.DefiningRules.Methods);
        }
        public Context.DefiningRules DefiningRules {
            get => defRules;
        }
        // Avoid name-clashes in Module
        protected virtual Module TheModule => contexts.First().Module;
        public Module Module => TheModule;

        public Position Position {
            get; set;
        }
        public virtual string Name {
            get => hasLocalContext && contexts.HasCount(2) ? contexts.ElementAt(1).Name : contexts.First().Name;
        }

        public DefResult DefineMethod(IMethod met) {
            if (CanDefineMethods) {
                if (contexts.First().DefineMethod(met).Get(out var fail))
                    return true;
                return fail;
            }
            return CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineMacro(MacroFunction m) {
            return (CanDefineMethods && contexts.First().DefineMacro(m)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineType(IType tp) {
            return (CanDefineTypes && contexts.First().DefineType(tp)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineVariable(IVariable vr) {
            return (CanDefineVariables && contexts.First().DefineVariable(vr)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineIdentifier(string sig, object o) {
            return (CanDefineVariables && contexts.First().DefineIdentifier(sig, o)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineTypeTemplate(ITypeTemplate<IType> tp) {
            return (CanDefineTypes && contexts.First().DefineTypeTemplate(tp)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineMethodTemplate(IMethodTemplate<IMethod> met) {
            return (CanDefineMethods && contexts.First().DefineMethodTemplate(met)) | CannotDefine.NotAllowedInCurrentContext;
        }
        public IEnumerable<IMethod> MethodsByName(string name) {
            return methods.FilterByName(name);
        }
        public IEnumerable<MacroFunction> MacrosByName(string name) {
            return macros.FilterByName(name);
        }
        public IEnumerable<IType> TypesByName(string name) {
            return types.FilterByName(name);
        }
        public IEnumerable<IVariable> VariablesByName(string name) {
            return variables.FilterByName(name);
        }

        public IEnumerable<IMethodTemplate<IMethod>> MethodTemplatesByName(string name) {
            return methodTemplates.FilterByName(name);
        }

        public IEnumerable<object> IdentifiersByName(string name) {
            return idents.FilterByName(name);
        }

        public IEnumerable<ITypeTemplate<IType>> TypeTemplatesByName(string name) {
            return typeTemplates.FilterByName(name);
        }
        public IEnumerable<IContext> Underlying => contexts;
        public IEnumerator<IContext> GetEnumerator() => contexts.GetEnumerator();
        //IEnumerator IEnumerable.GetEnumerator() => contexts.GetEnumerator();

        public virtual IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {

                var ctxs = new List<IContext>();
                bool needNew = false;
                foreach (var ctx in contexts) {
                    var nwCtx = ctx.Replace(genericActualParameter, curr, this);
                    if (nwCtx != ctx)
                        needNew = true;
                    ctxs.Add(nwCtx);
                }
                if (needNew)
                    //genericCache.TryAdd((genericActualParameter, curr), ret = new HierarchialContext(Module, ctxs, DefiningRules));
                    genericCache.TryAdd((genericActualParameter, curr), ret = new ContextReplace(this, genericActualParameter, curr, parent));
                else
                    genericCache.TryAdd((genericActualParameter, curr), ret = this);
            }
            return ret;
        }


    }
}