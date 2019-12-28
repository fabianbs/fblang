/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Instructions {
    using Structure.Types;

    [Serializable]
    public class Declaration : StatementImpl {
        public Declaration(Position pos, IType type, Variable.Specifier specs, IEnumerable<(string, IExpression)> names, Visibility vis = Visibility.Private)
            : base(pos) {

            Variables = names.Select(x => new BasicVariable(pos, type, specs, x.Item1, null, vis) { DefaultValue = x.Item2 }).ToArray();
            //DefaultValues = names.Select(x => x.Item2).ToArray();
            Type = type;
            VariableSpecifiers = specs;
        }
        public Declaration(Position pos, IType type, Variable.Specifier specs, IEnumerable<string> names, IExpression defaultValue = null, Visibility vis = Visibility.Private)
            : base(pos) {
            Type = type;
            Variables = names.Select(x => new BasicVariable(pos, type, specs, x, null, vis) { DefaultValue = defaultValue }).ToArray();
            //DefaultValues = Enumerable.Repeat(defaultValue, Variables.Length).ToArray();
            VariableSpecifiers = specs;
        }
        protected Declaration(Position pos, IType type, Variable.Specifier specs, IVariable[] vars)
            : base(pos) {
            Type = type;
            VariableSpecifiers = specs;
            Variables = vars ?? Array.Empty<IVariable>();
        }
        /*protected Declaration(Position pos, IType type, Variable.Specifier specs, IVariable[] vars, IExpression[] defaultValues) : base(pos) {
            Type = type;
            VariableSpecifiers = specs;
            Variables = vars ?? Array.Empty<IVariable>();
            //DefaultValues = defaultValues;
        }*/
        public IEnumerable<IExpression> DefaultValues {
            get => Variables.Select(x => x.DefaultValue);
        }
        public IType Type {
            get;
        }
        public Variable.Specifier VariableSpecifiers {
            get;
        }
        public IVariable[] Variables {
            get;
        }

        //public override IRefEnumerator<IASTNode> GetEnumerator() => RefEnumerator.FromArray<IASTNode>(Variables);
        public override IEnumerable<IExpression> GetExpressions() => Variables.Select(x => x.DefaultValue).Where(x => x != null).Distinct();
        public override IEnumerable<IStatement> GetStatements() => Enumerable.Empty<IStatement>();

        public override IStatement ReplaceImpl(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            return new Declaration(Position,
                Type.Replace(genericActualParameter, curr, parent),
                VariableSpecifiers,
                Variables.Select(x => x.Replace(genericActualParameter, curr, parent)).ToArray()//,
                                                                                                //DefaultValues?.Select(x => x?.Replace(genericActualParameter, curr, parent)).ToArray()
            );
        }

        protected override bool TryReplaceMacroParametersImpl(MacroCallParameters args, out IStatement stmt) {
            bool changed = false;
            var nwVariables = Variables.Select(x => {
                if (x.DefaultValue != null && x.DefaultValue.TryReplaceMacroParameters(args, out var dflt)) {
                    changed = true;
                    if (dflt.Length != 1) {
                        "A variable cannot be initialized with a variable number of values".Report(Position.Concat(args.Position));
                    }
                    var nwVar = new BasicVariable(x.Position.Concat(args.Position), x.Type.IsTop() ? dflt.First().ReturnType : x.Type, x.VariableSpecifiers, x.Signature.Name, x.DefinedInType, x.Visibility) {
                        DefaultValue = dflt.FirstOrDefault()
                    };
                    args.VariableReplace[x] = nwVar;
                    return nwVar;
                }
                return x;
            }).ToArray();
            if (changed) {
                stmt = new Declaration(Position.Concat(args.Position),
                    Type.IsTop() ? nwVariables.Aggregate(Structure.Types.Type.Top, (acc, x) => Structure.Types.Type.MostSpecialCommonSuperType(acc, x.Type)) : Type,
                    VariableSpecifiers,
                    nwVariables//,
                               //nwDefaultValues
                );
                return true;
            }

            stmt = this;
            return false;
        }
        public override bool TryReplaceMacroParameters(MacroCallParameters args, out IStatement stmt) {
            var ret = base.TryReplaceMacroParameters(args, out stmt);
            foreach (var vr in (stmt as Declaration).Variables) {
                if (!args.Callee.LocalContext.Variables.ContainsKey(vr.Signature) && !args.contextStack.Peek().DefineVariable(vr).Get(out var fail)) {
                    if (fail == Contexts.CannotDefine.AlreadyExisting)
                        $"The variable {vr.Signature} is already defined in {args.contextStack.Peek().Name}".Report(vr.Position.Concat(args.Position));
                    else
                        $"The variable {vr.Signature} cannot be defined in {args.contextStack.Peek().Name}".Report(vr.Position.Concat(args.Position));
                }
            }
            return ret;
        }
    }
}
