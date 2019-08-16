using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FBc {
    static class TypeHelper {
        public static bool OverloadsOperator(this IType tp, OverloadableOperator op, out IEnumerable<IDeclaredMethod> overloads, 
                                            SimpleMethodContext.VisibleMembers vis = SimpleMethodContext.VisibleMembers.Both) {
            string suffix;
            bool unary = false, binary = false, ternary = false;
            switch (op) {
                case OverloadableOperator.Add:
                    binary = true;
                    suffix = "+";
                    break;
                case OverloadableOperator.Sub:
                    binary = true;
                    suffix = "-";
                    break;
                case OverloadableOperator.Mul:
                    binary = true;
                    suffix = "*";
                    break;
                case OverloadableOperator.Div:
                    binary = true;
                    suffix = "/";
                    break;
                case OverloadableOperator.Rem:
                    binary = true;
                    suffix = "%";
                    break;
                case OverloadableOperator.LShift:
                    binary = true;
                    suffix = "<<";
                    break;
                case OverloadableOperator.SRShift:
                    binary = true;
                    suffix = ">>";
                    break;
                case OverloadableOperator.URShift:
                    binary = true;
                    suffix = ">>>";
                    break;
                case OverloadableOperator.FunctionCall:
                    suffix = "()";
                    break;
                case OverloadableOperator.Indexer:
                    suffix = "[]";
                    break;
                case OverloadableOperator.RangedIndexer:
                    ternary = true;
                    suffix = "[:]";
                    break;
                case OverloadableOperator.At:
                    suffix = "@";
                    break;
                case OverloadableOperator.Deconstruct:
                    suffix = "<-";
                    break;
                case OverloadableOperator.LessThan:
                    binary = true;
                    suffix = "<";
                    break;
                case OverloadableOperator.LessThanOrEqual:
                    binary = true;
                    suffix = "<=";
                    break;
                case OverloadableOperator.GreaterThan:
                    binary = true;
                    suffix = ">";
                    break;
                case OverloadableOperator.GreaterThanOrEqual:
                    binary = true;
                    suffix = ">=";
                    break;
                case OverloadableOperator.Equal:
                    binary = true;
                    suffix = "==";
                    break;
                case OverloadableOperator.Unequal:
                    binary = true;
                    suffix = "!=";
                    break;
                case OverloadableOperator.Not:
                    unary = true;
                    suffix = "~";
                    break;
                case OverloadableOperator.LNot:
                    unary = true;
                    suffix = "!";
                    break;
                case OverloadableOperator.Neg:
                    unary = true;
                    suffix = "-";
                    break;
                case OverloadableOperator.Increment:
                    unary = true;
                    suffix = "++";
                    break;
                case OverloadableOperator.Decrement:
                    unary = true;
                    suffix = "--";
                    break;
                case OverloadableOperator.And:
                    binary = true;
                    suffix = "&";
                    break;
                case OverloadableOperator.Or:
                    binary = true;
                    suffix = "|";
                    break;
                case OverloadableOperator.Xor:
                    binary = true;
                    suffix = "^";
                    break;
                case OverloadableOperator.Bool:
                    unary = true;
                    suffix = "bool";
                    break;
                case OverloadableOperator.Default:
                    suffix = "default";
                    break;
                default:
                    throw new ArgumentException();
            }

            var name = "operator " + suffix;
            IEnumerable<IDeclaredMethod> inst, stat;
            if (vis.HasFlag(SimpleMethodContext.VisibleMembers.Instance)) {

                IEnumerable<IDeclaredMethod> instMets = tp.Context.InstanceContext.MethodsByName(name);
                IEnumerable<IDeclaredMethod> instTms = tp.Context.InstanceContext.MethodTemplatesByName(name);

                inst = instMets.Concat(instTms).Where(x => x.Specifiers.HasFlag(Method.Specifier.OperatorOverload));
                if (unary)
                    inst = inst.Where(x => x.Arguments.Length == 0);
                else if (binary)
                    inst = inst.Where(x => x.Arguments.Length == 1);
                else if (ternary)
                    inst = inst.Where(x => x.Arguments.Length == 2);
            }
            else
                inst = Enumerable.Empty<IDeclaredMethod>();
            if (vis.HasFlag(SimpleMethodContext.VisibleMembers.Static)) {
                IEnumerable<IDeclaredMethod> statMets = tp.Context.StaticContext.MethodsByName(name);
                IEnumerable<IDeclaredMethod> statTms = tp.Context.StaticContext.MethodTemplatesByName(name);

                stat = statMets.Concat(statTms).Where(x => x.Specifiers.HasFlag(Method.Specifier.OperatorOverload));
                if (unary)
                    stat = stat.Where(x => x.Arguments.Length == 1);
                else if (binary)
                    stat = stat.Where(x => x.Arguments.Length == 2);
                else if (ternary)
                    stat = stat.Where(x => x.Arguments.Length == 3);
            }
            else
                stat = Enumerable.Empty<IDeclaredMethod>();

            overloads = inst.Concat(stat);
            return overloads.Any();
        }
    }
}
