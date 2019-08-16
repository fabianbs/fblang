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
    using DefResult = BooleanResult<CannotDefine>;
    [Serializable]
    class ConstrainedContext : IWrapperContext {
        [Serializable]
        class ConstrainedDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TValue : IVisible {
            readonly IReadOnlyDictionary<TKey, TValue> underlying;
            int underlyingCount;
            int count;
            readonly Visibility outerVis;

            public ConstrainedDictionary(IReadOnlyDictionary<TKey, TValue> underlying, Visibility outerVis) {
                this.underlying = underlying;
                this.outerVis = outerVis;
                count = 0;
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
                    var nwUC = underlying.Count;
                    if (underlyingCount != nwUC) {
                        underlyingCount = nwUC;
                        count = Keys.Count();
                    }
                    return count;
                }
            }

            public bool ContainsKey(TKey key) => TryGetValue(key, out _);
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
                foreach (var kvp in underlying) {
                    if (kvp.Value.Visibility >= outerVis)
                        yield return kvp;
                }
            }
            public bool TryGetValue(TKey key, out TValue value) {
                if (underlying.TryGetValue(key, out value) && value.Visibility >= outerVis)
                    return true;
                value = default;
                return false;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal readonly IContext underlying;
        readonly Visibility outerVis;

        public ConstrainedContext(IContext underlying, Visibility constraint) {
            outerVis = constraint;
            this.underlying = underlying;

            Types = new ConstrainedDictionary<Type.Signature, IType>(underlying.Types, constraint);
            TypeTemplates = new ConstrainedDictionary<TypeTemplate.Signature, ITypeTemplate<IType>>(underlying.TypeTemplates, constraint);
            Methods = new ConstrainedDictionary<Method.Signature, IMethod>(underlying.Methods, constraint);
            MethodTemplates = new ConstrainedDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>>(underlying.MethodTemplates, constraint);
            Variables = new ConstrainedDictionary<Variable.Signature, IVariable>(underlying.Variables, constraint);
            Macros = new ConstrainedDictionary<StringSignature, MacroFunction>(underlying.Macros, constraint);
            Identifiers = underlying.Identifiers;
        }

        public Visibility Constraint => outerVis;

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
        public IReadOnlyDictionary<Variable.Signature, IVariable> Variables {
            get;
        }
        public IReadOnlyDictionary<StringSignature, MacroFunction> Macros {
            get;
        }
        public IReadOnlyDictionary<StringSignature, object> Identifiers {
            get;
        }
        public bool CanDefineTypes {
            get => false;
        }
        public bool CanDefineVariables {
            get => false;
        }
        public bool CanDefineMethods {
            get => false;
        }
        public Context.DefiningRules DefiningRules {
            get => Context.DefiningRules.None;
        }
        public Module Module {
            get => underlying.Module;
        }
        public Position  Position {
            get => underlying.Position;
        }
        public IContext Underlying => underlying;

        public string Name {
            get => $"the {(Visibility)(Visibility.Public - outerVis)}-variant of {underlying.Name}";
        }
        public IContext LocalContext {
            get => underlying.LocalContext;
        }

        public DefResult DefineMethod(IMethod met) => CannotDefine.NotAllowedInCurrentContext;
        public DefResult DefineMacro(MacroFunction m) => CannotDefine.NotAllowedInCurrentContext;
        public DefResult DefineMethodTemplate(IMethodTemplate<IMethod> met) => CannotDefine.NotAllowedInCurrentContext;
        public DefResult DefineType(IType tp) => CannotDefine.NotAllowedInCurrentContext;
        public DefResult DefineTypeTemplate(ITypeTemplate<IType> tp) => CannotDefine.NotAllowedInCurrentContext;
        public DefResult DefineVariable(IVariable vr) => CannotDefine.NotAllowedInCurrentContext;
        public IEnumerable<IMethod> MethodsByName(string name) {
            return underlying.MethodsByName(name).Where(x => x.Visibility >= outerVis);
        }
        public IEnumerable<MacroFunction> MacrosByName(string name) {
            return underlying.MacrosByName(name).Where(x => x.Visibility >= outerVis);
        }
        public IEnumerable<IMethodTemplate<IMethod>> MethodTemplatesByName(string name) {
            return underlying.MethodTemplatesByName(name).Where(x => x.Visibility >= outerVis);
        }

        public IEnumerable<IType> TypesByName(string name) {
            return underlying.TypesByName(name).Where(x => x.Visibility >= outerVis);
        }

        public IEnumerable<ITypeTemplate<IType>> TypeTemplatesByName(string name) {
            return underlying.TypeTemplatesByName(name).Where(x => x.Visibility >= outerVis);
        }

        public IEnumerable<IVariable> VariablesByName(string name) {
            return underlying.VariablesByName(name).Where(x => x.Visibility >= outerVis);
        }

        public IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ContextReplace(this, genericActualParameter, curr, parent);
        }

        public IEnumerable<object> IdentifiersByName(string name) => underlying.IdentifiersByName(name);
        public DefResult DefineIdentifier(string sig, object o) => underlying.DefineIdentifier(sig, o);

    }
}
