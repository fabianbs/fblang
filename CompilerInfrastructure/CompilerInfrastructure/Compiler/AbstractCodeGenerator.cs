/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Compiler {
    /// <summary>
    /// An abstract class implementing the <see cref="ICodeGenerator"/> interface by
    /// specializing it to the structure of <see cref="Module"/>
    /// </summary>
    public abstract class AbstractCodeGenerator : ICodeGenerator {
        /// <summary>
        /// Specifies, how to deal with templates
        /// </summary>
        public enum TemplateBehavior {
            /// <summary>
            /// Ignore templates
            /// </summary>
            None,
            /// <summary>
            /// Only declare, but not implement templates
            /// </summary>
            Declare,
            /// <summary>
            /// Both declare and implement templates as with other entities
            /// </summary>
            DeclareAndImplement
        }
        readonly HashSet<IType> declaredTypes = new HashSet<IType>();
        readonly HashSet<IMethod> declaredMethods = new HashSet<IMethod>();
        readonly HashSet<ITypeTemplate<IType>> declaredTypeTemplates;
        readonly HashSet<IMethodTemplate<IMethod>> declaredMethodTemplates;

        readonly HashSet<IType> implementedTypes = new HashSet<IType>();
        readonly HashSet<IMethod> implementedMethods = new HashSet<IMethod>();
        readonly HashSet<ITypeTemplate<IType>> implementedTypeTemplates;
        readonly HashSet<IMethodTemplate<IMethod>> implementedMethodTemplates;

        readonly HashSet<IVariable> declaredFields = new HashSet<IVariable>();
        protected INameMangler mangler;
        /// <summary>
        /// Initializes a new code generator with the specified properties
        /// </summary>
        /// <param name="declaresTypeTemplates">Specifies, how to deal with type-templates</param>
        /// <param name="declaresMethodTemplates">Specifies, how to deal with method-templates</param>
        /// <param name="implicitDeclareTypeMembers">When true, all members of a type are automatically declared, such that it does not have to be done when declaring a type</param>
        /// <param name="implicitImplementTypeMembers">When true, all members of a type are automatically implemented, such that it does not have to be done when implementing a type</param>
        public AbstractCodeGenerator(TemplateBehavior declaresTypeTemplates,
            TemplateBehavior declaresMethodTemplates,
            bool implicitDeclareTypeMembers = true,
            bool implicitImplementTypeMembers = true,
            INameMangler _mangler=null) {
            DeclaresTypeTemplates = declaresTypeTemplates != TemplateBehavior.None;
            DeclaresMethodTemplates = declaresMethodTemplates != TemplateBehavior.None;
            ImplementsTypeTemplates = declaresTypeTemplates == TemplateBehavior.DeclareAndImplement;
            ImplementsMethodTemplates = declaresMethodTemplates == TemplateBehavior.DeclareAndImplement;
            if (DeclaresTypeTemplates) {
                declaredTypeTemplates = new HashSet<ITypeTemplate<IType>>();
                if (ImplementsTypeTemplates)
                    implementedTypeTemplates = new HashSet<ITypeTemplate<IType>>();
            }

            if (DeclaresMethodTemplates) {
                declaredMethodTemplates = new HashSet<IMethodTemplate<IMethod>>();
                if (ImplementsMethodTemplates)
                    implementedMethodTemplates = new HashSet<IMethodTemplate<IMethod>>();
            }

            ImplicitDeclareTypeMembers = implicitDeclareTypeMembers;
            ImplicitImplementTypeMembers = implicitImplementTypeMembers;
            mangler = _mangler ?? NullMangler.Instance;
        }
        /// <summary>
        /// True, iff type-templates should be declared
        /// </summary>
        protected bool DeclaresTypeTemplates {
            get;
        }
        /// <summary>
        /// True, iff method-templates should be declared
        /// </summary>
        protected bool DeclaresMethodTemplates {
            get;
        }
        /// <summary>
        /// True, iff type-templates should be implemented
        /// </summary>
        protected bool ImplementsTypeTemplates {
            get;
        }
        /// <summary>
        /// True, iff method-templates should be implemented
        /// </summary>
        protected bool ImplementsMethodTemplates {
            get;
        }
        /// <summary>
        /// When true, all members of a type are automatically declared, such that it does not have to be done when declaring a type
        /// </summary>
        public bool ImplicitDeclareTypeMembers {
            get;
        }
        /// <summary>
        /// When true, all members of a type are automatically implemented, such that it does not have to be done when implementing a type
        /// </summary>
        public bool ImplicitImplementTypeMembers {
            get;
        }
        /// <summary>
        /// Actually generates the code for the given module.
        /// 
        /// This consists of: declaring all types and type-members by calling <see cref="DeclareTypes(IContext)"/>, <see cref="DeclareTypeMembers(IContext)"/> 
        /// and optionally <see cref="DeclareTypeTemplates(IContext)"/>,
        /// and after that implementing these by calling <see cref="ImplementTypes(IContext)"/>, <see cref="ImplementTypeMembers(IContext)"/> and optionally <see cref="ImplementTypeTemplates(IContext)"/>
        /// </summary>
        /// <param name="mod">The module, for which code should be generated</param>
        /// <returns>True, when no errors were reported, false otherwise</returns>
        protected virtual bool DoCodeGen(Module mod) {
            bool ret = true;
            ret &= DeclareTypes(mod);
            if (DeclaresTypeTemplates)
                ret &= DeclareTypeTemplates(mod);

            ret &= DeclareTypeMembers(mod);

            ret &= ImplementTypes(mod);
            if (ImplementsTypeTemplates)
                ret &= ImplementTypeTemplates(mod);
            ret &= ImplementTypeMembers(mod);
            return ret;
        }
        /// <summary>
        /// Does the actual code-generation for the given module.
        /// 
        /// This method calls the overridable methods <see cref="InitCodeGen(Module)"/>, <see cref="DoCodeGen(Module)"/> and <see cref="FinalizeCodeGen()"/>
        /// </summary>
        /// <param name="mod">The module, for which code should be generated</param>
        /// <returns>True, when no errors were reported, false otherwise</returns>
        public bool CodeGen(Module mod) {
            InitCodeGen(mod);
            try {
                return DoCodeGen(mod);
            }
            finally {
                FinalizeCodeGen();
            }

        }
        /// <summary>
        /// Initialized the code-generation for the given module
        /// </summary>
        public virtual void InitCodeGen(Module mod) {

        }
        /// <summary>
        /// Finalizes the code-generation for the given module
        /// </summary>
        public virtual void FinalizeCodeGen() {
        }
        #region types
        /// <summary>
        /// Declares all types from the given context and from subcontexts
        /// </summary>
        protected virtual bool DeclareTypes(IContext ctx) {
            bool ret = true;
            foreach (var tp in ctx.LocalTypes()) {
                ret &= DeclareType(tp);
                ret &= DeclareTypes(tp.Context);
                if (DeclaresTypeTemplates)
                    ret &= DeclareTypeTemplates(tp.Context);
            }
            return ret;
        }
        /// <summary>
        /// Declares the given type by calling <see cref="DeclareTypeImpl(IType)"/>, if it is not already declared
        /// </summary>
        protected virtual bool DeclareType(IType ty) {
            if (ty != null) {
                if (declaredTypes.Add(ty))
                    return DeclareTypeImpl(ty);
            }
            // no errors if there is nothing to compile
            return true;
        }
        /// <summary>
        /// Declares the given type
        /// </summary>
        /// <param name="ty">The type to declare</param>
        /// <returns>True, when no errors were reported, false otherwise</returns>
        protected abstract bool DeclareTypeImpl(IType ty);
        /// <summary>
        /// Declares all type-templates from the given context and all subcontexts
        /// </summary>
        protected virtual bool DeclareTypeTemplates(IContext ctx) {
            bool ret = true;
            foreach (var tp in ctx.LocalTypeTemplates()) {
                ret &= DeclareTypeTemplate(tp);
                ret &= DeclareTypes(tp.Context);
                ret &= DeclareTypeTemplates(tp.Context);
            }
            return ret;
        }

        protected virtual bool DeclareTypeTemplate(ITypeTemplate<IType> ty) {
            if (ty != null) {
                if (declaredTypeTemplates.Add(ty))
                    return DeclareTypeTemplateImpl(ty);
            }
            // no errors if there is nothing to compile
            return true;
        }
        /// <summary>
        /// Declares the given type-template by calling <see cref="DeclareTypeTemplateImpl(ITypeTemplate{IType})"/>, if it is not already declared
        /// </summary>
        protected abstract bool DeclareTypeTemplateImpl(ITypeTemplate<IType> ty);
        /// <summary>
        /// Implements all types from the given context and from subcontexts
        /// </summary>
        protected virtual bool ImplementTypes(IContext ctx) {
            bool ret = true;
            foreach (var tp in ctx.LocalTypes()) {
                ret &= ImplementType(tp);
                ret &= ImplementTypes(tp.Context);
                if (ImplementsTypeTemplates)
                    ret &= ImplementTypeTemplates(tp.Context);
            }
            return ret;
        }
        /// <summary>
        /// Implements the given type by calling <see cref="ImplementTypeImpl(IType)"/>, if it is not already implemented
        /// </summary>
        protected virtual bool ImplementType(IType ty) {
            if (ty != null && implementedTypes.Add(ty)) {
                bool ret = DeclareType(ty);
                return ret & ImplementTypeImpl(ty);
            }
            return true;
        }
        /// <summary>
        /// Implements the given type by implementing all methods (and optionally all method-templates) of it
        /// </summary>
        protected virtual bool ImplementTypeImpl(IType ty) {
            bool ret = ImplementMethods(ty.Context);
            if (ImplementsMethodTemplates)
                ret &= ImplementMethodTemplates(ty.Context);
            return ret;
        }
        /// <summary>
        /// Implements all type-templates from the given context and from subcontexts
        /// </summary>
        protected virtual bool ImplementTypeTemplates(IContext ctx) {
            bool ret = true;
            foreach (var tp in ctx.LocalTypeTemplates()) {
                ret &= ImplementTypeTemplate(tp);
                ret &= ImplementTypes(tp.Context);
                ret &= ImplementTypeTemplates(tp.Context);
            }
            return ret;
        }
        /// <summary>
        /// Implements the given type by calling <see cref="ImplementTypeTemplateImpl(IType)"/>, if it is not already implemented
        /// </summary>
        protected virtual bool ImplementTypeTemplate(ITypeTemplate<IType> ty) {
            if (ty != null && implementedTypeTemplates.Add(ty)) {
                bool ret = DeclareTypeTemplate(ty);
                return ret & ImplementTypeTemplateImpl(ty);
            }
            return true;
        }
        /// <summary>
        /// Implements the given type-template by implementing all methods (and optionally all method-templates) of it
        /// </summary>
        protected virtual bool ImplementTypeTemplateImpl(ITypeTemplate<IType> ty) {
            bool ret = ImplementMethods(ty.Context);
            if (ImplementsMethodTemplates)
                ret &= ImplementMethodTemplates(ty.Context);
            return ret;
        }

        #endregion
        #region type members
        /// <summary>
        /// Declares all methods (, optionally method-templates) and fields of the given context and optionally (depending on <see cref="ImplicitDeclareTypeMembers"/>) 
        /// of all subcontexts, which include type-contexts and optionally type-template-contxets
        /// </summary>
        protected virtual bool DeclareTypeMembers(IContext ctx) {
            bool ret = DeclareMethods(ctx);
            if (DeclaresMethodTemplates)
                ret &= DeclareMethodTemplates(ctx);
            ret &= DeclareFields(ctx);
            if (ImplicitDeclareTypeMembers) {

                foreach (var tp in ctx.LocalTypes()) {
                    ret &= DeclareTypeMembers(tp.Context);
                }
                if (DeclaresTypeTemplates) {
                    foreach (var tp in ctx.LocalTypeTemplates()) {
                        ret &= DeclareTypeMembers(tp.Context);
                    }
                }
            }
            return ret;
        }
        /// <summary>
        /// Implements all methods (, optionally method-templates) and fields of the given context and optionally (depending on <see cref="ImplicitImplementTypeMembers"/>) 
        /// of all subcontexts, which include type-contexts and optionally type-template-contxets
        /// </summary>
        protected virtual bool ImplementTypeMembers(IContext ctx) {
            bool ret = ImplementMethods(ctx);
            if (ImplementsMethodTemplates)
                ret &= ImplementMethodTemplates(ctx);
            if (ImplicitImplementTypeMembers) {

                foreach (var tp in ctx.LocalTypes()) {
                    ret &= ImplementTypeMembers(tp.Context);
                }
                if (ImplementsTypeTemplates) {
                    foreach (var tp in ctx.LocalTypeTemplates()) {
                        ret &= ImplementTypeMembers(tp.Context);
                    }
                }
            }
            return ret;
        }
        #endregion
        #region methods
        /// <summary>
        /// Declares all methods from the given context by calling <see cref="DeclareMethod(IMethod)"/>, not including subcontexts
        /// </summary>
        protected virtual bool DeclareMethods(IContext ctx) {
            bool ret = true;
            foreach (var met in ctx.LocalMethods()) {
                ret &= DeclareMethod(met);
            }
            return ret;
        }
        /// <summary>
        /// Declares the given method by calling <see cref="DeclareMethodImpl(IMethod)"/>, if it is not already declared
        /// </summary>
        protected virtual bool DeclareMethod(IMethod met) {
            if (met != null) {
                if (declaredMethods.Add(met))
                    return DeclareMethodImpl(met);
            }
            // no errors if there is nothing to compile
            return true;
        }
        /// <summary>
        /// Declares the given method
        /// </summary>
        protected abstract bool DeclareMethodImpl(IMethod met);
        /// <summary>
        /// Declares all method-templates from the given context by calling <see cref="DeclareMethodTemplate(IMethodTemplate{IMethod})"/>, not including subcontexts
        /// </summary>
        protected virtual bool DeclareMethodTemplates(IContext ctx) {
            bool ret = true;
            foreach (var met in ctx.LocalMethodTemplates()) {
                ret &= DeclareMethodTemplate(met);
            }
            return ret;
        }
        /// <summary>
        /// Declares the given method-template by calling <see cref="DeclareMethodTemplateImpl(IMethodTemplate{IMethod})"/>, if it is not already declared
        /// </summary>
        protected virtual bool DeclareMethodTemplate(IMethodTemplate<IMethod> met) {
            if (met != null) {
                if (declaredMethodTemplates.Add(met))
                    return DeclareMethodTemplateImpl(met);
            }
            // no errors if there is nothing to compile
            return true;
        }
        /// <summary>
        ///  Declares the given method-template
        /// </summary>
        protected abstract bool DeclareMethodTemplateImpl(IMethodTemplate<IMethod> met);

        /// <summary>
        /// Implements all methods from the given context by calling <see cref="ImplementMethod(IMethod)"/>, not including subcontexts
        /// </summary>
        protected virtual bool ImplementMethods(IContext ctx) {
            return ctx.LocalMethods().All(x => ImplementMethod(x));
        }
        /// <summary>
        /// Implements the given method by calling <see cref="ImplementMethodImpl(IMethod)"/>, if it is not already implemented
        /// </summary>
        protected virtual bool ImplementMethod(IMethod met) {
            if (met != null && implementedMethods.Add(met)) {
                bool ret = DeclareMethod(met);
                return ret & ImplementMethodImpl(met);
            }
            return true;
        }
        /// <summary>
        /// Implements the given method
        /// </summary>
        protected abstract bool ImplementMethodImpl(IMethod met);
        /// <summary>
        /// Implements all method-templates from the given context by calling <see cref="ImplementMethodTemplate(IMethodTemplate{IMethod})"/>, not including subcontexts
        /// </summary>
        protected virtual bool ImplementMethodTemplates(IContext ctx) {
            return ctx.LocalMethodTemplates().All(x => ImplementMethodTemplate(x));
        }
        /// <summary>
        /// Implements the given method-template by calling <see cref="ImplementMethodTemplateImpl(IMethodTemplate{IMethod})"/>, if it is not already implemented
        /// </summary>
        protected virtual bool ImplementMethodTemplate(IMethodTemplate<IMethod> met) {
            if (met != null && implementedMethodTemplates.Add(met)) {
                bool ret = DeclareMethodTemplate(met);
                return ret & ImplementMethodTemplateImpl(met);
            }
            return true;
        }
        /// <summary>
        /// Implements the given method-template
        /// </summary>
        protected abstract bool ImplementMethodTemplateImpl(IMethodTemplate<IMethod> met);
        #endregion
        #region fields
        /// <summary>
        /// Declares all fields of the given context by calling <see cref="DeclareField(IVariable)"/>
        /// </summary>
        protected virtual bool DeclareFields(IContext ctx) {
            bool ret = true;
            foreach (var met in ctx.Variables) {
                ret &= DeclareField(met.Value);
            }
            return ret;
        }
        /// <summary>
        /// Declares the given field by calling <see cref="DeclareFieldImpl(IVariable)"/>, if it is not already implemented
        /// </summary>
        protected virtual bool DeclareField(IVariable fld) {
            if (fld != null) {
                if (declaredFields.Add(fld))
                    return DeclareFieldImpl(fld);
            }
            // no errors if there is nothing to compile
            return true;
        }
        /// <summary>
        /// Declares the given field
        /// </summary>
        protected abstract bool DeclareFieldImpl(IVariable fld);
        #endregion
    }
}
