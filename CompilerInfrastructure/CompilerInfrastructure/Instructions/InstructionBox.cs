using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Instructions {
    [Serializable]
    public class InstructionBox : IReplaceableStructureElement<InstructionBox>, IStatement {
        public virtual IStatement Instruction {
            get;
            set;
        }
        public virtual InstructionBox Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new InstructionBoxReplace(this, new ContextReplace.ReplaceParameters { genericActualParameter = genericActualParameter, curr = curr, parent = parent });
        }

        public bool TryReplaceMacroParameters(MacroCallParameters args, out IStatement stmt) {
            if (HasValue)
                return Instruction.TryReplaceMacroParameters(args, out stmt);
            else {
                stmt = this;
                return false;
            }
        }

        /*public IRefEnumerator<IASTNode> GetEnumerator() =>HasValue? Instruction.GetEnumerator():RefEnumerator.Empty<IASTNode>();
        IEnumerator<IASTNode> IEnumerable<IASTNode>.GetEnumerator() =>GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
        IStatement IReplaceableStructureElement<IStatement, IGenericParameter, ITypeOrLiteral>.Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return Replace(genericActualParameter, curr, parent);
        }

        public IEnumerable<IStatement> GetStatements() {
            if (HasValue)
                yield return Instruction;
        }
        public IEnumerable<IExpression> GetExpressions() => Enumerable.Empty<IExpression>();

        public bool HasValue => Instruction != null;
        static InstructionBox error;
        public static InstructionBox Error {
            get {
                if (error is null)
                    error = new InstructionBox { Instruction = Statement.Error };
                return error;
            }
        }

        public Position Position => Instruction?.Position ?? default;
    }
    [Serializable]
    class InstructionBoxReplace : InstructionBox {
        ContextReplace.ReplaceParameters args;
        readonly InstructionBox underlying;
        IStatement val = null;
        public InstructionBoxReplace(InstructionBox _underlying, ContextReplace.ReplaceParameters _args) {
            underlying = _underlying;
            args = _args;
        }
        public override InstructionBox Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            var _args = new ContextReplace.ReplaceParameters { genericActualParameter = genericActualParameter, curr = curr, parent = parent };
            if (args == _args)
                return this;
            return new InstructionBoxReplace(this, _args);
        }
        public override IStatement Instruction {
            get {
                if (val is null) {
                    val = underlying.Instruction?.Replace(args.genericActualParameter, args.curr, args.parent);
                }
                return val;
            }
            set => val = value;
        }
    }
}
