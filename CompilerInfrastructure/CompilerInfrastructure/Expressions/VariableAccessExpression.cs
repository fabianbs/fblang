using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Expressions {
    [Serializable]
    public class VariableAccessExpression : ExpressionImpl, IMutableDataReadingExpression, SwitchStatement.IPattern, ICompileTimeEvaluable {
        readonly IExpression[] parEx = new IExpression[1];
        readonly IType returnTy;
        //TODO remove retTy
        public VariableAccessExpression(Position pos, IType retTy, IVariable vr, IExpression parent = null) : base(pos) {
            parEx[0] = parent;

            Variable = vr ?? "The variable for a variable-access must not be null".Report(pos, CompilerInfrastructure.Variable.Error);
            if (parent is null && !Variable.IsLocalVariable()) {
                $"The access of the variable {vr.Signature} must have a parent-expression".Report(pos);
            }
            IsEphemeral = false;
            VariableName = Variable.Signature.Name;
        }
        public VariableAccessExpression(Position pos, IType expectedReturnType, string variableName, IExpression parent) : base(pos) {
            parEx[0] = parent ?? "A field cannot be inferred without a parent-expression".Report(pos, Expression.Error);
            Variable = null;
            //returnTy = expectedReturnType ?? Type.Top;
            VariableName = variableName ?? throw new ArgumentNullException(nameof(variableName));
            IsEphemeral = parent.ReturnType.IsTop() || (Variable = parent.ReturnType.Context.InstanceContext.VariableByName(variableName)) is null;
            if (!IsEphemeral && returnTy.IsTop()) {
                returnTy = Variable.Type;
            }
        }
        public string VariableName {
            get;
        }
        public bool IsEphemeral {
            get;
        }
        public override IType ReturnType {
            get => Variable.Type.TryCast<ByRefType>(out var brt) ? brt.UnderlyingType : Variable.Type;
        }
        public IExpression ParentExpression => parEx[0];
        public IVariable Variable {
            get;
        }
        public bool ReadsMutableData {
            get => !Variable.IsLocalVariable() && (!Variable.IsFinal() || !Variable.Type.IsConstant());
        }
        public IType BaseType => ReturnType;
        public bool TypecheckNecessary => false;

        public bool IsCompileTimeEvaluable => Variable.IsStatic() && Variable.IsFinal() && Variable.DefaultValue != null && Variable.DefaultValue.IsCompileTimeEvaluable();

        public override bool IsLValue(IMethod met) {
            if (Variable.IsFinal()) {
                return ParentExpression is ThisExpression && met != null && met.IsConstructor();
            }
            return true;
        }

        /*public override IRefEnumerator<IASTNode> GetEnumerator() {
            return ParentExpression != null ? RefEnumerator.FromArray<IASTNode>(parEx) : RefEnumerator.Empty<IASTNode>();
        }*/

        public override IEnumerable<IExpression> GetExpressions() {
            return ParentExpression is null ? Enumerable.Empty<IExpression>() : parEx;
        }

        protected override IExpression ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new VariableAccessExpression(Position,
                ReturnType.Replace(genericActualParameter, curr, parent),
                Variable.Replace(genericActualParameter, curr, parent),
                ParentExpression?.Replace(genericActualParameter, curr, parent)
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IExpression[] expr) {

            if ((IsEphemeral || ParentExpression is ThisExpression || ParentExpression is VariableAccessExpression) && ParentExpression != null && ParentExpression.TryReplaceMacroParameters(args, out var nwPar)) {
                if (nwPar.Length != 1) {
                    "A field can only have one parent".Report(ParentExpression.Position.Concat(args.Position));
                    nwPar = new[] { ParentExpression };
                }
                expr = new[] { new VariableAccessExpression(Position.Concat(args.Position), null, nwPar.First().ReturnType.Context.InstanceContext.VariableByName(VariableName), nwPar.First()) };
                return true;
            }
            else if (args.VariableReplace.TryGetValue(Variable, out var nwVar)) {
                expr = new[] { new VariableAccessExpression(Position.Concat(args.Position), null, nwVar) };
                return true;
            }
            expr = new[] { this };
            return false;
        }
        public override string ToString() {
            if (ParentExpression != null)
                return ParentExpression + "." + Variable.Signature.Name;
            else
                return Variable.Signature.Name;
        }

        public bool TryReplaceMacroParameters(MacroCallParameters args, out SwitchStatement.IPattern ret) {
            if (!TryReplaceMacroParameters(args, out IExpression[] expr)) {
                ret = new SwitchStatement.Wildcard();
                return false;
            }
            ret = (VariableAccessExpression) expr[0];
            return true;
        }
        SwitchStatement.IPattern SwitchStatement.IPattern.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return (VariableAccessExpression) Replace(genericActualParameter, curr, parent);
        }
        public IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();
        public ILiteral Evaluate(ref EvaluationContext context) {
            if (!IsCompileTimeEvaluable)
                return null;
            return context.AssertFact(this, Variable.DefaultValue.Evaluate(ref context));
        }
    }
}
