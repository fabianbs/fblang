/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    public interface IStatement : ISourceElement, IReplaceableStructureElement<IStatement>, IStatementContainer, IExpressionContainer {
        //IStatement Replace(IDictionary<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);//must be cached
        bool TryReplaceMacroParameters(MacroCallParameters args, out IStatement stmt);
    }
    public interface ISideEffectFulStatement : IStatement {
        ref ValueLazy<bool> MayHaveSideEffects {
            get;
        }
    }
    [Serializable]
    public abstract class StatementImpl : IStatement {
        protected bool needToReplaceMacroParameters = true;
        protected readonly LazyDictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext, IContext), IStatement> genericCache;
        public StatementImpl(Position pos) {
            Position = pos;
            genericCache = new LazyDictionary<(GenericParameterMap<IGenericParameter, ITypeOrLiteral>, IContext, IContext), IStatement>(x => ReplaceImpl(x.Item1, x.Item2, x.Item3));
        }
        public virtual Position Position {
            get;
        }

        //public abstract IRefEnumerator<IASTNode> GetEnumerator();

        public abstract IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent);
        public virtual IStatement Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return genericCache[(genericActualParameter, curr, parent)];
        }

        /*IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        public abstract IEnumerable<IStatement> GetStatements();
        public abstract IEnumerable<IExpression> GetExpressions();
        public virtual bool TryReplaceMacroParameters(MacroCallParameters args, out IStatement stmt) {
            if (needToReplaceMacroParameters) {
                bool ret = TryReplaceMacroParametersImpl(args, out stmt);
                if (!ret)
                    needToReplaceMacroParameters = false;
                return ret;
            }
            stmt = this;
            return false;
        }
        protected abstract bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt);
    }
    public static class Statement {
        public static IStatement Error => ErrorStatement.Instance;
        public static IStatement Nop => NopStatement.Instance;
    }
    public static class StatementHelper {
        public static bool IsError(this IStatement stmt) {
            return stmt == Statement.Error;
        }
        public static bool MayHaveSideEffects(this IStatement stmt) {
            return stmt is ISideEffectFulStatement sefs && sefs.MayHaveSideEffects.Value;
        }
    }
}