using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public static unsafe class NativeManagedContext {
    public class ManagedContext : IDisposable {
        public readonly IntPtr Instance;
        private bool isDisposed = false;

        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr CreateManagedContext ([MarshalAs(UnmanagedType.LPUTF8Str)]string name, [MarshalAs(UnmanagedType.LPUTF8Str)]string filename);
        public  ManagedContext (string name, string filename) {
            Instance = CreateManagedContext(name, filename);
        }
        ~ManagedContext() {
            Dispose();
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void DisposeManagedContext (IntPtr ctx);
        public void Dispose () {
            if (isDisposed)return; isDisposed = true;
            DisposeManagedContext(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void Save (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string filename);
        public void Save (string filename) {
            Save(Instance, filename);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void DumpModule (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string filename);
        public void DumpModule (string filename) {
            DumpModule(Instance, filename);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void PrintValueDump (IntPtr ctx, IntPtr val);
        public void PrintValueDump (IntPtr val) {
            PrintValueDump(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void PrintTypeDump (IntPtr ctx, IntPtr ty);
        public void PrintTypeDump (IntPtr ty) {
            PrintTypeDump(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void PrintFunctionDump (IntPtr ctx, IntPtr fn);
        public void PrintFunctionDump (IntPtr fn) {
            PrintFunctionDump(Instance, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void PrintBasicBlockDump (IntPtr ctx, IntPtr bb);
        public void PrintBasicBlockDump (IntPtr bb) {
            PrintBasicBlockDump(Instance, bb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool VerifyModule (IntPtr ctx);
        public bool VerifyModule () {
            return VerifyModule(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getStruct (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name);
        public static IntPtr GetStruct (IntPtr ctx, string name) {
            return getStruct(ctx, name);
        }
        public IntPtr GetStruct (string name) {
            return getStruct(Instance, name);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void completeStruct (IntPtr ctx, IntPtr str, IntPtr[] body, uint bodyLen);
        public static void CompleteStruct (IntPtr ctx, IntPtr str, IntPtr[] body) {
            completeStruct(ctx, str, body, (uint)body.Length);
        }
        public void CompleteStruct (IntPtr str, IntPtr[] body) {
            completeStruct(Instance, str, body, (uint)body.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getUnnamedStruct (IntPtr ctx, IntPtr[] body, uint bodyLen);
        public static IntPtr GetUnnamedStruct (IntPtr ctx, IntPtr[] body) {
            return getUnnamedStruct(ctx, body, (uint)body.Length);
        }
        public IntPtr GetUnnamedStruct (IntPtr[] body) {
            return getUnnamedStruct(Instance, body, (uint)body.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getBoolType (IntPtr ctx);
        public static IntPtr GetBoolType (IntPtr ctx) {
            return getBoolType(ctx);
        }
        public IntPtr GetBoolType () {
            return getBoolType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getByteType (IntPtr ctx);
        public static IntPtr GetByteType (IntPtr ctx) {
            return getByteType(ctx);
        }
        public IntPtr GetByteType () {
            return getByteType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getShortType (IntPtr ctx);
        public static IntPtr GetShortType (IntPtr ctx) {
            return getShortType(ctx);
        }
        public IntPtr GetShortType () {
            return getShortType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getIntType (IntPtr ctx);
        public static IntPtr GetIntType (IntPtr ctx) {
            return getIntType(ctx);
        }
        public IntPtr GetIntType () {
            return getIntType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getLongType (IntPtr ctx);
        public static IntPtr GetLongType (IntPtr ctx) {
            return getLongType(ctx);
        }
        public IntPtr GetLongType () {
            return getLongType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getBiglongType (IntPtr ctx);
        public static IntPtr GetBiglongType (IntPtr ctx) {
            return getBiglongType(ctx);
        }
        public IntPtr GetBiglongType () {
            return getBiglongType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getFloatType (IntPtr ctx);
        public static IntPtr GetFloatType (IntPtr ctx) {
            return getFloatType(ctx);
        }
        public IntPtr GetFloatType () {
            return getFloatType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getDoubleType (IntPtr ctx);
        public static IntPtr GetDoubleType (IntPtr ctx) {
            return getDoubleType(ctx);
        }
        public IntPtr GetDoubleType () {
            return getDoubleType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getStringType (IntPtr ctx);
        public static IntPtr GetStringType (IntPtr ctx) {
            return getStringType(ctx);
        }
        public IntPtr GetStringType () {
            return getStringType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getArrayType (IntPtr ctx, IntPtr elem, uint count);
        public static IntPtr GetArrayType (IntPtr ctx, IntPtr elem, uint count) {
            return getArrayType(ctx, elem, count);
        }
        public IntPtr GetArrayType (IntPtr elem, uint count) {
            return getArrayType(Instance, elem, count);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getPointerType (IntPtr ctx, IntPtr pointsTo);
        public static IntPtr GetPointerType (IntPtr ctx, IntPtr pointsTo) {
            return getPointerType(ctx, pointsTo);
        }
        public IntPtr GetPointerType (IntPtr pointsTo) {
            return getPointerType(Instance, pointsTo);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getVoidPtr (IntPtr ctx);
        public static IntPtr GetVoidPtr (IntPtr ctx) {
            return getVoidPtr(ctx);
        }
        public IntPtr GetVoidPtr () {
            return getVoidPtr(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getVoidType (IntPtr ctx);
        public static IntPtr GetVoidType (IntPtr ctx) {
            return getVoidType(ctx);
        }
        public IntPtr GetVoidType () {
            return getVoidType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getOpaqueType (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name);
        public static IntPtr GetOpaqueType (IntPtr ctx, string name) {
            return getOpaqueType(ctx, name);
        }
        public IntPtr GetOpaqueType (string name) {
            return getOpaqueType(Instance, name);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getSizeTType (IntPtr ctx);
        public static IntPtr GetSizeTType (IntPtr ctx) {
            return getSizeTType(ctx);
        }
        public IntPtr GetSizeTType () {
            return getSizeTType(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getTypeFromValue (IntPtr ctx, IntPtr val);
        public static IntPtr GetTypeFromValue (IntPtr ctx, IntPtr val) {
            return getTypeFromValue(ctx, val);
        }
        public IntPtr GetTypeFromValue (IntPtr val) {
            return getTypeFromValue(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr declareFunction (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr retTy, IntPtr[] argTys, uint argc, string[] argNames, uint argNamec, bool isPublic);
        public static IntPtr DeclareFunction (IntPtr ctx, string name, IntPtr retTy, IntPtr[] argTys, string[] argNames, bool isPublic) {
            return declareFunction(ctx, name, retTy, argTys, (uint)argTys.Length, argNames, (uint)argNames.Length, isPublic);
        }
        public IntPtr DeclareFunction (string name, IntPtr retTy, IntPtr[] argTys, string[] argNames, bool isPublic) {
            return declareFunction(Instance, name, retTy, argTys, (uint)argTys.Length, argNames, (uint)argNames.Length, isPublic);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr declareFunctionOfType (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr fnPtrTy, bool isPublic);
        public static IntPtr DeclareFunctionOfType (IntPtr ctx, string name, IntPtr fnPtrTy, bool isPublic) {
            return declareFunctionOfType(ctx, name, fnPtrTy, isPublic);
        }
        public IntPtr DeclareFunctionOfType (string name, IntPtr fnPtrTy, bool isPublic) {
            return declareFunctionOfType(Instance, name, fnPtrTy, isPublic);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addFunctionAttributes (IntPtr ctx, IntPtr fn, string[] atts, uint attc);
        public static void AddFunctionAttributes (IntPtr ctx, IntPtr fn, string[] atts) {
            addFunctionAttributes(ctx, fn, atts, (uint)atts.Length);
        }
        public void AddFunctionAttributes (IntPtr fn, string[] atts) {
            addFunctionAttributes(Instance, fn, atts, (uint)atts.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addParamAttributes (IntPtr ctx, IntPtr fn, string[] atts, uint attc, uint paramIdx);
        public static void AddParamAttributes (IntPtr ctx, IntPtr fn, string[] atts, uint paramIdx) {
            addParamAttributes(ctx, fn, atts, (uint)atts.Length, paramIdx);
        }
        public void AddParamAttributes (IntPtr fn, string[] atts, uint paramIdx) {
            addParamAttributes(Instance, fn, atts, (uint)atts.Length, paramIdx);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addReturnNoAliasAttribute (IntPtr ctx, IntPtr fn);
        public static void AddReturnNoAliasAttribute (IntPtr ctx, IntPtr fn) {
            addReturnNoAliasAttribute(ctx, fn);
        }
        public void AddReturnNoAliasAttribute (IntPtr fn) {
            addReturnNoAliasAttribute(Instance, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addReturnNotNullAttribute (IntPtr ctx, IntPtr fn);
        public static void AddReturnNotNullAttribute (IntPtr ctx, IntPtr fn) {
            addReturnNotNullAttribute(ctx, fn);
        }
        public void AddReturnNotNullAttribute (IntPtr fn) {
            addReturnNotNullAttribute(Instance, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addReturnDereferenceableAttribute (IntPtr ctx, IntPtr fn, ulong numBytesOrDeduce);
        public static void AddReturnDereferenceableAttribute (IntPtr ctx, IntPtr fn, ulong numBytesOrDeduce) {
            addReturnDereferenceableAttribute(ctx, fn, numBytesOrDeduce);
        }
        public void AddReturnDereferenceableAttribute (IntPtr fn, ulong numBytesOrDeduce) {
            addReturnDereferenceableAttribute(Instance, fn, numBytesOrDeduce);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool addParamDereferenceableAttribute (IntPtr ctx, IntPtr fn, uint paramIdx, ulong numBytesOrDeduce);
        public static bool AddParamDereferenceableAttribute (IntPtr ctx, IntPtr fn, uint paramIdx, ulong numBytesOrDeduce) {
            return addParamDereferenceableAttribute(ctx, fn, paramIdx, numBytesOrDeduce);
        }
        public bool AddParamDereferenceableAttribute (IntPtr fn, uint paramIdx, ulong numBytesOrDeduce) {
            return addParamDereferenceableAttribute(Instance, fn, paramIdx, numBytesOrDeduce);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addReturnDereferenceableOrNullAttribute (IntPtr ctx, IntPtr fn, ulong numBytesOrDeduce);
        public static void AddReturnDereferenceableOrNullAttribute (IntPtr ctx, IntPtr fn, ulong numBytesOrDeduce) {
            addReturnDereferenceableOrNullAttribute(ctx, fn, numBytesOrDeduce);
        }
        public void AddReturnDereferenceableOrNullAttribute (IntPtr fn, ulong numBytesOrDeduce) {
            addReturnDereferenceableOrNullAttribute(Instance, fn, numBytesOrDeduce);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool addParamDereferenceableOrNullAttribute (IntPtr ctx, IntPtr fn, uint paramIdx, ulong numBytesOrDeduce);
        public static bool AddParamDereferenceableOrNullAttribute (IntPtr ctx, IntPtr fn, uint paramIdx, ulong numBytesOrDeduce) {
            return addParamDereferenceableOrNullAttribute(ctx, fn, paramIdx, numBytesOrDeduce);
        }
        public bool AddParamDereferenceableOrNullAttribute (IntPtr fn, uint paramIdx, ulong numBytesOrDeduce) {
            return addParamDereferenceableOrNullAttribute(Instance, fn, paramIdx, numBytesOrDeduce);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr declareMallocFunction (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr[] argTys, uint argc, bool resultNotNull);
        public static IntPtr DeclareMallocFunction (IntPtr ctx, string name, IntPtr[] argTys, bool resultNotNull) {
            return declareMallocFunction(ctx, name, argTys, (uint)argTys.Length, resultNotNull);
        }
        public IntPtr DeclareMallocFunction (string name, IntPtr[] argTys, bool resultNotNull) {
            return declareMallocFunction(Instance, name, argTys, (uint)argTys.Length, resultNotNull);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getFunctionPtrTypeFromFunction (IntPtr ctx, IntPtr fn);
        public static IntPtr GetFunctionPtrTypeFromFunction (IntPtr ctx, IntPtr fn) {
            return getFunctionPtrTypeFromFunction(ctx, fn);
        }
        public IntPtr GetFunctionPtrTypeFromFunction (IntPtr fn) {
            return getFunctionPtrTypeFromFunction(Instance, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getFunctionPtrType (IntPtr ctx, IntPtr retTy, IntPtr[] argTys, uint argc);
        public static IntPtr GetFunctionPtrType (IntPtr ctx, IntPtr retTy, IntPtr[] argTys) {
            return getFunctionPtrType(ctx, retTy, argTys, (uint)argTys.Length);
        }
        public IntPtr GetFunctionPtrType (IntPtr retTy, IntPtr[] argTys) {
            return getFunctionPtrType(Instance, retTy, argTys, (uint)argTys.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void resetInsertPoint (IntPtr ctx, IntPtr bb, IntPtr irb);
        public static void ResetInsertPoint (IntPtr ctx, IntPtr bb, IntPtr irb) {
            resetInsertPoint(ctx, bb, irb);
        }
        public void ResetInsertPoint (IntPtr bb, IntPtr irb) {
            resetInsertPoint(Instance, bb, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr defineAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, [MarshalAs(UnmanagedType.LPUTF8Str)]string name);
        public static IntPtr DefineAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, string name) {
            return defineAlloca(ctx, fn, ty, name);
        }
        public IntPtr DefineAlloca (IntPtr fn, IntPtr ty, string name) {
            return defineAlloca(Instance, fn, ty, name);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr defineZeroinitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr irb, bool addlifetime);
        public static IntPtr DefineZeroinitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, string name, IntPtr irb, bool addlifetime) {
            return defineZeroinitializedAlloca(ctx, fn, ty, name, irb, addlifetime);
        }
        public IntPtr DefineZeroinitializedAlloca (IntPtr fn, IntPtr ty, string name, IntPtr irb, bool addlifetime) {
            return defineZeroinitializedAlloca(Instance, fn, ty, name, irb, addlifetime);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr defineInitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, IntPtr initVal, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr irb, bool addlifetime);
        public static IntPtr DefineInitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr ty, IntPtr initVal, string name, IntPtr irb, bool addlifetime) {
            return defineInitializedAlloca(ctx, fn, ty, initVal, name, irb, addlifetime);
        }
        public IntPtr DefineInitializedAlloca (IntPtr fn, IntPtr ty, IntPtr initVal, string name, IntPtr irb, bool addlifetime) {
            return defineInitializedAlloca(Instance, fn, ty, initVal, name, irb, addlifetime);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr defineTypeInferredInitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr initVal, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr irb, bool addlifetime);
        public static IntPtr DefineTypeInferredInitializedAlloca (IntPtr ctx, IntPtr fn, IntPtr initVal, string name, IntPtr irb, bool addlifetime) {
            return defineTypeInferredInitializedAlloca(ctx, fn, initVal, name, irb, addlifetime);
        }
        public IntPtr DefineTypeInferredInitializedAlloca (IntPtr fn, IntPtr initVal, string name, IntPtr irb, bool addlifetime) {
            return defineTypeInferredInitializedAlloca(Instance, fn, initVal, name, irb, addlifetime);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void endLifeTime (IntPtr ctx, IntPtr ptr, IntPtr irb);
        public static void EndLifeTime (IntPtr ctx, IntPtr ptr, IntPtr irb) {
            endLifeTime(ctx, ptr, irb);
        }
        public void EndLifeTime (IntPtr ptr, IntPtr irb) {
            endLifeTime(Instance, ptr, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr loadField (IntPtr ctx, IntPtr basePtr, IntPtr[] gepIdx, uint gepIdxCount, IntPtr irb);
        public static IntPtr LoadField (IntPtr ctx, IntPtr basePtr, IntPtr[] gepIdx, IntPtr irb) {
            return loadField(ctx, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        public IntPtr LoadField (IntPtr basePtr, IntPtr[] gepIdx, IntPtr irb) {
            return loadField(Instance, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr loadFieldConstIdx (IntPtr ctx, IntPtr basePtr, uint[] gepIdx, uint geIdxCount, IntPtr irb);
        public static IntPtr LoadFieldConstIdx (IntPtr ctx, IntPtr basePtr, uint[] gepIdx, IntPtr irb) {
            return loadFieldConstIdx(ctx, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        public IntPtr LoadFieldConstIdx (IntPtr basePtr, uint[] gepIdx, IntPtr irb) {
            return loadFieldConstIdx(Instance, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void storeField (IntPtr ctx, IntPtr basePtr, IntPtr[] gepIdx, uint gepIdxCount, IntPtr nwVal, IntPtr irb);
        public static void StoreField (IntPtr ctx, IntPtr basePtr, IntPtr[] gepIdx, IntPtr nwVal, IntPtr irb) {
            storeField(ctx, basePtr, gepIdx, (uint)gepIdx.Length, nwVal, irb);
        }
        public void StoreField (IntPtr basePtr, IntPtr[] gepIdx, IntPtr nwVal, IntPtr irb) {
            storeField(Instance, basePtr, gepIdx, (uint)gepIdx.Length, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void storeFieldConstIdx (IntPtr ctx, IntPtr basePtr, uint[] gepIdx, uint gepIdxCount, IntPtr nwVal, IntPtr irb);
        public static void StoreFieldConstIdx (IntPtr ctx, IntPtr basePtr, uint[] gepIdx, IntPtr nwVal, IntPtr irb) {
            storeFieldConstIdx(ctx, basePtr, gepIdx, (uint)gepIdx.Length, nwVal, irb);
        }
        public void StoreFieldConstIdx (IntPtr basePtr, uint[] gepIdx, IntPtr nwVal, IntPtr irb) {
            storeFieldConstIdx(Instance, basePtr, gepIdx, (uint)gepIdx.Length, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr GetElementPtr (IntPtr ctx, IntPtr basePtr, IntPtr[] gepIdx, uint gepIdxCount, IntPtr irb);
        public IntPtr GetElementPtr (IntPtr basePtr, IntPtr[] gepIdx, IntPtr irb) {
            return GetElementPtr(Instance, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr GetElementPtrConstIdx (IntPtr ctx, IntPtr basePtr, uint[] gepIdx, uint gepIdxCount, IntPtr irb);
        public IntPtr GetElementPtrConstIdx (IntPtr basePtr, uint[] gepIdx, IntPtr irb) {
            return GetElementPtrConstIdx(Instance, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr GetElementPtrConstIdxWithType (IntPtr ctx, IntPtr pointeeTy, IntPtr basePtr, uint[] gepIdx, uint gepIdxCount, IntPtr irb);
        public IntPtr GetElementPtrConstIdxWithType (IntPtr pointeeTy, IntPtr basePtr, uint[] gepIdx, IntPtr irb) {
            return GetElementPtrConstIdxWithType(Instance, pointeeTy, basePtr, gepIdx, (uint)gepIdx.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr load (IntPtr ctx, IntPtr basePtr, IntPtr irb);
        public static IntPtr Load (IntPtr ctx, IntPtr basePtr, IntPtr irb) {
            return load(ctx, basePtr, irb);
        }
        public IntPtr Load (IntPtr basePtr, IntPtr irb) {
            return load(Instance, basePtr, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr loadAcquire (IntPtr ctx, IntPtr basePtr, IntPtr irb);
        public static IntPtr LoadAcquire (IntPtr ctx, IntPtr basePtr, IntPtr irb) {
            return loadAcquire(ctx, basePtr, irb);
        }
        public IntPtr LoadAcquire (IntPtr basePtr, IntPtr irb) {
            return loadAcquire(Instance, basePtr, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void store (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb);
        public static void Store (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            store(ctx, basePtr, nwVal, irb);
        }
        public void Store (IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            store(Instance, basePtr, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void storeRelease (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb);
        public static void StoreRelease (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            storeRelease(ctx, basePtr, nwVal, irb);
        }
        public void StoreRelease (IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            storeRelease(Instance, basePtr, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr compareExchangeAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr cmp, IntPtr nwVal, IntPtr irb);
        public static IntPtr CompareExchangeAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr cmp, IntPtr nwVal, IntPtr irb) {
            return compareExchangeAcqRel(ctx, basePtr, cmp, nwVal, irb);
        }
        public IntPtr CompareExchangeAcqRel (IntPtr basePtr, IntPtr cmp, IntPtr nwVal, IntPtr irb) {
            return compareExchangeAcqRel(Instance, basePtr, cmp, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr exchangeAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb);
        public static IntPtr ExchangeAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            return exchangeAcqRel(ctx, basePtr, nwVal, irb);
        }
        public IntPtr ExchangeAcqRel (IntPtr basePtr, IntPtr nwVal, IntPtr irb) {
            return exchangeAcqRel(Instance, basePtr, nwVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getArgument (IntPtr ctx, IntPtr fn, uint index);
        public static IntPtr GetArgument (IntPtr ctx, IntPtr fn, uint index) {
            return getArgument(ctx, fn, index);
        }
        public IntPtr GetArgument (IntPtr fn, uint index) {
            return getArgument(Instance, fn, index);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr negate (IntPtr ctx, IntPtr subEx, sbyte op, bool isUnsigned, IntPtr irb);
        public static IntPtr Negate (IntPtr ctx, IntPtr subEx, sbyte op, bool isUnsigned, IntPtr irb) {
            return negate(ctx, subEx, op, isUnsigned, irb);
        }
        public IntPtr Negate (IntPtr subEx, sbyte op, bool isUnsigned, IntPtr irb) {
            return negate(Instance, subEx, op, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr arithmeticBinOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb);
        public static IntPtr ArithmeticBinOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return arithmeticBinOp(ctx, lhs, rhs, op, isUnsigned, irb);
        }
        public IntPtr ArithmeticBinOp (IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return arithmeticBinOp(Instance, lhs, rhs, op, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr arithmeticUpdateAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb);
        public static IntPtr ArithmeticUpdateAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return arithmeticUpdateAcqRel(ctx, basePtr, rhs, op, isUnsigned, irb);
        }
        public IntPtr ArithmeticUpdateAcqRel (IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return arithmeticUpdateAcqRel(Instance, basePtr, rhs, op, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr bitwiseBinop (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb);
        public static IntPtr BitwiseBinop (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return bitwiseBinop(ctx, lhs, rhs, op, isUnsigned, irb);
        }
        public IntPtr BitwiseBinop (IntPtr lhs, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return bitwiseBinop(Instance, lhs, rhs, op, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr bitwiseUpdateAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb);
        public static IntPtr BitwiseUpdateAcqRel (IntPtr ctx, IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return bitwiseUpdateAcqRel(ctx, basePtr, rhs, op, isUnsigned, irb);
        }
        public IntPtr BitwiseUpdateAcqRel (IntPtr basePtr, IntPtr rhs, sbyte op, bool isUnsigned, IntPtr irb) {
            return bitwiseUpdateAcqRel(Instance, basePtr, rhs, op, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr shiftOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, bool leftShift, bool isUnsigned, IntPtr irb);
        public static IntPtr ShiftOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, bool leftShift, bool isUnsigned, IntPtr irb) {
            return shiftOp(ctx, lhs, rhs, leftShift, isUnsigned, irb);
        }
        public IntPtr ShiftOp (IntPtr lhs, IntPtr rhs, bool leftShift, bool isUnsigned, IntPtr irb) {
            return shiftOp(Instance, lhs, rhs, leftShift, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr compareOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool orEqual, bool isUnsigned, IntPtr irb);
        public static IntPtr CompareOp (IntPtr ctx, IntPtr lhs, IntPtr rhs, sbyte op, bool orEqual, bool isUnsigned, IntPtr irb) {
            return compareOp(ctx, lhs, rhs, op, orEqual, isUnsigned, irb);
        }
        public IntPtr CompareOp (IntPtr lhs, IntPtr rhs, sbyte op, bool orEqual, bool isUnsigned, IntPtr irb) {
            return compareOp(Instance, lhs, rhs, op, orEqual, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr ptrDiff (IntPtr ctx, IntPtr lhs, IntPtr rhs, IntPtr irb);
        public static IntPtr PtrDiff (IntPtr ctx, IntPtr lhs, IntPtr rhs, IntPtr irb) {
            return ptrDiff(ctx, lhs, rhs, irb);
        }
        public IntPtr PtrDiff (IntPtr lhs, IntPtr rhs, IntPtr irb) {
            return ptrDiff(Instance, lhs, rhs, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool tryCast (IntPtr ctx, IntPtr val, IntPtr dest, ref IntPtr ret, bool isUnsigned, IntPtr irb);
        public static bool TryCast (IntPtr ctx, IntPtr val, IntPtr dest, ref IntPtr ret, bool isUnsigned, IntPtr irb) {
            return tryCast(ctx, val, dest, ref ret, isUnsigned, irb);
        }
        public bool TryCast (IntPtr val, IntPtr dest, ref IntPtr ret, bool isUnsigned, IntPtr irb) {
            return tryCast(Instance, val, dest, ref ret, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr forceCast (IntPtr ctx, IntPtr val, IntPtr dest, bool isUnsigned, IntPtr irb);
        public static IntPtr ForceCast (IntPtr ctx, IntPtr val, IntPtr dest, bool isUnsigned, IntPtr irb) {
            return forceCast(ctx, val, dest, isUnsigned, irb);
        }
        public IntPtr ForceCast (IntPtr val, IntPtr dest, bool isUnsigned, IntPtr irb) {
            return forceCast(Instance, val, dest, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void returnVoid (IntPtr ctx, IntPtr irb);
        public static void ReturnVoid (IntPtr ctx, IntPtr irb) {
            returnVoid(ctx, irb);
        }
        public void ReturnVoid (IntPtr irb) {
            returnVoid(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void returnValue (IntPtr ctx, IntPtr retVal, IntPtr irb);
        public static void ReturnValue (IntPtr ctx, IntPtr retVal, IntPtr irb) {
            returnValue(ctx, retVal, irb);
        }
        public void ReturnValue (IntPtr retVal, IntPtr irb) {
            returnValue(Instance, retVal, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void integerSwitch (IntPtr ctx, IntPtr compareInt, IntPtr defaultTarget, IntPtr[] conditionalTargets, uint numCondTargets, IntPtr[] conditions, uint numConditions, IntPtr irb);
        public static void IntegerSwitch (IntPtr ctx, IntPtr compareInt, IntPtr defaultTarget, IntPtr[] conditionalTargets, IntPtr[] conditions, IntPtr irb) {
            integerSwitch(ctx, compareInt, defaultTarget, conditionalTargets, (uint)conditionalTargets.Length, conditions, (uint)conditions.Length, irb);
        }
        public void IntegerSwitch (IntPtr compareInt, IntPtr defaultTarget, IntPtr[] conditionalTargets, IntPtr[] conditions, IntPtr irb) {
            integerSwitch(Instance, compareInt, defaultTarget, conditionalTargets, (uint)conditionalTargets.Length, conditions, (uint)conditions.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr marshalMainMethodCMDLineArgs (IntPtr ctx, IntPtr managedMain, IntPtr unmanagedMain, IntPtr irb);
        public static IntPtr MarshalMainMethodCMDLineArgs (IntPtr ctx, IntPtr managedMain, IntPtr unmanagedMain, IntPtr irb) {
            return marshalMainMethodCMDLineArgs(ctx, managedMain, unmanagedMain, irb);
        }
        public IntPtr MarshalMainMethodCMDLineArgs (IntPtr managedMain, IntPtr unmanagedMain, IntPtr irb) {
            return marshalMainMethodCMDLineArgs(Instance, managedMain, unmanagedMain, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getInt128 (IntPtr ctx, ulong hi, ulong lo);
        public static IntPtr GetInt128 (IntPtr ctx, ulong hi, ulong lo) {
            return getInt128(ctx, hi, lo);
        }
        public IntPtr GetInt128 (ulong hi, ulong lo) {
            return getInt128(Instance, hi, lo);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getInt64 (IntPtr ctx, ulong val);
        public static IntPtr GetInt64 (IntPtr ctx, ulong val) {
            return getInt64(ctx, val);
        }
        public IntPtr GetInt64 (ulong val) {
            return getInt64(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getIntSZ (IntPtr ctx, ulong val);
        public static IntPtr GetIntSZ (IntPtr ctx, ulong val) {
            return getIntSZ(ctx, val);
        }
        public IntPtr GetIntSZ (ulong val) {
            return getIntSZ(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getInt32 (IntPtr ctx, uint val);
        public static IntPtr GetInt32 (IntPtr ctx, uint val) {
            return getInt32(ctx, val);
        }
        public IntPtr GetInt32 (uint val) {
            return getInt32(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getInt16 (IntPtr ctx, ushort val);
        public static IntPtr GetInt16 (IntPtr ctx, ushort val) {
            return getInt16(ctx, val);
        }
        public IntPtr GetInt16 (ushort val) {
            return getInt16(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getInt8 (IntPtr ctx, byte val);
        public static IntPtr GetInt8 (IntPtr ctx, byte val) {
            return getInt8(ctx, val);
        }
        public IntPtr GetInt8 (byte val) {
            return getInt8(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getString (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string strlit, IntPtr irb);
        public static IntPtr GetString (IntPtr ctx, string strlit, IntPtr irb) {
            return getString(ctx, strlit, irb);
        }
        public IntPtr GetString (string strlit, IntPtr irb) {
            return getString(Instance, strlit, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr True (IntPtr ctx);
        public IntPtr True () {
            return True(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr False (IntPtr ctx);
        public IntPtr False () {
            return False(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getFloat (IntPtr ctx, float val);
        public static IntPtr GetFloat (IntPtr ctx, float val) {
            return getFloat(ctx, val);
        }
        public IntPtr GetFloat (float val) {
            return getFloat(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getDouble (IntPtr ctx, double val);
        public static IntPtr GetDouble (IntPtr ctx, double val) {
            return getDouble(ctx, val);
        }
        public IntPtr GetDouble (double val) {
            return getDouble(Instance, val);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getNullPtr (IntPtr ctx);
        public static IntPtr GetNullPtr (IntPtr ctx) {
            return getNullPtr(ctx);
        }
        public IntPtr GetNullPtr () {
            return getNullPtr(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getAllZeroValue (IntPtr ctx, IntPtr ty);
        public static IntPtr GetAllZeroValue (IntPtr ctx, IntPtr ty) {
            return getAllZeroValue(ctx, ty);
        }
        public IntPtr GetAllZeroValue (IntPtr ty) {
            return getAllZeroValue(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getAllOnesValue (IntPtr ctx, IntPtr ty);
        public static IntPtr GetAllOnesValue (IntPtr ctx, IntPtr ty) {
            return getAllOnesValue(ctx, ty);
        }
        public IntPtr GetAllOnesValue (IntPtr ty) {
            return getAllOnesValue(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getConstantStruct (IntPtr ctx, IntPtr ty, IntPtr[] values, uint valuec);
        public static IntPtr GetConstantStruct (IntPtr ctx, IntPtr ty, IntPtr[] values) {
            return getConstantStruct(ctx, ty, values, (uint)values.Length);
        }
        public IntPtr GetConstantStruct (IntPtr ty, IntPtr[] values) {
            return getConstantStruct(Instance, ty, values, (uint)values.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getStructValue (IntPtr ctx, IntPtr ty, IntPtr[] values, uint valuec, IntPtr irb);
        public static IntPtr GetStructValue (IntPtr ctx, IntPtr ty, IntPtr[] values, IntPtr irb) {
            return getStructValue(ctx, ty, values, (uint)values.Length, irb);
        }
        public IntPtr GetStructValue (IntPtr ty, IntPtr[] values, IntPtr irb) {
            return getStructValue(Instance, ty, values, (uint)values.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getCall (IntPtr ctx, IntPtr fn, IntPtr[] args, uint argc, IntPtr irb);
        public static IntPtr GetCall (IntPtr ctx, IntPtr fn, IntPtr[] args, IntPtr irb) {
            return getCall(ctx, fn, args, (uint)args.Length, irb);
        }
        public IntPtr GetCall (IntPtr fn, IntPtr[] args, IntPtr irb) {
            return getCall(Instance, fn, args, (uint)args.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void branch (IntPtr ctx, IntPtr dest, IntPtr irb);
        public static void Branch (IntPtr ctx, IntPtr dest, IntPtr irb) {
            branch(ctx, dest, irb);
        }
        public void Branch (IntPtr dest, IntPtr irb) {
            branch(Instance, dest, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void conditionalBranch (IntPtr ctx, IntPtr destTrue, IntPtr destFalse, IntPtr cond, IntPtr irb);
        public static void ConditionalBranch (IntPtr ctx, IntPtr destTrue, IntPtr destFalse, IntPtr cond, IntPtr irb) {
            conditionalBranch(ctx, destTrue, destFalse, cond, irb);
        }
        public void ConditionalBranch (IntPtr destTrue, IntPtr destFalse, IntPtr cond, IntPtr irb) {
            conditionalBranch(Instance, destTrue, destFalse, cond, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void unreachable (IntPtr ctx, IntPtr irb);
        public static void Unreachable (IntPtr ctx, IntPtr irb) {
            unreachable(ctx, irb);
        }
        public void Unreachable (IntPtr irb) {
            unreachable(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr conditionalSelect (IntPtr ctx, IntPtr valTrue, IntPtr valFalse, IntPtr cond, IntPtr irb);
        public static IntPtr ConditionalSelect (IntPtr ctx, IntPtr valTrue, IntPtr valFalse, IntPtr cond, IntPtr irb) {
            return conditionalSelect(ctx, valTrue, valFalse, cond, irb);
        }
        public IntPtr ConditionalSelect (IntPtr valTrue, IntPtr valFalse, IntPtr cond, IntPtr irb) {
            return conditionalSelect(Instance, valTrue, valFalse, cond, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr extractValue (IntPtr ctx, IntPtr aggregate, uint index, IntPtr irb);
        public static IntPtr ExtractValue (IntPtr ctx, IntPtr aggregate, uint index, IntPtr irb) {
            return extractValue(ctx, aggregate, index, irb);
        }
        public IntPtr ExtractValue (IntPtr aggregate, uint index, IntPtr irb) {
            return extractValue(Instance, aggregate, index, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr extractNestedValue (IntPtr ctx, IntPtr aggregate, uint[] indices, uint idxCount, IntPtr irb);
        public static IntPtr ExtractNestedValue (IntPtr ctx, IntPtr aggregate, uint[] indices, IntPtr irb) {
            return extractNestedValue(ctx, aggregate, indices, (uint)indices.Length, irb);
        }
        public IntPtr ExtractNestedValue (IntPtr aggregate, uint[] indices, IntPtr irb) {
            return extractNestedValue(Instance, aggregate, indices, (uint)indices.Length, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr isNotNull (IntPtr ctx, IntPtr val, IntPtr irb);
        public static IntPtr IsNotNull (IntPtr ctx, IntPtr val, IntPtr irb) {
            return isNotNull(ctx, val, irb);
        }
        public IntPtr IsNotNull (IntPtr val, IntPtr irb) {
            return isNotNull(Instance, val, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getSizeOf (IntPtr ctx, IntPtr ty);
        public static IntPtr GetSizeOf (IntPtr ctx, IntPtr ty) {
            return getSizeOf(ctx, ty);
        }
        public IntPtr GetSizeOf (IntPtr ty) {
            return getSizeOf(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getI32SizeOf (IntPtr ctx, IntPtr ty);
        public static IntPtr GetI32SizeOf (IntPtr ctx, IntPtr ty) {
            return getI32SizeOf(ctx, ty);
        }
        public IntPtr GetI32SizeOf (IntPtr ty) {
            return getI32SizeOf(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getI64SizeOf (IntPtr ctx, IntPtr ty);
        public static IntPtr GetI64SizeOf (IntPtr ctx, IntPtr ty) {
            return getI64SizeOf(ctx, ty);
        }
        public IntPtr GetI64SizeOf (IntPtr ty) {
            return getI64SizeOf(Instance, ty);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getMin (IntPtr ctx, IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb);
        public static IntPtr GetMin (IntPtr ctx, IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb) {
            return getMin(ctx, v1, v2, isUnsigned, irb);
        }
        public IntPtr GetMin (IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb) {
            return getMin(Instance, v1, v2, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getMax (IntPtr ctx, IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb);
        public static IntPtr GetMax (IntPtr ctx, IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb) {
            return getMax(ctx, v1, v2, isUnsigned, irb);
        }
        public IntPtr GetMax (IntPtr v1, IntPtr v2, bool isUnsigned, IntPtr irb) {
            return getMax(Instance, v1, v2, isUnsigned, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr memoryCopy (IntPtr ctx, IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb);
        public static IntPtr MemoryCopy (IntPtr ctx, IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb) {
            return memoryCopy(ctx, dest, src, byteCount, isVolatile, irb);
        }
        public IntPtr MemoryCopy (IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb) {
            return memoryCopy(Instance, dest, src, byteCount, isVolatile, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr memoryMove (IntPtr ctx, IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb);
        public static IntPtr MemoryMove (IntPtr ctx, IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb) {
            return memoryMove(ctx, dest, src, byteCount, isVolatile, irb);
        }
        public IntPtr MemoryMove (IntPtr dest, IntPtr src, IntPtr byteCount, bool isVolatile, IntPtr irb) {
            return memoryMove(Instance, dest, src, byteCount, isVolatile, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool currentBlockIsTerminated (IntPtr ctx, IntPtr irb);
        public static bool CurrentBlockIsTerminated (IntPtr ctx, IntPtr irb) {
            return currentBlockIsTerminated(ctx, irb);
        }
        public bool CurrentBlockIsTerminated (IntPtr irb) {
            return currentBlockIsTerminated(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool currentBlockHasPredecessor (IntPtr ctx, IntPtr irb);
        public static bool CurrentBlockHasPredecessor (IntPtr ctx, IntPtr irb) {
            return currentBlockHasPredecessor(ctx, irb);
        }
        public bool CurrentBlockHasPredecessor (IntPtr irb) {
            return currentBlockHasPredecessor(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern uint currentBlockCountPredecessors (IntPtr ctx, IntPtr irb);
        public static uint CurrentBlockCountPredecessors (IntPtr ctx, IntPtr irb) {
            return currentBlockCountPredecessors(ctx, irb);
        }
        public uint CurrentBlockCountPredecessors (IntPtr irb) {
            return currentBlockCountPredecessors(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void currentBlockGetPredecessors (IntPtr ctx, IntPtr irb, IntPtr[] bbs, uint bbc);
        public static void CurrentBlockGetPredecessors (IntPtr ctx, IntPtr irb, IntPtr[] bbs) {
            currentBlockGetPredecessors(ctx, irb, bbs, (uint)bbs.Length);
        }
        public void CurrentBlockGetPredecessors (IntPtr irb, IntPtr[] bbs) {
            currentBlockGetPredecessors(Instance, irb, bbs, (uint)bbs.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getCurrentBasicBlock (IntPtr ctx, IntPtr irb);
        public static IntPtr GetCurrentBasicBlock (IntPtr ctx, IntPtr irb) {
            return getCurrentBasicBlock(ctx, irb);
        }
        public IntPtr GetCurrentBasicBlock (IntPtr irb) {
            return getCurrentBasicBlock(Instance, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void optimize (IntPtr ctx, byte optLvl, byte maxIterations);
        public static void Optimize (IntPtr ctx, byte optLvl, byte maxIterations) {
            optimize(ctx, optLvl, maxIterations);
        }
        public void Optimize (byte optLvl, byte maxIterations) {
            optimize(Instance, optLvl, maxIterations);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void linkTimeOptimization (IntPtr ctx);
        public static void LinkTimeOptimization (IntPtr ctx) {
            linkTimeOptimization(ctx);
        }
        public void LinkTimeOptimization () {
            linkTimeOptimization(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void forceVectorizationForAllLoops (IntPtr ctx, IntPtr fn);
        public static void ForceVectorizationForAllLoops (IntPtr ctx, IntPtr fn) {
            forceVectorizationForAllLoops(ctx, fn);
        }
        public void ForceVectorizationForAllLoops (IntPtr fn) {
            forceVectorizationForAllLoops(Instance, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool forceVectorizationForCurrentLoop (IntPtr ctx, IntPtr loopBB, IntPtr fn);
        public static bool ForceVectorizationForCurrentLoop (IntPtr ctx, IntPtr loopBB, IntPtr fn) {
            return forceVectorizationForCurrentLoop(ctx, loopBB, fn);
        }
        public bool ForceVectorizationForCurrentLoop (IntPtr loopBB, IntPtr fn) {
            return forceVectorizationForCurrentLoop(Instance, loopBB, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getVTable (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr vtableTy, IntPtr[] virtualMethods, uint virtualMethodc, IntPtr superVtable);
        public static IntPtr GetVTable (IntPtr ctx, string name, IntPtr vtableTy, IntPtr[] virtualMethods, IntPtr superVtable) {
            return getVTable(ctx, name, vtableTy, virtualMethods, (uint)virtualMethods.Length, superVtable);
        }
        public IntPtr GetVTable (string name, IntPtr vtableTy, IntPtr[] virtualMethods, IntPtr superVtable) {
            return getVTable(Instance, name, vtableTy, virtualMethods, (uint)virtualMethods.Length, superVtable);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getImplicitVTableType (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr[] virtualMethods, uint virtualMethodc);
        public static IntPtr GetImplicitVTableType (IntPtr ctx, string name, IntPtr[] virtualMethods) {
            return getImplicitVTableType(ctx, name, virtualMethods, (uint)virtualMethods.Length);
        }
        public IntPtr GetImplicitVTableType (string name, IntPtr[] virtualMethods) {
            return getImplicitVTableType(Instance, name, virtualMethods, (uint)virtualMethods.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr getVTableType (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr[] fnTypes, uint fnTypec);
        public static IntPtr GetVTableType (IntPtr ctx, string name, IntPtr[] fnTypes) {
            return getVTableType(ctx, name, fnTypes, (uint)fnTypes.Length);
        }
        public IntPtr GetVTableType (string name, IntPtr[] fnTypes) {
            return getVTableType(Instance, name, fnTypes, (uint)fnTypes.Length);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr isInst (IntPtr ctx, IntPtr vtablePtr, [MarshalAs(UnmanagedType.LPUTF8Str)]string typeName, IntPtr irb);
        public static IntPtr IsInst (IntPtr ctx, IntPtr vtablePtr, string typeName, IntPtr irb) {
            return isInst(ctx, vtablePtr, typeName, irb);
        }
        public IntPtr IsInst (IntPtr vtablePtr, string typeName, IntPtr irb) {
            return isInst(Instance, vtablePtr, typeName, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool isAlloca (IntPtr ctx, IntPtr val);
        public static bool IsAlloca (IntPtr ctx, IntPtr val) {
            return isAlloca(ctx, val);
        }
        public bool IsAlloca (IntPtr val) {
            return isAlloca(Instance, val);
        }
        public static implicit operator IntPtr (ManagedContext inst) {
            return inst != null ? inst.Instance : IntPtr.Zero;
        }
    }
    public class BasicBlock {
        public readonly IntPtr Instance;

        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr createBasicBlock (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr fn);
        public  BasicBlock (IntPtr ctx, string name, IntPtr fn) {
            Instance = createBasicBlock(ctx, name, fn);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool isTerminated (IntPtr bb);
        public static bool IsTerminated (IntPtr bb) {
            return isTerminated(bb);
        }
        public bool IsTerminated () {
            return isTerminated(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern bool hasPredecessor (IntPtr bb);
        public static bool HasPredecessor (IntPtr bb) {
            return hasPredecessor(bb);
        }
        public bool HasPredecessor () {
            return hasPredecessor(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern uint countPredecessors (IntPtr bb);
        public static uint CountPredecessors (IntPtr bb) {
            return countPredecessors(bb);
        }
        public uint CountPredecessors () {
            return countPredecessors(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void removeBasicBlock (IntPtr bb);
        public static void RemoveBasicBlock (IntPtr bb) {
            removeBasicBlock(bb);
        }
        public void RemoveBasicBlock () {
            removeBasicBlock(Instance);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void getPredecessors (IntPtr curr, IntPtr[] bbs, uint bbc);
        public static void GetPredecessors (IntPtr curr, IntPtr[] bbs) {
            getPredecessors(curr, bbs, (uint)bbs.Length);
        }
        public void GetPredecessors (IntPtr[] bbs) {
            getPredecessors(Instance, bbs, (uint)bbs.Length);
        }
        public static implicit operator IntPtr (BasicBlock inst) {
            return inst != null ? inst.Instance : IntPtr.Zero;
        }
    }
    public class IRBuilder : IDisposable {
        public readonly IntPtr Instance;
        private bool isDisposed = false;

        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr CreateIRBuilder (IntPtr ctx);
        public  IRBuilder (IntPtr ctx) {
            Instance = CreateIRBuilder(ctx);
        }
        ~IRBuilder() {
            Dispose();
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void DisposeIRBuilder (IntPtr irb);
        public void Dispose () {
            if (isDisposed)return; isDisposed = true;
            DisposeIRBuilder(Instance);
        }
        public static implicit operator IntPtr (IRBuilder inst) {
            return inst != null ? inst.Instance : IntPtr.Zero;
        }
    }
    public class PHINode {
        public readonly IntPtr Instance;

        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr createPHINode (IntPtr ctx, IntPtr ty, uint numCases, IntPtr irb);
        public  PHINode (IntPtr ctx, IntPtr ty, uint numCases, IntPtr irb) {
            Instance = createPHINode(ctx, ty, numCases, irb);
        }
        [DllImport(@"LLVMInterface.dll")]
        private static extern void addMergePoint (IntPtr phi, IntPtr val, IntPtr prev);
        public static void AddMergePoint (IntPtr phi, IntPtr val, IntPtr prev) {
            addMergePoint(phi, val, prev);
        }
        public void AddMergePoint (IntPtr val, IntPtr prev) {
            addMergePoint(Instance, val, prev);
        }
        public static implicit operator IntPtr (PHINode inst) {
            return inst != null ? inst.Instance : IntPtr.Zero;
        }
    }
    public class VTable {
        public readonly IntPtr Instance;

        [DllImport(@"LLVMInterface.dll")]
        private static extern IntPtr createVTable (IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)]string name, IntPtr vtableTy, IntPtr[] virtualMethods, uint virtualMethodc);
        public  VTable (IntPtr ctx, string name, IntPtr vtableTy, IntPtr[] virtualMethods) {
            Instance = createVTable(ctx, name, vtableTy, virtualMethods, (uint)virtualMethods.Length);
        }
        public static implicit operator IntPtr (VTable inst) {
            return inst != null ? inst.Instance : IntPtr.Zero;
        }
    }
}
