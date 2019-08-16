using CompilerInfrastructure;
using CompilerInfrastructure.Contexts;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types;
using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using static LLVMCodeGenerator.InstructionGenerator.InternalFunction;
using static NativeManagedContext;

namespace LLVMCodeGenerator {
    public partial class InstructionGenerator {

        public enum InternalFunction {
            NONE,
            cprint,
            cprintln,
            cprintnwln,
            to_str,
            uto_str,
            lto_str,
            ulto_str,
            llto_str,
            dto_str,
            fto_str,
            strconcat,
            strmulticoncat,
            strmul,
            gc_init,
            gc_new,
            gcnewActorContext,
            runAsync,
            runAsyncT,
            runImmediately,
            runImmediatelyT,
            runImmediatelyTask,
            runImmediatelyTaskT,
            waitTask,
            waitTaskValue,
            getCompletedTask,
            getCompletedTaskT,
            taskAwaiterEnqueue,
            getTaskValue,
            runLoopAsync,
            runLoopCoroutineAsync,
            runLoopCoroutineAsyncTask,
            runLoopCoroutineAsyncOffs,
            throwException,
            throwOutOfBounds,
            throwOutOfBounds64,
            throwIfOutOfBounds,
            throwIfOutOfBounds64,
            throwIfNullOrOutOfBounds,
            throwIfNullOrOutOfBounds64,
            throwIfNull,
            throwNullDereference,
            initExceptionHandling,
            initializeActorQueue,
            runActorTaskAsync,
            runActorTaskTAsync,
            delay,
            thread_sleep,
            reduceAsync,
            randomUInt,
            randomUIntRange,
            // boost::program_options
            SetupProgramOptions,
            AddOptionFlag,
            AddOptionI32,
            AddOptionI64,
            AddOptionU32,
            AddOptionU64,
            AddOptionFloat,
            AddOptionDouble,
            AddOptionString,
            AddOptionFlagDflt,
            AddOptionI32Dflt,
            AddOptionI64Dflt,
            AddOptionU32Dflt,
            AddOptionU64Dflt,
            AddOptionFloatDflt,
            AddOptionDoubleDflt,
            AddOptionStringDflt,
            AddPositionalI32,
            AddPositionalI64,
            AddPositionalU32,
            AddPositionalU64,
            AddPositionalFloat,
            AddPositionalDouble,
            AddPositionalString,
            ParseOptions,
            //TODO more
        }
        private static readonly Dictionary<InternalFunction, IntPtr> internalFunctions = new Dictionary<InternalFunction, IntPtr>();
        protected static InternalFunction PrimitiveToStringFunction(PrimitiveName sourceTp) {
            switch (sourceTp) {

                case PrimitiveName.Byte:
                case PrimitiveName.Char:
                case PrimitiveName.Short:
                case PrimitiveName.UShort:
                case PrimitiveName.Int:
                    return to_str;
                case PrimitiveName.UInt:
                    return uto_str;
                case PrimitiveName.Handle:
                case PrimitiveName.Long:
                    return lto_str;
                case PrimitiveName.ULong:
                    return ulto_str;
                case PrimitiveName.BigLong:
                case PrimitiveName.UBigLong:
                    return llto_str;
                case PrimitiveName.Float:
                    return fto_str;
                case PrimitiveName.Double:
                    return dto_str;
                default:
                    return NONE;
            }
        }
        protected internal static IntPtr GetOrCreateInternalFunction(ManagedContext ctx, InternalFunction intnlName) {
            if (internalFunctions.TryGetValue(intnlName, out var ret))
                return ret;
            string name = intnlName.ToString();
            switch (intnlName) {
                case cprint:
                case cprintln: {
                    // cprint(ln) : const char* x int -> void
                    var cptrTy = ctx.GetVoidPtr();
                    var intty = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, new[] { cptrTy, intty }, new[] { "strVal", "strLen" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 0);
                    break;
                }
                case cprintnwln: {
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, Array.Empty<IntPtr>(), Array.Empty<string>(), true);
                    break;
                }
                case uto_str:
                case to_str: {
                    var inpTy = ctx.GetIntType();
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { inpTy, retTy }, new[] { "val", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 1);
                    break;
                }
                case lto_str:
                case ulto_str: {
                    var inpTy = ctx.GetLongType();
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { inpTy, retTy }, new[] { "val", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 1);
                    break;
                }
                case llto_str: {
                    var inpTy = ctx.GetLongType();
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { inpTy, inpTy, retTy }, new[] { "hi", "lo", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 2);
                    break;
                }
                case dto_str: {
                    var inpTy = ctx.GetDoubleType();
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { inpTy, retTy }, new[] { "val", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 1);
                    break;
                }
                case fto_str: {
                    var inpTy = ctx.GetFloatType();
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { inpTy, retTy }, new[] { "val", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 1);
                    break;
                }
                case strconcat: {
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    var cptrTy = ctx.GetVoidPtr();
                    var intty = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction("strconcat", voidTy, new[] { cptrTy, intty, cptrTy, intty, retTy }, new[] { "str1val", "str1len", "str2val", "str2len", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 2);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 4);
                    break;
                }
                case strmulticoncat: {
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    var intty = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction("strmulticoncat", voidTy, new[] { retTy, intty, retTy }, new[] { "strs", "strc", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 2);
                    break;
                }
                case strmul: {
                    var retTy = ctx.GetPointerType(ctx.GetStringType());
                    var cptrTy = ctx.GetVoidPtr();
                    var intty = ctx.GetIntType();
                    var szTy = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction("strmul", voidTy, new[] { cptrTy, szTy, intty, retTy }, new[] { "strVal", "strLen", "factor", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "writeonly" }, 3);
                    break;
                }
                case gc_init: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), Array.Empty<IntPtr>(), Array.Empty<string>(), true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "inaccessiblememonly" });
                    break;
                }
                case gc_new: {
                    ret = ctx.DeclareMallocFunction(name, new[] { ctx.GetSizeTType() }, true);
                    break;
                }
                case gcnewActorContext: {
                    ret = ctx.DeclareMallocFunction(name, Array.Empty<IntPtr>(), false);
                    break;
                }
                case runAsync:
                case runImmediately: {
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr() });
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), new[] { taskTy, ctx.GetVoidPtr() }, new[] { "task", "args" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    if (intnlName == runAsync)
                        ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case runAsyncT:
                case runImmediatelyT: {
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, ctx.GetPointerType(voidPtr) });
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), new[] { taskTy, ctx.GetVoidPtr() }, new[] { "task", "args" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    if (intnlName == runAsyncT)
                        ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case runImmediatelyTask: {
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { ctx.GetVoidPtr() });
                    ret = ctx.DeclareFunction(name, ctx.GetBoolType(), new[] { taskTy, ctx.GetVoidPtr(), ctx.GetPointerType(ctx.GetVoidPtr()) }, new[] { "task", "args", "resultTask" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "writeonly" }, 2);
                    break;
                }
                case runImmediatelyTaskT: {
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, ctx.GetPointerType(voidPtr) });
                    ret = ctx.DeclareFunction(name, ctx.GetBoolType(), new[] { taskTy, ctx.GetVoidPtr(), ctx.GetPointerType(ctx.GetVoidPtr()) }, new[] { "task", "args", "resultTask" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "writeonly" }, 2);
                    break;
                }
                case waitTask: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { ctx.GetVoidPtr() }, new[] { "task" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture" }, 0);
                    break;
                }
                case waitTaskValue: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), new[] { ctx.GetVoidPtr() }, new[] { "task" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture" }, 0);
                    break;
                }
                case getCompletedTask: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), Array.Empty<IntPtr>(), Array.Empty<string>(), true);
                    break;
                }
                case getCompletedTaskT: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), new[] { ctx.GetVoidPtr() }, new[] { "result" }, true);
                    break;
                }
                case taskAwaiterEnqueue: {
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, ctx.GetBoolType(), new[] { voidPtr, voidPtr }, new[] { "t", "awaiter" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture" }, 0);
                    break;
                }
                case getTaskValue: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidPtr(), new[] { ctx.GetVoidPtr() }, new[] { "taskt" }, true);
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 0);
                    break;
                }
                case runLoopAsync: {
                    var longTy = ctx.GetLongType();
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, longTy, longTy });
                    ret = ctx.DeclareFunction(name, voidPtr, new[] { taskTy, voidPtr, longTy, longTy, longTy }, new[] { "body", "args", "start", "count", "blckSz" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case runLoopCoroutineAsync: {
                    var longTy = ctx.GetLongType();
                    var intTy = ctx.GetIntType();
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, voidPtr, ctx.GetPointerType(longTy), longTy });
                    ret = ctx.DeclareFunction(name, voidPtr, new[] { taskTy, voidPtr, voidPtr, intTy, longTy, longTy, longTy }, new[] { "body", "args", "mutableArgs", "mutableArgsSize", "start", "count", "blckSz" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case runLoopCoroutineAsyncTask: {
                    var longTy = ctx.GetLongType();
                    var intTy = ctx.GetIntType();
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, voidPtr, ctx.GetPointerType(longTy), longTy });
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { taskTy, voidPtr, voidPtr, intTy, longTy, longTy, longTy, ctx.GetPointerType(voidPtr) }, new[] { "body", "args", "mutableArgs", "mutableArgsSize", "start", "count", "blckSz", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    break;
                }
                case runLoopCoroutineAsyncOffs: {
                    var longTy = ctx.GetLongType();
                    var intTy = ctx.GetIntType();
                    var voidPtr = ctx.GetVoidPtr();
                    var taskTy = ctx.GetFunctionPtrType(ctx.GetBoolType(), new[] { voidPtr, voidPtr, ctx.GetPointerType(longTy), longTy });
                    ret = ctx.DeclareFunction(name, voidPtr, new[] { taskTy, voidPtr, voidPtr, intTy, longTy, longTy, longTy, intTy }, new[] { "body", "args", "mutableArgs", "mutableArgsSize", "start", "count", "blckSz", "mutThisTaskOffset" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case throwException:
                case throwNullDereference: {
                    var voidPtr = ctx.GetVoidPtr();
                    var intTy = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, new[] { voidPtr, intTy }, new[] { "msg", "msgLen" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "noreturn", "uwtable" });
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    break;
                }
                case throwIfNull: {
                    var voidPtr = ctx.GetVoidPtr();
                    var intTy = ctx.GetSizeTType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, new[] { voidPtr, voidPtr, intTy }, new[] { "msg", "msgLen" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "uwtable" });
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readnone" }, 0);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 1);
                    break;
                }
                case throwIfNullOrOutOfBounds:
                case throwIfNullOrOutOfBounds64: {
                    var intTy = intnlName == throwIfNullOrOutOfBounds ? ctx.GetIntType() : ctx.GetLongType();
                    var intPtr = ctx.GetPointerType(intTy);
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, new[] { intTy, intPtr }, new[] { "index", "lengthPtr" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "uwtable" });
                    ctx.AddParamAttributes(ret, new[] { "nocapture", "readonly" }, 1);
                    break;
                }
                case throwOutOfBounds64:
                case throwOutOfBounds: {
                    var intTy = intnlName == throwOutOfBounds ? ctx.GetIntType() : ctx.GetLongType();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { intTy, intTy }, new[] { "index", "length" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "noreturn", "uwtable" });

                    break;
                }
                case throwIfOutOfBounds:
                case throwIfOutOfBounds64: {
                    var intTy = intnlName == throwIfOutOfBounds ? ctx.GetIntType() : ctx.GetLongType();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { intTy, intTy }, new[] { "index", "length" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "uwtable" });
                    break;
                }
                case initExceptionHandling: {
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), Array.Empty<IntPtr>(), Array.Empty<String>(), true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "inaccessiblememonly" });
                    break;
                }
                case initializeActorQueue: {
                    // void initializeActorQueue(void ** behavior_queue);
                    var voidPtrPtr = ctx.GetPointerType(ctx.GetVoidPtr());
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { voidPtrPtr }, new[] { "behavior_queue" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind" });
                    ctx.AddParamAttributes(ret, new[] { "writeonly", "nocapture" }, 0);
                    break;
                }
                case runActorTaskAsync: {
                    // void * runActorTaskAsync(bool(*task)(void*), void* args, void* actorContext);
                    var voidPtr = ctx.GetVoidPtr();
                    var boolTy = ctx.GetBoolType();
                    //var boolPtr = ctx.GetPointerType(boolTy);
                    var taskTy = ctx.GetFunctionPtrType(boolTy, new[] { voidPtr });
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { taskTy, voidPtr, voidPtr, ctx.GetPointerType(voidPtr) }, new[] { "task", "args", "actorContext", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    //ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case runActorTaskTAsync: {
                    // void * runActorTaskTAsync(bool(*task)(void*,void**), void* args, void* actorContext);
                    var voidPtr = ctx.GetVoidPtr();
                    var boolTy = ctx.GetBoolType();
                    //var boolPtr = ctx.GetPointerType(boolTy);
                    var taskTy = ctx.GetFunctionPtrType(boolTy, new[] { voidPtr, ctx.GetPointerType(voidPtr) });
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { taskTy, voidPtr, voidPtr, ctx.GetPointerType(voidPtr) }, new[] { "task", "args", "actorContext", "ret" }, true);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 0);
                    //ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case delay: {
                    var intTy = ctx.GetIntType();
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, voidPtr, new[] { intTy }, new[] { "millis" }, true);
                    ctx.AddReturnNoAliasAttribute(ret);
                    break;
                }
                case thread_sleep: {
                    var intTy = ctx.GetIntType();
                    var voidTy = ctx.GetVoidType();
                    ret = ctx.DeclareFunction(name, voidTy, new[] { intTy }, new[] { "millis" }, true);
                    break;
                }
                case reduceAsync: {
                    var voidPtr = ctx.GetVoidPtr();
                    var longTy = ctx.GetLongType();
                    var intTy = ctx.GetSizeTType();
                    var bodyTy = ctx.GetFunctionPtrType(voidPtr, new[] { voidPtr, longTy, longTy });
                    var reductionTy = ctx.GetFunctionPtrType(voidPtr, new[] { voidPtr, voidPtr });

                    ret = ctx.DeclareFunction(name, voidPtr, new[] { bodyTy, reductionTy, voidPtr, intTy, longTy, voidPtr }, new[] { "body", "reducer", "values", "valuec", "blockSize", "seed" }, true);
                    ctx.AddReturnNoAliasAttribute(ret);
                    var readonlyAttr = new[] { "readonly" };
                    ctx.AddParamAttributes(ret, readonlyAttr, 0);
                    ctx.AddParamAttributes(ret, readonlyAttr, 1);
                    ctx.AddParamAttributes(ret, readonlyAttr, 2);
                    ctx.AddParamAttributes(ret, readonlyAttr, 5);
                    break;
                }
                case randomUInt: {
                    var intTy = ctx.GetIntType();
                    ret = ctx.DeclareFunction(name, intTy, Array.Empty<IntPtr>(), Array.Empty<string>(), true);
                    break;
                }
                case randomUIntRange: {
                    var intTy = ctx.GetIntType();
                    ret = ctx.DeclareFunction(name, intTy, new[] { intTy, intTy }, new[] { "min", "max" }, true);
                    break;
                }
                case SetupProgramOptions: {
                    var retTy = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, retTy, Array.Empty<IntPtr>(), Array.Empty<string>(), true);
                    ctx.AddFunctionAttributes(ret, new[] { "uwtable", "inaccessiblememonly" });
                    break;
                }
                case AddOptionFlag:
                case AddOptionI32:
                case AddOptionI64:
                case AddOptionU32:
                case AddOptionU64:
                case AddOptionFloat:
                case AddOptionDouble:
                case AddOptionString: {
                    var valueTy = GetTypeFromEnding(ctx, name);
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { voidPtr, ctx.GetPointerType(valueTy), voidPtr, voidPtr }, new[] { "dat", "result", "name", "description" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "argmemonly" });
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 2);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 3);
                    break;
                }
                case AddOptionFlagDflt:
                case AddOptionI32Dflt:
                case AddOptionI64Dflt:
                case AddOptionU32Dflt:
                case AddOptionU64Dflt:
                case AddOptionFloatDflt:
                case AddOptionDoubleDflt: {
                    var valueTy = GetTypeFromEnding(ctx, name);
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { voidPtr, ctx.GetPointerType(valueTy), valueTy, voidPtr, voidPtr }, new[] { "dat", "result", "dflt", "name", "description" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "argmemonly" });
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 3);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 4);
                    break;
                }
                case AddOptionStringDflt: {
                    var valueTy = ctx.GetStringType();
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { voidPtr, ctx.GetPointerType(valueTy), voidPtr, ctx.GetSizeTType(), voidPtr, voidPtr }, new[] { "dat", "result", "dflt", "dflt_len", "name", "description" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "argmemonly" });
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 2);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 4);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 5);
                    break;
                }
                case AddPositionalI32:
                case AddPositionalI64:
                case AddPositionalU32:
                case AddPositionalU64:
                case AddPositionalFloat:
                case AddPositionalDouble:
                case AddPositionalString: {
                    var valueTy = GetTypeFromEnding(ctx, name);
                    var valueTyPtrPtr = ctx.GetPointerType(ctx.GetPointerType(valueTy));
                    var size_tPtr = ctx.GetPointerType(ctx.GetSizeTType());
                    var voidPtr = ctx.GetVoidPtr();
                    ret = ctx.DeclareFunction(name, ctx.GetVoidType(), new[] { voidPtr, valueTyPtrPtr, size_tPtr, ctx.GetIntType(), voidPtr, voidPtr }, new[] { "dat", "result", "resultc", "maxc", "name", "description" }, true);
                    ctx.AddFunctionAttributes(ret, new[] { "nounwind", "argmemonly" });
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 4);
                    ctx.AddParamAttributes(ret, new[] { "readonly" }, 5);
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
            internalFunctions[intnlName] = ret;
            return ret;
        }
        static IntPtr GetTypeFromEnding(ManagedContext ctx, string name) {
            if (name.EndsWith("Flag"))
                return ctx.GetBoolType();
            else if (name.EndsWith("I32") || name.EndsWith("U32"))
                return ctx.GetIntType();
            else if (name.EndsWith("I64") || name.EndsWith("U64"))
                return ctx.GetLongType();
            else if (name.EndsWith("Float"))
                return ctx.GetFloatType();
            else if (name.EndsWith("Double"))
                return ctx.GetDoubleType();
            else if (name.EndsWith("String"))
                return ctx.GetStringType();
            else
                throw new ArgumentException();
        }
        public static IntPtr GetOrCreateInternalFunction(ManagedContext ctx, string name) {
            if (!Enum.TryParse<InternalFunction>(name, out var intnlName))
                throw new NotImplementedException();
            return GetOrCreateInternalFunction(ctx, intnlName);
        }
        public static IntPtr GetOrCreateInternalFunction(ManagedContext ctx, IMethod met) {
            if (met.Signature.Name == "wait" && (met.NestedIn as ITypeContext).Type.IsAwaitable()) {
                if (((met.NestedIn as ITypeContext).Type as IWrapperType).ItemType.IsVoid()) {
                    return GetOrCreateInternalFunction(ctx, waitTask);
                }
                else {
                    return GetOrCreateInternalFunction(ctx, waitTaskValue);
                }
            }
            if (!Enum.TryParse<InternalFunction>(met.Signature.InternalName, out var intnlName))
                throw new NotImplementedException();
            return GetOrCreateInternalFunction(ctx, intnlName);
        }
    }
}
