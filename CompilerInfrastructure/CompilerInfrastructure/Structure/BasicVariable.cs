/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System.Linq;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Macros;

namespace CompilerInfrastructure.Structure {
    /// <summary>
    /// The basic-implementation of <see cref="IVariable"/>. For most local variables and fields this class should be sufficient
    /// </summary>
    [Serializable]
    public class BasicVariable : MutatingVariableImpl {
        static Dictionary<(string ,IType, IType, IExpression), BasicVariable> variableCache
            = new Dictionary<(string, IType, IType, IExpression), BasicVariable>();
        /// <summary>
        /// Initializes this unnamed variable/field with the given properties. Do not forget to assign <see cref="MutatingVariableImpl.Type"/>
        /// </summary>
        /// <param name="pos">The source-code location, where this variable/field is defined</param>
        /// <param name="isField"><c>null</c> if this is a local variable, else the type, where this field is defined in. Note: for global variables consider the <see cref="Types.NamespaceType"/></param>
        public BasicVariable(Position pos, IType isField)
            : base(pos, isField) {
        }
        /// <summary>
        /// Initializes this variable/field with the given properties
        /// </summary>
        /// <param name="pos">The source-code location, where this variable/field is defined</param>
        /// <param name="type">The type of this variable/field</param>
        /// <param name="specs">The specifiers</param>
        /// <param name="name">The name of this named variable/field</param>
        /// <param name="isField"><c>null</c> if this is a local variable, else the type, where this field is defined in. Note: for global variables consider the <see cref="Types.NamespaceType"/></param>
        /// <param name="vis">The visibility</param>
        public BasicVariable(Position pos, IType type, Variable.Specifier specs, string name, IType isField, Visibility vis = Visibility.Private)
            : base(pos, vis, type.Signature, specs, name, isField) {
            Type = type;
            variableCache[(Signature.Name, type, isField, null)] = this;
        }
        ///<inheritdoc/>
        protected override IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {

            var ret = this;
            var otherType = Type.Replace(genericActualParameter, curr, parent);
            var otherDefIn = DefinedInType?.Replace(genericActualParameter, curr, parent);
            var otherDflt = DefaultValue?.Replace(genericActualParameter, curr, parent);

            if (otherType != Type || otherDefIn != DefinedInType || otherDflt != DefaultValue) {
                /*ret = new BasicVariable(Position,
                    otherType,
                    VariableSpecifiers,
                    Signature.Name,
                    otherDefIn,
                    Visibility) {
                    DefaultValue = otherDflt
                };*/
                if (!variableCache.TryGetValue((Signature.Name, otherType, otherDefIn, otherDflt), out ret)) {
                    ret = new BasicVariableReplace(this,
                        genericActualParameter,
                        otherType,
                        otherDefIn,
                        otherDflt
                    );
                }
            }
            return ret;
        }
        /// <summary>
        /// The context, where this variable/field is defined in
        /// </summary>
        public IContext Parent {
            get; set;
        }
        ///<inheritdoc/>
        public override bool TryReplaceMacroParameters(MacroCallParameters args, out IVariable vr) {
            if (DefaultValue != null && DefaultValue.TryReplaceMacroParameters(args, out var dflts)) {
                if (dflts.Length != 1)
                    "A variable cannot be initialized with multiple default-values".Report(Position.Concat(args.Position));
                else {
                    if (!CompilerInfrastructure.Type.IsAssignable(dflts[0].ReturnType, Type))
                        $"The type {dflts[0].ReturnType.Signature} is incompatible with the variable-type {Type.Signature}".Report(Position.Concat(args.Position));
                    vr = new BasicVariable(Position, Type, VariableSpecifiers, Signature.Name, DefinedInType, Visibility) { DefaultValue = dflts[0] };
                    return true;
                }
            }
            vr = this;
            return false;
        }
        public override string ToString() => Signature.ToString();
        public override IExpression DefaultValue {
            get => base.DefaultValue;
            set {
                if (DefaultValue != value) {
                    variableCache.Remove((Signature.Name, Type, DefinedInType, DefaultValue));
                    variableCache[(Signature.Name, Type, DefinedInType, value)] = this;
                    base.DefaultValue = value;
                }
            }
        }
    }
    [Serializable]
    class BasicVariableReplace : BasicVariable {
        BasicVariable parentVar;
        GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter;
        internal BasicVariableReplace(BasicVariable parVr, GenericParameterMap<IGenericParameter, ITypeOrLiteral> gen, IType otherType, IType otherDefIn, IExpression otherDflt)
            : base(parVr.Position, otherType, parVr.VariableSpecifiers, parVr.Signature.Name, otherDefIn, parVr.Visibility) {
            DefaultValue = otherDflt;
            genericActualParameter = gen;
            parentVar = parVr;
        }
        protected override IVariable ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var parGen = this.genericActualParameter.Then(genericActualParameter, curr, parent, true);
            if (!parentVar.genericCache.TryGetValue(parGen, out var ret)) {
                ret = base.ReplaceImpl(genericActualParameter, curr, parent);
                (ret as BasicVariableReplace).parentVar = parentVar;
                (ret as BasicVariableReplace).genericActualParameter = parGen;
                if (!parentVar.genericCache.TryAdd(parGen, ret))
                    ret = parentVar.genericCache[parGen];
                return ret;
            }
            return ret;
        }
    }
}
