using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace FBc {
    public interface ICaptureExpression : IExpression {
        LambdaCapture Variable {
            get;
        }
        IExpression ParentExpression {
            get;
        }
        IExpression Original {
            get;
        }
    }
    public interface IBaseCaptureExpression : ICaptureExpression {

    }
    [Serializable]
    public class CaptureAccessExpression : VariableAccessExpression, ICaptureExpression {
        internal CaptureAccessExpression(Position pos, IExpression original, LambdaCapture vr, IExpression parent = null)
            : base(pos, original.ReturnType, vr, parent) {
            Original = original;
        }

        public IExpression Original {
            get;
        }
        LambdaCapture ICaptureExpression.Variable {
            get => Variable as LambdaCapture;
        }
        // captures are always by-value and 'final immutable'
        public override bool IsLValue(IMethod met) => false;
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new CaptureAccessExpression(Position,
                Original.Replace(genericActualParameter, curr, parent),
                Variable.Replace(genericActualParameter, curr, parent) as LambdaCapture,
                ParentExpression?.Replace(genericActualParameter, curr, parent)
            );
        }
        public override bool TryReplaceMacroParameters(MacroCallParameters args, out IExpression[] expr) {
            if (IsEphemeral && ParentExpression != null && ParentExpression.TryReplaceMacroParameters(args, out var nwPar)) {
                if (nwPar.Length != 1) {
                    "A field can only have one parent".Report(ParentExpression.Position.Concat(args.Position));
                    nwPar = new[] { ParentExpression };
                }
                expr = new[] { new CaptureAccessExpression(Position.Concat(args.Position), null, new LambdaCapture(nwPar.First().ReturnType.Context.InstanceContext.VariableByName(VariableName)), nwPar.First()) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
    }
    [Serializable]
    public class BaseCaptureExpression : CaptureAccessExpression, IBaseCaptureExpression {
        internal BaseCaptureExpression(Position pos, IType retTy, LambdaCapture vr, IExpression parent = null)
            : base(pos, new BaseExpression(pos, retTy), vr, parent) {
        }
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BaseCaptureExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Variable.Replace(genericActualParameter, curr, parent) as LambdaCapture,
                ParentExpression?.Replace(genericActualParameter, curr, parent)
            );
        }
    }
    /*public class ThisCaptureExpression : ThisExpression, ICaptureExpression {
        
        public ThisCaptureExpression(Position pos, IType thisTy)
            : base(pos, thisTy) {
            Variable = LambdaCapture.This(thisTy);
        }

        public LambdaCapture Variable {
            get;
        }
        public IExpression ParentExpression {
            get => null;
        }
    }*/
    /*public class BaseCaptureExpression : ExpressionImpl, ICaptureExpression {
        IType thisTp;
        public BaseCaptureExpression(Position pos, IType thisTy)
            : base(pos) {
            Variable = LambdaCapture.Base(thisTy);
            thisTp = thisTy;
            ReturnType = (thisTy as IHierarchialType)?.SuperType
                ?? $"the super-keyword is invalid in this context, because {thisTy.Signature} has no supertype".ReportTypeError(pos);
        }
        public LambdaCapture Variable {
            get;
        }
        public IExpression ParentExpression {
            get => null;
        }
        public override IType ReturnType {
            get;
        }

        public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.Empty<IASTNode>();
        public override IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();
        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new BaseCaptureExpression(Position,
                thisTp.Replace(genericActualParameter, curr, parent)
            );
        }
    }*/
}
