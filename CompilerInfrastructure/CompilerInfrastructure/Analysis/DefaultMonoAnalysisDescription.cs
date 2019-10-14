using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using Imms;

namespace CompilerInfrastructure.Analysis {
    public abstract class MonoAnalysisDescription<S, D>  {

        public InterMonoAnalysis<S, D> Analysis { get; internal set; }
        public virtual ISet<D> InitialSeeds() => ImmSet.Empty<D>();
        public virtual ISet<D> Merge(ISet<D> frst, ISet<D> scnd) => frst.ToImmSet().Union(scnd);
        public virtual bool SqSubsetEqual(ISet<D> frst, ISet<D> scnd) => frst.IsSubsetOf(scnd);
        public abstract ISet<D> NormalFlow(ISet<D> In, IStatement stmt);
        public abstract ISet<D> NormalFlow(ISet<D> In, IExpression expr, IStatement parent);

    }
}
