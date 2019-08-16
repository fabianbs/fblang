using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class ForeachLoop : StatementImpl, IRangeScope {
        readonly IExpression[] rangeLoopVars;
        readonly IStatement[] bodyDecl;

        readonly bool declares;
        public ForeachLoop(Position pos, IExpression[] lvExprs, IExpression range, IStatement body, IContext headerScope) : base(pos) {
            rangeLoopVars = lvExprs.Prepend(range).ToArray();
            bodyDecl = new[] { body ?? Statement.Nop };
            declares = false;
            Context = headerScope;
        }
        public ForeachLoop(Position pos, IStatement decl, IExpression range, IStatement body, IContext headerScope) : base(pos) {
            rangeLoopVars = new[] { range };
            bodyDecl = new[] { body ?? Statement.Nop, decl };
            declares = true;
            Context = headerScope;
        }
        public bool TryGetDeclaration(out IStatement decl) {
            if (declares) {
                decl = bodyDecl[1];
                return true;
            }
            decl = Statement.Error;
            return false;
        }
        public bool HasDeclaration => declares;
        public bool TryGetLoopVariables(out ReadOnlySpan<IExpression> vars) {
            if (!declares) {
                vars = rangeLoopVars.AsSpan(1);
            }
            else
                vars = ReadOnlySpan<IExpression>.Empty;
            return false;
        }
        public IExpression Range => rangeLoopVars[0];
        public IStatement Body {
            get => bodyDecl[0];
            set => bodyDecl[0] = value ?? Statement.Nop;
        }
        public bool IsVectorizable {
            get; set;
        }
        public bool EnableVectorization {
            get; set;
        }
        public bool IsParallelizable {
            get; set;
        }
        public bool EnableParallelization {
            get; set;
        }
        public bool IsReduction {
            get; set;
        }
        public IMethod ReductionFunction {
            get; set;
        }
        public IExpression ReductionVariable {
            get; set;
        }
        public IContext Context {
            get;
        }

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return RefEnumerator.ConcatMany(RefEnumerable.FromArray<IASTNode>(rangeLoopVars), RefEnumerable.FromArray<IASTNode>(bodyDecl));
        }*/
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            if (Range.TryReplaceMacroParameters(args, out var nwRange)) {
                if (nwRange.Length != 1) {
                    "A range-based for-loop cannot iterate over a variable number of ranges".Report(Range.Position.Concat(args.Position));
                    nwRange = rangeLoopVars;
                }
                changed = true;
            }
            else
                nwRange = rangeLoopVars;
            if (Body.TryReplaceMacroParameters(args, out var nwBody))
                changed = true;
            else
                nwBody = Body;

            if (TryGetDeclaration(out var decl)) {
                if (decl.TryReplaceMacroParameters(args, out var nwDecl))
                    changed = true;
                else
                    nwDecl = decl;
                if (changed) {
                    stmt = new ForeachLoop(Position.Concat(args.Position), nwDecl, nwRange[0], nwBody, Context);
                    return true;
                }
            }
            else {
                var loopVars = rangeLoopVars.Skip(1);
                var nwLoopVars = loopVars.Select(x => {
                    if (x.TryReplaceMacroParameters(args, out var nwVr)) {
                        changed = true;
                        if (nwVr.Length != 1) {
                            "A range-based for-loop cannot have a variable number of loop-variables".Report(x.Position.Concat(args.Position));
                            return x;
                        }
                        return nwVr[0];
                    }
                    return x;
                }).ToArray();
                if (changed) {
                    stmt = new ForeachLoop(Position.Concat(args.Position), nwLoopVars, nwRange[0], nwBody, Context);
                    return true;
                }
            }
            stmt = this;
            return false;
        }
        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (TryGetDeclaration(out var decl)) {
                return new ForeachLoop(Position,
                    decl.Replace(genericActualParameter, curr, parent),
                    Range.Replace(genericActualParameter, curr, parent),
                    Body.Replace(genericActualParameter, curr, parent),
                    Context.Replace(genericActualParameter, curr, parent)
                ) {
                    IsVectorizable = IsVectorizable,
                    IsParallelizable = IsParallelizable,
                    EnableParallelization = EnableParallelization,
                    EnableVectorization = EnableVectorization,
                    IsReduction = IsReduction,
                    ReductionFunction = ReductionFunction?.Replace(genericActualParameter, curr, parent),
                    ReductionVariable = ReductionVariable?.Replace(genericActualParameter, curr, parent)
                };
            }
            else {
                TryGetLoopVariables(out var vrs);
                return new ForeachLoop(Position,
                    vrs.Select(x => (x as IExpression).Replace(genericActualParameter, curr, parent)).ToArray(),
                    Range.Replace(genericActualParameter, curr, parent),
                    Body.Replace(genericActualParameter, curr, parent),
                    Context.Replace(genericActualParameter, curr, parent)
                ) {
                    IsVectorizable = IsVectorizable,
                    IsParallelizable = IsParallelizable,
                    EnableParallelization = EnableParallelization,
                    EnableVectorization = EnableVectorization,
                    IsReduction = IsReduction,
                    ReductionFunction = ReductionFunction?.Replace(genericActualParameter, curr, parent),
                    ReductionVariable = ReductionVariable?.Replace(genericActualParameter, curr, parent)
                };
            }
        }

        public override IEnumerable<IStatement> GetStatements() => bodyDecl;
        public override IEnumerable<IExpression> GetExpressions() => rangeLoopVars;
    }
}
