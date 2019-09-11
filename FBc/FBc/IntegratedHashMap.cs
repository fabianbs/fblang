﻿using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Macros;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace FBc {
    class IntegratedHashMap : ClassTypeTemplate {
        static IntegratedHashMap instance = null;
        readonly GenericTypeParameter T, U;
        private IntegratedHashMap(Module mod, IReadOnlyList<IGenericParameter> genArgs)
            : base(default, mod, "::HashMap", genArgs) {
            T = (GenericTypeParameter) genArgs[0];
            U = (GenericTypeParameter) genArgs[1];
        }
        private void InitializeInterface() {
            // operator[]: U -> T&
            Context.DefineMethod(new BasicMethod(default, "operator []", Visibility.Internal, T.AsByRef(), new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null)
            }) { Specifiers = Method.Specifier.OperatorOverload | Method.Specifier.UniqueThis | Method.Specifier.SideEffectFree | Method.Specifier.Builtin, NestedIn = Context });
            // operator[]: U, T
            Context.DefineMethod(new BasicMethod(default, "operator []", Visibility.Internal, T.AsByRef(), new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null),
                new BasicVariable(default, T, Variable.Specifier.LocalVariable,"value",null)
            }) { Specifiers = Method.Specifier.OperatorOverload | Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });
            // tryGetValue: U, T& -> bool
            Context.DefineMethod(new BasicMethod(default, "tryGetValue", Visibility.Internal, PrimitiveType.Bool, new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null),
                new BasicVariable(default, T.AsByRef(), Variable.Specifier.LocalVariable,"out_value",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });
            // getOrElse: U, T -> T
            Context.DefineMethod(new BasicMethod(default, "getOrElse", Visibility.Internal, T, new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null),
                new BasicVariable(default, T, Variable.Specifier.LocalVariable,"orElse",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });
            // contains -> bool
            Context.DefineMethod(new BasicMethod(default, "contains", Visibility.Internal, PrimitiveType.Bool, new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });
            // count -> zint
            Context.DefineMethod(new BasicMethod(default, "count", Visibility.Internal, PrimitiveType.SizeT, Array.Empty<IVariable>()
                ) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });

            // insert: U, T, bool -> bool
            Context.DefineMethod(new BasicMethod(default, "insert", Visibility.Internal, PrimitiveType.Bool, new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null),
                new BasicVariable(default, T, Variable.Specifier.LocalVariable,"value",null),
                new BasicVariable(default, PrimitiveType.Bool, Variable.Specifier.LocalVariable,"replace",null),
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });

            // insertZip: iterable U, iterable T -> bool
            Context.DefineMethod(new BasicMethod(default, "insertZip", Visibility.Internal, PrimitiveType.SizeT, new[] {
                new BasicVariable(default, IterableType.Get(U), Variable.Specifier.LocalVariable,"keys",null),
                new BasicVariable(default, IterableType.Get(T), Variable.Specifier.LocalVariable,"values",null),
                new BasicVariable(default, PrimitiveType.Bool, Variable.Specifier.LocalVariable,"replace",null),
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });
            // remove: U -> bool
            Context.DefineMethod(new BasicMethod(default, "remove", Visibility.Internal, PrimitiveType.Bool, new[] {
                new BasicVariable(default, U, Variable.Specifier.LocalVariable,"key",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });
            // removeAll: Func<U, bool> -> bool
            Context.DefineMethod(new BasicMethod(default, "removeAll", Visibility.Internal, PrimitiveType.SizeT, new[] {
                new BasicVariable(default, new FunctionType(default,Context.Module,":func",PrimitiveType.Bool,new[]{ U}, Visibility.Public), Variable.Specifier.LocalVariable,"predicate",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });

            //TODO tuples, optionals

            // keys -> iterable U
            Context.DefineMethod(new BasicMethod(default, "keys", Visibility.Internal, IterableType.Get(U), Array.Empty<IVariable>()
                ) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });
            // values -> iterable T
            Context.DefineMethod(new BasicMethod(default, "values", Visibility.Internal, IterableType.Get(T), Array.Empty<IVariable>()
                ) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.SideEffectFree, NestedIn = Context });

            // clear:
            Context.DefineMethod(new BasicMethod(default, "count", Visibility.Internal, PrimitiveType.Void, Array.Empty<IVariable>()
                ) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin, NestedIn = Context });

            // Constructors: 
            // ctor:
            Context.DefineMethod(new BasicMethod(default, "ctor", Visibility.Internal, PrimitiveType.Void, Array.Empty<IVariable>()
                ) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.Constructor, NestedIn = Context });
            // ctor: zint
            Context.DefineMethod(new BasicMethod(default, "ctor", Visibility.Internal, PrimitiveType.Void, new[] {
                new BasicVariable(default, PrimitiveType.SizeT, Variable.Specifier.LocalVariable,"initialCapacity",null)
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.Constructor, NestedIn = Context });

            //TODO 'resize: -> bool' method
        }

        private void InitializeMacros() {
            IMethod tryGetNext;
            Context.DefineMethod(tryGetNext = new BasicMethod(default, "::tryGetNext", Visibility.Private, PrimitiveType.Bool, new[] {
                new BasicVariable(default, PrimitiveType.SizeT.AsByRef(), Variable.Specifier.LocalVariable,"state",null),
                new BasicVariable(default, U.AsByRef(), Variable.Specifier.LocalVariable,"ky",null),
                new BasicVariable(default, T.AsByRef(), Variable.Specifier.LocalVariable,"val",null),
            }) { Specifiers = Method.Specifier.UniqueThis | Method.Specifier.Builtin | Method.Specifier.Readonly, NestedIn = Context });
            var forEach = new MacroFunction(default, "forEach",Visibility.Internal,Context, Dictionary.Empty<string,ExpressionParameter>(),null,new StatementParameter(default, "callBack"));
            
            Context.DefineMacro(forEach);
            IVariable state, key, value;
            var _this = new ThisExpression(default, BuildType());
            var body = Vector<IStatement>.Reserve(4);
            body.Add(new Declaration(default, PrimitiveType.SizeT, Variable.Specifier.LocalVariable, new[] { "%state" }, new SizeTLiteral(default, 0)));
            state = (body.Back() as Declaration).Variables[0];
            body.Add(new Declaration(default, U, Variable.Specifier.LocalVariable, new[] { "%key" }, vis: Visibility.Public));
            key = (body.Back() as Declaration).Variables[0];
            body.Add(new Declaration(default, T, Variable.Specifier.LocalVariable, new[] { "%value" }, vis: Visibility.Public));
            value = (body.Back() as Declaration).Variables[0];

            forEach.LocalContext.DefineVariable(key);
            forEach.LocalContext.DefineVariable(value);

            body.Add(new WhileLoop(default, new CallExpression(default, PrimitiveType.Bool, tryGetNext, _this, new[] {
                new VariableAccessExpression(default,null, state),
                new VariableAccessExpression(default, null, key),
                new VariableAccessExpression(default, null, value)
            }), new StatementParameterAccess(default, forEach.CapturedStatement)));

            forEach.Body.Instruction = new BlockStatement(default, body.AsArray(), Context);

        }

        public static ClassTypeTemplate GetOrCreateIntegratedHashMap(Module mod) {
            if (instance is null) {
                var ttcx = SimpleTypeTemplateContext.NewScope(mod);
                var keyTy = new GenericTypeParameter(default, ttcx, "U");
                var valTy = new GenericTypeParameter(default, ttcx, "T");
                ttcx.DefineType(valTy);
                ttcx.DefineType(keyTy);
                instance = new IntegratedHashMap(mod, new[] { valTy, keyTy }) {
                    Context = ttcx,
                    Visibility = Visibility.Public,
                    TypeSpecifiers = CompilerInfrastructure.Type.Specifier.NoInheritance
                };
                ttcx.TypeTemplate = instance;

                instance.InitializeInterface();

                ttcx.Type = instance.BuildType();
                instance.InitializeMacros();
            }
            return instance;
        }
    }
}
