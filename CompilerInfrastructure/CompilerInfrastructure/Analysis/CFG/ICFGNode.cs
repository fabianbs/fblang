using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Analysis.CFG {
    public interface ICFGNode : IPositional {
        bool IsRoot => Previous is null;
        bool IsTerminatingNode {
            get {
                var nxt = Next;
                return nxt is null || (nxt is ICollection<ICFGNode> coll ? coll.Count == 0 : !nxt.Any());
            }
        }
        bool IsExitNode { get; }
        ISet<ICFGNode> Previous { get; }
        ISet<ICFGNode> Next { get; }
        void ForEach<TState>(TState state, Func<ICFGNode, TState, bool> fn) {
            if(fn(this, state)) {
                foreach(var succ in Next) {
                    succ.ForEach(state, fn);
                }
            }
        }
    }
    public class StatementCFGNode : ICFGNode {
        public StatementCFGNode(IStatement _stmt, bool terminator = false, bool exit = false) {
            Statement = _stmt ?? throw new ArgumentNullException(nameof(_stmt));
            Next = new HashSet<ICFGNode>();
            Previous = new HashSet<ICFGNode>();
            IsTerminatingNode = terminator | exit;
            IsExitNode = exit;
        }
        public IStatement Statement { get; }
        public ISet<ICFGNode> Previous { get; }
        public ISet<ICFGNode> Next { get; }
        public Position Position => Statement.Position;
        public bool IsTerminatingNode { get; }
        public bool IsExitNode { get; }

        
    }
    public class ExpressionCFGNode : ICFGNode {
        public ExpressionCFGNode(IExpression _expr) {
            Expression = _expr ?? throw new ArgumentNullException(nameof(_expr));
            Next = new HashSet<ICFGNode>();
            Previous = new HashSet<ICFGNode>();
        }
        public IExpression Expression { get; }
        public ISet<ICFGNode> Previous { get; }
        public ISet<ICFGNode> Next { get; }
        public Position Position => Expression.Position;
        public bool IsTerminatingNode => false;

        public bool IsExitNode => false;

        
    }
    public class NopCFGNode : ICFGNode {
        public NopCFGNode(Position pos) {
            Position = pos;
            Next = new HashSet<ICFGNode>();
            Previous = new HashSet<ICFGNode>();
        }
        public bool IsTerminatingNode => false;
        public ISet<ICFGNode> Previous { get; }
        public ISet<ICFGNode> Next { get; }
        public Position Position { get; }
        public bool IsExitNode => false;

        
    }
}
