/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Contexts {
    public interface IStructTypeContext : IContext {
        IContext InstanceContext {
            get;
        }
        IContext StaticContext {
            get;
        }
    }
    public interface ITypeContext : IStructTypeContext {

        IType Type {
            get; set;
        }
    }
    public interface ITypeTemplateContext : ITypeContext {

        ITypeTemplate<IType> TypeTemplate {
            get; set;
        }
    }
    public static class StructTypeContext {
        public static IEnumerable<IType> LocalTypes(this IStructTypeContext stc) {
            return stc.InstanceContext.LocalContext.Types.Values.Concat(stc.StaticContext.LocalContext.Types.Values);
        }
        public static IEnumerable<ITypeTemplate<IType>> LocalTypeTemplates(this IStructTypeContext stc) {
            return stc.InstanceContext.LocalContext.TypeTemplates.Values.Concat(stc.StaticContext.LocalContext.TypeTemplates.Values);
        }
        public static IEnumerable<IMethod> LocalMethods(this IStructTypeContext stc) {
            return stc.InstanceContext.LocalContext.Methods.Values.Concat(stc.StaticContext.LocalContext.Methods.Values);
        }
        public static IEnumerable<IMethodTemplate<IMethod>> LocalMethodTemplates(this IStructTypeContext stc) {
            return stc.InstanceContext.LocalContext.MethodTemplates.Values.Concat(stc.StaticContext.LocalContext.MethodTemplates.Values);
        }
        public static IEnumerable<IVariable> LocalVariables(this IStructTypeContext stc) {
            return stc.InstanceContext.LocalContext.Variables.Values.Concat(stc.StaticContext.LocalContext.Variables.Values);
        }

        public static IEnumerable<IType> LocalTypes(this IContext ctx) {
            if (ctx is IStructTypeContext stc)
                return stc.LocalTypes();
            return ctx.LocalContext.Types.Values;
        }
        public static IEnumerable<ITypeTemplate<IType>> LocalTypeTemplates(this IContext ctx) {
            if (ctx is IStructTypeContext stc)
                return stc.LocalTypeTemplates();
            return ctx.LocalContext.TypeTemplates.Values;
        }
        public static IEnumerable<IMethod> LocalMethods(this IContext ctx) {
            if (ctx is IStructTypeContext stc)
                return stc.LocalMethods();
            return ctx.LocalContext.Methods.Values;
        }
        public static IEnumerable<IMethodTemplate<IMethod>> LocalMethodTemplates(this IContext ctx) {
            if (ctx is IStructTypeContext stc)
                return stc.LocalMethodTemplates();
            return ctx.LocalContext.MethodTemplates.Values;
        }
        public static IEnumerable<IVariable> LocalVariables(this IContext ctx) {
            if (ctx is IStructTypeContext stc)
                return stc.LocalVariables();
            return ctx.LocalContext.Variables.Values;
        }
    }
}
