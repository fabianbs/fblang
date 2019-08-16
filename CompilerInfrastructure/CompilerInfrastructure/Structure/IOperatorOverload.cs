using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    public interface IOperatorOverload : IMethod {
        OverloadableOperator Operator {
            get;
        }
    }
    [Serializable]
    public enum OverloadableOperator {
        Add, Sub,
        Mul, Div, Rem,
        LShift, SRShift, URShift,
        FunctionCall, Indexer, RangedIndexer, At,
        Deconstruct,
        LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
        Equal, Unequal,
        Not, LNot, Neg,
        Increment, Decrement,
        And, Or, Xor,
        Bool, String, Default
    }
    public static class OverloadableOperatorHelper {
        public static bool IsUnary(this OverloadableOperator op) {
            switch (op) {
                case OverloadableOperator.Not:
                case OverloadableOperator.LNot:
                case OverloadableOperator.Neg:
                case OverloadableOperator.Increment:
                case OverloadableOperator.Decrement:
                case OverloadableOperator.Bool:
                    return true;
                default:
                    return false;
            }
        }
        public static bool IsTernary(this OverloadableOperator op) {
            return op == OverloadableOperator.RangedIndexer;
        }
        public static bool IsToString(this OverloadableOperator op) {
            return op == OverloadableOperator.String;
        }
        public static bool IsBinary(this OverloadableOperator op) {
            switch (op) {
                case OverloadableOperator.Add:
                case OverloadableOperator.Sub:
                case OverloadableOperator.Mul:
                case OverloadableOperator.Div:
                case OverloadableOperator.Rem:
                case OverloadableOperator.LShift:
                case OverloadableOperator.SRShift:
                case OverloadableOperator.URShift:
                case OverloadableOperator.LessThan:
                case OverloadableOperator.LessThanOrEqual:
                case OverloadableOperator.GreaterThan:
                case OverloadableOperator.GreaterThanOrEqual:
                case OverloadableOperator.Equal:
                case OverloadableOperator.Unequal:
                case OverloadableOperator.And:
                case OverloadableOperator.Or:
                case OverloadableOperator.Xor:
                case OverloadableOperator.Bool:
                    return true;
                default:
                    return false;

            }

        }
        public static string OperatorName(this OverloadableOperator op) {
            switch (op) {
                case OverloadableOperator.Add:
                    return "+";
                case OverloadableOperator.Sub:
                case OverloadableOperator.Neg:
                    return "-";
                case OverloadableOperator.Mul:
                    return "*";
                case OverloadableOperator.Div:
                    return "/";
                case OverloadableOperator.Rem:
                    return "%";
                case OverloadableOperator.LShift:
                    return "<<";
                case OverloadableOperator.SRShift:
                    return ">>";
                case OverloadableOperator.URShift:
                    return ">>>";
                case OverloadableOperator.FunctionCall:
                    return "()";
                case OverloadableOperator.Indexer:
                    return "[]";
                case OverloadableOperator.RangedIndexer:
                    return "[:]";
                case OverloadableOperator.At:
                    return "@";
                case OverloadableOperator.Deconstruct:
                    return "<-";
                case OverloadableOperator.LessThan:
                    return "<";
                case OverloadableOperator.LessThanOrEqual:
                    return "<=";
                case OverloadableOperator.GreaterThan:
                    return ">";
                case OverloadableOperator.GreaterThanOrEqual:
                    return ">=";
                case OverloadableOperator.Equal:
                    return "==";
                case OverloadableOperator.Unequal:
                    return "!=";
                case OverloadableOperator.Not:
                    return "~";
                case OverloadableOperator.LNot:
                    return "!";
                case OverloadableOperator.Increment:
                    return "++";
                case OverloadableOperator.Decrement:
                    return "--";
                case OverloadableOperator.And:
                    return "&";
                case OverloadableOperator.Or:
                    return "|";
                case OverloadableOperator.Xor:
                    return "^";
                case OverloadableOperator.Bool:
                    return "bool";
                case OverloadableOperator.String:
                    return "string";
                case OverloadableOperator.Default:
                    return "default";
                default:
                    throw new ArgumentException();
            }
        }
    }
}
