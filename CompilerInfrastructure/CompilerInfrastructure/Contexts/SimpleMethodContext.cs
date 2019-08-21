/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CompilerInfrastructure.Context;

namespace CompilerInfrastructure.Contexts {
    [Serializable]
    public class SimpleMethodContext : HierarchialContext {
        [Flags]
        [Serializable]
        public enum VisibleMembers {
            None = 0x00,
            Instance = 0x01,
            Static = 0x02,
            Both = ~0,
        }

        readonly IContext instCtx, statCtx;
        IVariable[] args = Array.Empty<IVariable>();
        readonly VisibleMembers visMem;
        static IEnumerable<IContext> Extract(ITypeContext parentCtx, VisibleMembers parentVis) {
            if (parentVis.HasFlag(VisibleMembers.Instance))
                yield return parentCtx.InstanceContext;
            if (parentVis.HasFlag(VisibleMembers.Static))
                yield return parentCtx.StaticContext;
        }
        static IEnumerable<IContext> Extract(IEnumerable<IContext> ctxs, VisibleMembers parentVis) {
            using (var it = ctxs.GetEnumerator()) {
                if (it.MoveNext()) {
                    if (parentVis.HasFlag(VisibleMembers.Instance))
                        yield return it.Current;
                    if (it.MoveNext()) {
                        if (parentVis.HasFlag(VisibleMembers.Static))
                            yield return it.Current;
                    }
                }
            }
        }
        public SimpleMethodContext(Module mod, DefiningRules defRules, ITypeContext parentCtx, VisibleMembers parentVis = VisibleMembers.Both, IEnumerable<IVariable> args = null)
            : base(mod, Extract(parentCtx, parentVis), defRules, parentCtx != null && parentCtx.Type != null && !parentCtx.Type.MayRelyOnGenericParameters()) {
            instCtx = parentVis.HasFlag(VisibleMembers.Instance) ? parentCtx.InstanceContext : null;
            statCtx = parentVis.HasFlag(VisibleMembers.Static) ? parentCtx.StaticContext : null;
            SetArguments(args);
            visMem = parentVis;
            DefinedInType = parentCtx.Type;
        }
        public SimpleMethodContext(Module mod, IEnumerable<IContext> ctxs, VisibleMembers parentVis = VisibleMembers.Both, IEnumerable<IVariable> args = null, IType definedInType = null)
            : base(mod, Extract(ctxs, parentVis), DefiningRules.All, definedInType != null && !definedInType.MayRelyOnGenericParameters()) {
            using (var it = ctxs.GetEnumerator()) {
                if (it.MoveNext() && parentVis.HasFlag(VisibleMembers.Instance))
                    instCtx = it.Current;
                else
                    instCtx = null;
                if (it.MoveNext() && parentVis.HasFlag(VisibleMembers.Static))
                    statCtx = it.Current;
                else
                    statCtx = null;
            }
            SetArguments(args);
            visMem = parentVis;
            DefinedInType = definedInType;
        }

        public IMethod Method {
            get; set;
        }
        public IMethodTemplate<IMethod> MethodTemplate {
            get; set;
        }
        /// <summary>
        /// Sets the arguments for the method where this context belongs to and defines a variable for each argument.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void SetArguments(IEnumerable<IVariable> args) {
            if (args != null && args.Any()) {
                this.args = args.ToArray();
                foreach (var arg in args) {
                    DefineVariable(arg);
                }
            }
            else {
                this.args = Array.Empty<IVariable>();
            }
        }
        public IVariable[] Arguments {
            get => args;
        }
        public SimpleMethodContext Restrict(VisibleMembers vis) {
            return new SimpleMethodContext(Module, new[] { instCtx, statCtx }, vis, Arguments, DefinedInType);
        }

        static readonly LazyDictionary<Module, SimpleMethodContext> immu = new LazyDictionary<Module, SimpleMethodContext>(mod =>
            new SimpleMethodContext(mod, DefiningRules.None, SimpleTypeContext.GetImmutable(mod), VisibleMembers.Both)
        );
        public static SimpleMethodContext GetImmutable(Module mod) {
            return immu[mod];
        }
        public override IContext Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue((genericActualParameter, curr), out var ret)) {
                ret = new SimpleMethodContext(Module, new[] {
                    instCtx?.Replace(genericActualParameter, curr, parent),
                    statCtx?.Replace(genericActualParameter, curr, parent)},
                    visMem,
                    args.Select(x => x.Replace(genericActualParameter, curr, parent)),
                    DefinedInType
                );
                genericCache.TryAdd((genericActualParameter, curr), ret);
            }
            return ret;
        }
        public IType DefinedInType {
            get;
        }
    }
}
