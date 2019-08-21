/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;

namespace CompilerInfrastructure.Structure.Macros {
    public class MacroFunction : IDeclaredMethodBase, IReplaceableStructureElement<MacroFunction>, ISigned<StringSignature> {
        public MacroFunction(Position pos, string name, Visibility vis, IContext nestedIn, IReadOnlyDictionary<string, ExpressionParameter> args, ExpressionParameterPack varargs, StatementParameter stmt) {
            Position = pos;
            Name = name;
            NestedIn = nestedIn;
            NamedArguments = args ?? Map.Empty<string, ExpressionParameter>();
            VarArgs = varargs;
            CapturedStatement = stmt;
            Visibility = vis;
            LocalContext = new BasicContext(nestedIn.Module, Context.DefiningRules.Variables, true, pos);
        }
        protected MacroFunction(MacroFunction other, GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            Position = other.Position;
            Name = other.Name;
            NestedIn = other.NestedIn.Replace(genericActualParameter, curr, parent);
            NamedArguments = other.NamedArguments;
            VarArgs = other.VarArgs;
            CapturedStatement = other.CapturedStatement;
            Visibility = other.Visibility;
            Body = other.Body.Replace(genericActualParameter, curr, parent);
            LocalContext = other.LocalContext.Replace(genericActualParameter, curr, parent);
        }
        public string Name {
            get;
        }
        
        public IReadOnlyDictionary<string, ExpressionParameter> NamedArguments {
            get;
        }
        public ICollection<ExpressionParameter> Arguments {
            get => NamedArguments.Values.AsCollection(NamedArguments.Count);
        }
        public ExpressionParameterPack VarArgs {
            get;
        }
        public bool HasVarArgs => VarArgs != null;
        public StatementParameter CapturedStatement {
            get;
        }
        public bool HasCapturedStatement => CapturedStatement != null;
        public IContext NestedIn {
            get;
        }
        public InstructionBox Body { get; } = new InstructionBox();

        public Position Position {
            get;
        }
        public Visibility Visibility {
            get; set;
        }
        public IContext LocalContext {
            get;
        }
        StringSignature ISigned<StringSignature>.Signature {
            get => Name;
        }
        ISignature ISigned.Signature => new StringSignature(Name);
        public bool Finished {
            get;
            private set;
        } = false;

        readonly Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, MacroFunction> genericCache
            = new Dictionary<GenericParameterMap<IGenericParameter, ITypeOrLiteral>, MacroFunction>();
        public MacroFunction Replace(GenericParameterMap<IGenericParameter, ITypeOrLiteral> genericActualParameter, IContext curr, IContext parent) {
            if (!genericCache.TryGetValue(genericActualParameter, out var ret)) {
                ret = new MacroFunction(this, genericActualParameter, curr, parent);
                if (!genericCache.TryAdd(genericActualParameter, ret))
                    ret = genericCache[genericActualParameter];
            }
            return ret;
        }
        public bool Finish() {
            if (!Finished) {
                Finished = true;
                return true;
            }
            return false;
        }
    }
}
