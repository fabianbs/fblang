using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    [Serializable]
    class ConcurrentForLoop : ExpressionImpl {
        public ConcurrentForLoop(Position pos, IExpression range, LambdaExpression body, Declaration args) : base(pos) {
            Range = range;
            Body = body;
            Arguments = args;
        }
        public Declaration Arguments {
            get;
        }
        public override IType ReturnType {
            get => PrimitiveType.Void.AsAwaitable();
        }
        public IExpression Range {
            get;
        }
        public LambdaExpression Body {
            get;
        }
        public bool EnableVectorization {
            get; set;
        }
        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() {
            yield return Range;
            yield return Body;
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new ConcurrentForLoop(Position,
                 Range.Replace(genericActualParameter, curr, parent),
                 Body.Replace(genericActualParameter, curr, parent) as LambdaExpression,
                 Arguments.Replace(genericActualParameter, curr, parent) as Declaration
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            if (Range.TryReplaceMacroParameters(args, out var nwRange)) {
                changed = true;
                if (nwRange.Length != 1) {
                    "A concurrent for-loop cannot iterate over a variable number of ranges".Report(Range.Position.Concat(args.Position));
                    nwRange = new[] { Range };
                }
            }
            else
                nwRange = new[] { Range };

            if (Body.TryReplaceMacroParameters(args, out var nwBody)) {
                changed = true;
                if (nwBody.Length != 1) {// Sollte nicht vorkommen
                    "A concurrent for-loop cannot have a variable number of bodies".Report(Body.Position.Concat(args.Position));
                    nwBody = new[] { Body };
                }
            }
            else
                nwBody = new[] { Body };

            if (Arguments.TryReplaceMacroParameters(args, out var nwArgs))
                changed = true;
            else
                nwArgs = Arguments;

            if (changed) {
                if (!args.Semantics.IsIterable(Range.ReturnType, (nwArgs as Declaration).Type)) {
                    $"The expression-parameter must be substituted such that the object, over which the concurrent for-loop iterates, is iterable over the arguments. {Range.ReturnType} is not iterable over {(nwArgs as Declaration).Type}".Report(Position.Concat(args.Position));
                }
                expr = new[]{ new ConcurrentForLoop(Position.Concat(args.Position),
                    nwRange[0],
                    nwBody[0] as LambdaExpression,
                    nwArgs as Declaration
                ) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
