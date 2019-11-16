using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Analysis.Lattice;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using Imms;

namespace CompilerInfrastructure.Analysis {
    public abstract class MonoAnalysisDescription<S, D> : DefaultSubsetLattice<D> {
        protected MonoAnalysisDescription(IDataFlowDomain<D> _dom) {
            Domain = _dom ?? throw new ArgumentNullException(nameof(_dom));
        }
        public IDataFlowDomain<D> Domain { get; }
        public InterMonoAnalysis<S, D> Analysis { get; internal set; }
        public virtual ISet<D> InitialSeeds(IDeclaredMethod met) => ImmSet.Empty<D>();
        public abstract ISet<D> NormalFlow(ISet<D> In, IStatement stmt);
        public abstract ISet<D> NormalFlow(ISet<D> In, IExpression expr);
        public abstract S ComputeSummary(IDeclaredMethod met, ISet<D> mfp);
        public abstract ISet<D> SummaryFlow(ISet<D> In, IDeclaredMethod met);
        public virtual void Initialize(IDeclaredMethod met) { }
        public virtual void Finish() { }
    }
}
