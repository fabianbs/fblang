using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FBc {
    [Serializable]
    public class LambdaExpression : ExpressionImpl, IRangeScope {
        IType retTy;
        IReadOnlyCollection<LambdaCapture> captures = List.Empty<LambdaCapture>();
        IReadOnlyCollection<ICaptureExpression> captureAccesses = List.Empty<ICaptureExpression>();
        public LambdaExpression(Position pos, IType lambdaTy, IContext bodyCtx, string[] argNames)
            : base(pos) {
            retTy = lambdaTy ?? throw new ArgumentNullException(nameof(lambdaTy));
            Context = bodyCtx ?? throw new ArgumentNullException(nameof(bodyCtx));
            ArgumentNames = argNames ?? Array.Empty<string>();
        }
        public override IType ReturnType {
            get => retTy;
        }
        public IReadOnlyCollection<LambdaCapture> Captures {
            get => captures;
            set => captures = value ?? List.Empty<LambdaCapture>();
        }
        public IReadOnlyCollection<ICaptureExpression> CaptureAccesses {
            get => captureAccesses;
            set => captureAccesses = value ?? List.Empty<CaptureAccessExpression>();
        }
        public bool TryResetReturnType(IType nwRetTy) {
            if (nwRetTy is null)
                return false;
            if (FBModule.StaticSemantics.IsFunctional(nwRetTy, out _)) {
                retTy = nwRetTy;
                return true;
            }
            return false;
        }
        public string[] ArgumentNames {
            get;
        }
        public IVariable[] Arguments {
            get; set;
        }
        public InstructionBox Body {
            get;
            private set;
        } = new InstructionBox();

        public IContext Context {
            get;
        }

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return Body.HasValue ? Body.Instruction.GetEnumerator() : RefEnumerator.Empty<IASTNode>();
        }*/

        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new LambdaExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Context.Replace(genericActualParameter, curr, parent),
                ArgumentNames) {
                Body = Body.Replace(genericActualParameter, curr, parent)
            };
        }
        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {
            bool changed = false;
            var nwCapAcc = CaptureAccesses.Select(x => {
                if (x.TryReplaceMacroParameters(args, out var nwX)) {
                    changed = true;
                    // CaptureAccessExpression can only be instantiated inside of this module => nwX has always length 1
                    return nwX[0] as CaptureAccessExpression;
                }
                return x;
            }).ToArray();
            IReadOnlyCollection<LambdaCapture> nwCap ;
            if (changed) 
                nwCap = nwCapAcc.Select(x => (x as ICaptureExpression).Variable).Distinct().AsCollection().AsReadOnly();
            else
                nwCap = captures;

            if (Body.HasValue && Body.Instruction.TryReplaceMacroParameters(args, out var nwBody)) 
                changed = true;
            else
                nwBody = Body.Instruction;

            if (changed) {
                expr = new[] { new LambdaExpression(Position.Concat(args.Position),
                    ReturnType,
                    Context,
                    ArgumentNames
                ){
                    Captures = nwCap,
                    CaptureAccesses = nwCapAcc.AsReadOnly()
                } };
                (expr[0] as LambdaExpression).Body.Instruction = nwBody;
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
}
