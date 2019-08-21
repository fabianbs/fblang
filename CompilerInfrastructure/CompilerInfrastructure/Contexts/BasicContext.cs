/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Contexts.DataFlowAnalysis;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Summaries;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure {
    using DefResult = BooleanResult<CannotDefine>;
    [Serializable]
    public class BasicContext : IContext, ISerializable {
        protected internal readonly SignatureMultiMap<Type.Signature, IType> types;
        protected internal readonly SignatureMultiMap<Method.Signature, IMethod> methods;
        protected internal readonly SignatureMultiMap<Variable.Signature, IVariable> variables;
        protected internal readonly SignatureMultiMap<TypeTemplate.Signature, ITypeTemplate<IType>> typeTemplates = new SignatureMultiMap<TypeTemplate.Signature, ITypeTemplate<IType>>(false);
        protected internal readonly SignatureMultiMap<MethodTemplate.Signature, IMethodTemplate<IMethod>> methodTemplates = new SignatureMultiMap<MethodTemplate.Signature, IMethodTemplate<IMethod>>(false);
        protected internal readonly Dictionary<StringSignature, MacroFunction> macros = new Dictionary<StringSignature, MacroFunction>();
        protected internal readonly Dictionary<StringSignature, object> idents = new Dictionary<StringSignature, object>();
        private readonly Module module;

        public BasicContext(Module mod, Context.DefiningRules defRules, bool onlyConsiderPublicValuesForSerialiation, Position beginning = default) {
            module = mod;
            DefiningRules = defRules;
            Position = beginning;

            types = new SignatureMultiMap<Type.Signature, IType>(onlyConsiderPublicValuesForSerialiation);
            methods = new SignatureMultiMap<Method.Signature, IMethod>(onlyConsiderPublicValuesForSerialiation);
            variables = new SignatureMultiMap<Variable.Signature, IVariable>(onlyConsiderPublicValuesForSerialiation);
        }
        protected BasicContext(SerializationInfo info, StreamingContext context) {
            types = info.GetT<SignatureMultiMap<Type.Signature, IType>>(nameof(types));
            methods = info.GetT<SignatureMultiMap<Method.Signature, IMethod>>(nameof(methods));
            variables = info.GetT<SignatureMultiMap<Variable.Signature, IVariable>>(nameof(variables));
            typeTemplates = info.GetT<SignatureMultiMap<TypeTemplate.Signature, ITypeTemplate<IType>>>(nameof(typeTemplates));
            methodTemplates = info.GetT<SignatureMultiMap<MethodTemplate.Signature, IMethodTemplate<IMethod>>>(nameof(methodTemplates));
            macros = info.GetT<Dictionary<StringSignature, MacroFunction>>(nameof(macros));
            idents = info.GetT<Dictionary<StringSignature, object>>(nameof(idents));
            module = info.GetT<Module>(nameof(module));
            DefiningRules = Context.DefiningRules.None;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue(nameof(types), types);
            info.AddValue(nameof(methods), methods);
            info.AddValue(nameof(variables), variables);
            info.AddValue(nameof(typeTemplates), typeTemplates);
            info.AddValue(nameof(methodTemplates), methodTemplates);
            info.AddValue(nameof(macros), macros);
            info.AddValue(nameof(idents), idents);
            info.AddValue(nameof(module), module);
        }
        #region ISummarizable implementation
        class Summarizer : ISummarizer<IContext> {
            private readonly BasicContext dies;

            public Summarizer(BasicContext dies) {
                this.dies = dies;
            }

            public void Add(ISummary ctx) {
                
            }

            public bool TryTake(ISummary ctx, out IContext obj) => throw new NotImplementedException();
        }
        Summarizer summarizer=null;
        public ISummarizer GetSummarizer() {
            if(summarizer is null) {
                summarizer = new Summarizer(this);
            }
            return summarizer;
        }
        #endregion
        public virtual IReadOnlyDictionary<Type.Signature, IType> Types {
            get => types;
        }
        public virtual IReadOnlyDictionary<Method.Signature, IMethod> Methods {
            get => methods;
        }
        public virtual IReadOnlyDictionary<Variable.Signature, IVariable> Variables {
            get => variables;
        }
        public virtual IReadOnlyDictionary<TypeTemplate.Signature, ITypeTemplate<IType>> TypeTemplates {
            get => typeTemplates;
        }
        public virtual IReadOnlyDictionary<MethodTemplate.Signature, IMethodTemplate<IMethod>> MethodTemplates {
            get => methodTemplates;
        }
        public virtual IReadOnlyDictionary<StringSignature, MacroFunction> Macros {
            get => macros;
        }
        public IReadOnlyDictionary<StringSignature, object> Identifiers {
            get;
        }
        public DataFlowSet DataFlowSet { get; } = new DataFlowSet();

        public bool CanDefineTypes {
            get => DefiningRules.HasFlag(Context.DefiningRules.Types);
        }
        public bool CanDefineVariables {
            get => DefiningRules.HasFlag(Context.DefiningRules.Variables);
        }
        public bool CanDefineMethods {
            get => DefiningRules.HasFlag(Context.DefiningRules.Methods);
        }
        public virtual Module Module {
            get => module;
        }
        public Context.DefiningRules DefiningRules {
            get;
        }
        public Position Position {
            get;
        }

        public virtual DefResult DefineMethod(IMethod met) {
            if (CanDefineMethods) {
                return (methods.Add(met.Signature, met), CannotDefine.AlreadyExisting);
            }
            return CannotDefine.NotAllowedInCurrentContext;
        }
        public DefResult DefineMacro(MacroFunction m) {
            if (CanDefineMethods) {
                return (macros.TryAdd(m.Name, m), CannotDefine.AlreadyExisting);
            }
            return CannotDefine.NotAllowedInCurrentContext;

        }
        public DefResult DefineIdentifier(string sig, object o) {
            if (CanDefineVariables)
                return (idents.TryAdd(sig, o), CannotDefine.AlreadyExisting);
            return CannotDefine.NotAllowedInCurrentContext;
        }
        public virtual DefResult DefineMethodTemplate(IMethodTemplate<IMethod> met) {
            //return CanDefineMethods && methodTemplates.Add(met.Signature, met);
            if (CanDefineMethods)
                return (methodTemplates.Add(met.Signature, met), CannotDefine.AlreadyExisting);
            return CannotDefine.NotAllowedInCurrentContext;
        }

        public virtual DefResult DefineType(IType tp) {
            //return CanDefineTypes && types.Add(tp.Signature, tp);
            if (CanDefineTypes)
                return (types.Add(tp.Signature, tp), CannotDefine.AlreadyExisting);
            return CannotDefine.NotAllowedInCurrentContext;
        }

        public virtual DefResult DefineTypeTemplate(ITypeTemplate<IType> tp) {
            //return CanDefineTypes && typeTemplates.Add(tp.Signature, tp);
            if (CanDefineTypes)
                return (typeTemplates.Add(tp.Signature, tp), CannotDefine.AlreadyExisting);
            return CannotDefine.NotAllowedInCurrentContext;
        }

        public virtual DefResult DefineVariable(IVariable vr) {
            //return CanDefineVariables && variables.Add(vr.Signature, vr);
            if (CanDefineVariables)
                return (variables.Add(vr.Signature, vr), CannotDefine.NotAllowedInCurrentContext);
            return CannotDefine.NotAllowedInCurrentContext;
        }
        public virtual IEnumerable<IMethod> MethodsByName(string name) {
            return methods.FilterByName(name).Values;
        }
        public IEnumerable<MacroFunction> MacrosByName(string name) {
            if (macros.TryGetValue(name, out var val)) {
                yield return val;
            }
        }

        public virtual IEnumerable<IMethodTemplate<IMethod>> MethodTemplatesByName(string name) {
            return methodTemplates.FilterByName(name).Values;
        }

        public virtual IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            /*var ret = new BasicContext(Module, DefiningRules);

            foreach (var kvp in Types) {
                var tp = kvp.Value.Replace(genericActualParameter, curr, this);
                ret.types.Add(tp.Signature, tp);
            }

            foreach (var kvp in Methods) {
                var met = kvp.Value.Replace(genericActualParameter, curr, this);
                ret.methods.Add(met.Signature, met);
            }

            foreach (var kvp in Variables) {
                var vr = kvp.Value.Replace(genericActualParameter, curr, this);
                ret.variables.Add(vr.Signature, vr);
            }

            foreach (var kvp in TypeTemplates) {
                var tt = kvp.Value.Replace(genericActualParameter, curr, this);
                ret.typeTemplates.Add(tt.Signature, tt);
            }

            foreach (var kvp in MethodTemplates) {
                var mt = kvp.Value.Replace(genericActualParameter, curr, this);
                ret.methodTemplates.Add(mt.Signature, mt);
            }

            return ret;*/
            return new ContextReplace(this, genericActualParameter, curr, parent);
        }

        public virtual IEnumerable<IType> TypesByName(string name) {
            return types.FilterByName(name).Values;
        }

        public virtual IEnumerable<ITypeTemplate<IType>> TypeTemplatesByName(string name) {
            return typeTemplates.FilterByName(name).Values;
        }

        public virtual IEnumerable<IVariable> VariablesByName(string name) {
            return variables.FilterByName(name).Values;
        }
        public virtual IEnumerable<object> IdentifiersByName(string name) {
            if (idents.TryGetValue(name, out var ret))
                yield return ret;
        }

        public void DefineDataFlowFact(IDataFlowFact fact, IDataFlowValue val) {
            DataFlowSet[fact] = val;
        }

        

        public string Name {
            get {
                return "this context";
            }
        }

        public IContext LocalContext {
            get => this;
        }

    }
}