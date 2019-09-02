/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Instructions;
using CompilerInfrastructure.Semantics;
using CompilerInfrastructure.Structure;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Structure.Types.Generic;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Type = CompilerInfrastructure.Type;

namespace FBc {
    [Serializable]
    class FBModule : Module {
        readonly Dictionary<EquatableLList<string>, IContext> namespaces = new Dictionary<EquatableLList<string>, IContext>();
        readonly Dictionary<IContext, EquatableLList<string>> namespaceCtxs = new Dictionary<IContext, EquatableLList<string>>();
        readonly Dictionary<string, IMethod> internalMethods = new Dictionary<string, IMethod>();
        public FBModule(string moduleName = "the global context", string filename = "") : base(moduleName, filename) {
            var cprintln = new BasicMethod(("", 0, 0), "cprintln", Visibility.Public, PrimitiveType.Void, PrimitiveType.String) {
                ReturnType = PrimitiveType.Void,
                Arguments = new[] { new BasicVariable(("", 0, 0), PrimitiveType.String, Variable.Specifier.LocalVariable, "msg", null) },
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            var cprintln_void = new BasicMethod(("", 0, 0), "cprintln", Visibility.Public, PrimitiveType.Void, List.Empty<IVariable>(), internalName: "cprintnwln") {
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            var cprint = new BasicMethod(("", 0, 0), "cprint", Visibility.Public, PrimitiveType.Void, PrimitiveType.String) {
                ReturnType = PrimitiveType.Void,
                Arguments = new[] { new BasicVariable(("", 0, 0), PrimitiveType.String, Variable.Specifier.LocalVariable, "msg", null) },
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            var delay = new BasicMethod(("", 0, 0), "delay", Visibility.Public, PrimitiveType.Void.AsAwaitable(), PrimitiveType.UInt) {
                ReturnType = PrimitiveType.Void.AsAwaitable(),
                Arguments = new[] { new BasicVariable(("", 0, 0), PrimitiveType.UInt, Variable.Specifier.LocalVariable, "millis", null) },
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            var sleep = new BasicMethod(("", 0, 0), "sleep", Visibility.Public, PrimitiveType.Void, new[] {
                    new BasicVariable(("",0, 0), PrimitiveType.UInt, Variable.Specifier.LocalVariable, "millis", null)
                }, internalName: "thread_sleep") {
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            var randomUInt = new BasicMethod(default, "randomUInt", Visibility.Public, PrimitiveType.UInt, List.Empty<IVariable>()) {
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };
            var randomUIntRange = new BasicMethod(default, "randomUInt", Visibility.Public, PrimitiveType.UInt, new[] {
                    new BasicVariable(default, PrimitiveType.UInt, Variable.Specifier.LocalVariable,"min",null),
                    new BasicVariable(default, PrimitiveType.UInt, Variable.Specifier.LocalVariable,"max",null)
                }, internalName: "randomUIntRange") {
                Specifiers = Method.Specifier.Internal | Method.Specifier.Static,
                NestedIn = this
            };

            AddInternalMethods(
                cprintln,
                cprintln_void,
                cprint,
                delay,
                sleep,
                randomUInt,
                randomUIntRange
            );

            DefineMethod(cprintln);
            DefineMethod(cprintln_void);
            DefineMethod(cprint);
            DefineMethod(delay);
        }
        void AddInternalMethods(params IMethod[] intnls) {
            foreach (var x in intnls) {
                internalMethods.Add(x.Signature.InternalName, x);
            }
        }
        public bool TryGetInternalMethod(string name, out IMethod met) {
            return internalMethods.TryGetValue(name, out met);
        }
        public byte WarningLevel {
            get; set;
        }
        public IReadOnlyDictionary<EquatableLList<string>, IContext> Namespaces {
            get => namespaces;
        }
        [NonSerialized]
        private FBSemantics semantics = FBSemantics.Instance;

        public FBSemantics Semantics {
            get => semantics;
        }
        public static FBSemantics StaticSemantics {
            get;
        } = FBSemantics.Instance;
        public bool DefineNamespace(string name, EquatableLList<string> parent, IContext ctx) {
            var node = new EquatableLList<string>(name, parent);
            return namespaces.TryAdd(node, ctx)
                && namespaceCtxs.TryAdd(ctx, node);
        }
        public bool DefineNamespace(EquatableLList<string> nsQualifier, IContext ctx) {
            return namespaces.TryAdd(nsQualifier, ctx)
                && namespaceCtxs.TryAdd(ctx, nsQualifier);
        }
        public IContext NamespaceContext(EquatableLList<string> nsQualifier) {
            if (namespaces.TryGetValue(nsQualifier, out var ctx)) {
                return ctx;
            }
            return null;
        }
        public bool IsNamespaceContext(IContext ctx, out EquatableLList<string> nsQualifier) {
            return namespaceCtxs.TryGetValue(ctx, out nsQualifier);
        }
        public IMethod GetConstructor(Position pos, IType objectType, IExpression[] args, ErrorBuffer err) {
            if (objectType is null || objectType.IsError())
                return Method.Error;
            if (objectType.IsAbstract()) {
                "An abstract class cannot be instantiated".Report(pos);
                return Method.Error;
            }
            var mets = (objectType.Context.InstanceContext.LocalContext.MethodsByName("ctor") as IEnumerable<IDeclaredMethod>).Concat(objectType.Context.InstanceContext.LocalContext.MethodTemplatesByName("ctor"));
            var ret = Semantics.BestFittingMethod(pos, mets, args.Select(x => x.MinimalType()).AsCollection(args.Length), PrimitiveType.Void, err);
            if (ret.IsError()) {
                if (args.Length == 0 && !mets.Any() && objectType is ClassType ctp && !ctp.IsInterface() && !ctp.IsAbstract()) {
                    // create default constructor if no one is defined
                    objectType.Context.DefineMethod(ret = new BasicMethod(objectType.Position, "ctor", Visibility.Internal, PrimitiveType.Void, List.Empty<IVariable>()) {
                        Specifiers = Method.Specifier.Constructor,
                        NestedIn = ctp.Context,
                        Context = SimpleMethodContext.GetImmutable(this)
                    });
                    if (ctp.Superclass != null) {
                        var superCtor = GetConstructor(pos, ctp.Superclass, Array.Empty<IExpression>(), err);
                        if (!superCtor.IsError()) {
                            (ret as BasicMethod).Body.Instruction = new SuperCall(pos, ctp.Superclass, superCtor, Array.Empty<IExpression>());
                        }
                    }
                    else {
                        (ret as BasicMethod).Body.Instruction = new BlockStatement(pos, Array.Empty<IStatement>(), Context.GetImmutable(this));
                    }
                }
                else
                    $"The type {objectType} does not define a constructor ctor: {string.Join(" x ", args.Select(x => x.ReturnType))}".Report(pos);
            }
            return ret;
        }
        public IExpression CreateNewObjectExpression(Position pos, IType objectType, IExpression[] args, ErrorBuffer err) {
            if (args.Any(x => x is ExpressionParameterPackUnpack || x is ExpressionParameterAccess)) {
                return new NewObjectExpression(pos, objectType, null, args);
            }
            var ctor = GetConstructor(pos, objectType, args, err);
            if (ctor.IsError())
                return Expression.Error;
            return new NewObjectExpression(pos, objectType, ctor, args);
        }
        public bool IsRefIterableOver(IType range, IType over) {
            if (range is ModifierType mt)
                range = mt.UnderlyingType;
            if (range.IsError())
                return true;
            if (range is null || range.IsTop())
                return false;
            if (over is null)
                return false;
            return (range.IsArray() || range.IsArraySlice()) && (range as IWrapperType).ItemType.IsSubTypeOf(over);
        }
        public IEnumerable<IType> IsIterable(IType range) {
            if (range is ModifierType mt)
                range = mt.UnderlyingType;
            if (range.IsError()) {
                yield return Type.Error;
                yield break;
            }
            if (range is null || range.IsTop())
                yield break;
            if (range.IsArray() || range.IsArraySlice()) {
                yield return (range as AggregateType).ItemType;
                yield break;
            }
            var mets = range.Context.InstanceContext.MethodsByName("getIterator");
            foreach (var met in mets) {
                foreach (var it in IsIterator(met.ReturnType)) {
                    yield return it;
                }
            }
        }
        public IEnumerable<IType> IsIterator(IType it) {
            if (it is ModifierType mt)
                it = mt.UnderlyingType;
            if (it.IsError()) {
                yield return Type.Error;
                yield break;
            }
            if (it is null || it.IsTop())
                yield break;
            var mets = it.Context.InstanceContext.MethodsByName("tryGetNext").Where(met => {
                if (!met.ReturnType.IsPrimitive(PrimitiveName.Bool))
                    return false;
                if (met.Arguments.Length != 1)
                    return false;
                if (!met.Arguments[0].Type.IsByRef())
                    return false;
                if (met.Arguments[0].IsFinal())
                    return false;
                return true;
            });
            foreach (var met in mets) {
                yield return (met.Arguments[0].Type as IWrapperType).ItemType;
            }
        }
        public bool IsIterableOver(IType range, IType over) {
            if (range is ModifierType mt)
                range = mt.UnderlyingType;
            if (range.IsError())
                return true;
            if (range is null || range.IsTop())
                return false;
            if (over is null)
                return false;
            if (range is VarArgType va && va.ItemType.UnWrapAll() == over)
                return true;
            if (range is IterableType iter && over.IsSubTypeOf(iter.ItemType))
                return true;
            if (IsRefIterableOver(range, over))
                return true;

            var mets = range.Context.InstanceContext.DeclaredMethodsByName("getIterator");
            return mets.Any(met => {
                return met.Arguments.Length == 0 && IsIteratorOver(met.ReturnType, over);
            });
        }
        public bool IsIteratorOver(IType it, IType over) {
            if (it is ModifierType mt)
                it = mt.UnderlyingType;
            if (it.IsError())
                return true;
            if (it is null || it.IsTop() || over is null)
                return false;
            if (it is IteratorType iter && over.IsSubTypeOf(iter.ItemType))
                return true;
            var mets = it.Context.InstanceContext.DeclaredMethodsByName("tryGetNext");
            if (over.IsTop() && mets.Any())
                return true;
            return mets.Any(met => {
                if (!met.ReturnType.IsPrimitive(PrimitiveName.Bool))
                    return false;
                if (met.Arguments.Length != 1)
                    return false;
                if (!met.Arguments[0].Type.IsByRef())
                    return false;
                if (met.Arguments[0].IsFinal())
                    return false;
                var retTy = (met.Arguments[0].Type as IWrapperType).ItemType;
                if (retTy.IsSubTypeOf(over))
                    return true;
                if (retTy is GenericTypeParameter genTy)
                    return genTy.CanReplace(over);

                return false;
            });
        }

    }
}
