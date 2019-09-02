/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <memory>
#include <string>
#include <vector>
#include "importdef.h"
namespace llvm {
    class Module;
    class LLVMContext;
    class Type;
    class Function;
    class ConstantFolder;
    class IRBuilderDefaultInserter;
    template <typename T = ConstantFolder,
        typename Inserter = IRBuilderDefaultInserter>
        class IRBuilder;
    class BasicBlock;
    class Value;
    class Constant;
    class PHINode;
    class Attribute;
}
namespace {
    class AllocationRemovingPass;
}
struct ManagedContext
{
    llvm::Module* M;
    llvm::LLVMContext* context;
    llvm::Type* stringTy = 0;
    llvm::IRBuilder<>* builder = 0;
    AllocationRemovingPass* arp = 0;

    ManagedContext(const char* name, const char* filename );
    ~ManagedContext();

};
EXTERN_API(ManagedContext)* CreateManagedContext(const char* name, const char* filename);
EXTERN_API(void) DisposeManagedContext(ManagedContext* ctx);

EXTERN_API(void) Save(ManagedContext* ctx, const char* filename);
EXTERN_API(void) DumpModule(ManagedContext* ctx, const char* filename);
EXTERN_API(void) PrintValueDump(ManagedContext* ctx, llvm::Value* val);
EXTERN_API(void) PrintTypeDump(ManagedContext* ctx, llvm::Type* ty);
EXTERN_API(void) PrintFunctionDump(ManagedContext* ctx, llvm::Function* fn);
EXTERN_API(void) PrintBasicBlockDump(ManagedContext* ctx, llvm::BasicBlock* bb);

EXTERN_API(bool) VerifyModule(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getStruct(ManagedContext* ctx, const char* name);
EXTERN_API(void) completeStruct(ManagedContext* ctx, llvm::Type* str, llvm::Type* const* body, uint32_t bodyLen);
EXTERN_API(llvm::Type)* getUnnamedStruct(ManagedContext* ctx, llvm::Type* const* body, uint32_t bodyLen);
EXTERN_API(llvm::Type)* getBoolType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getByteType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getShortType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getIntType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getLongType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getBiglongType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getFloatType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getDoubleType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getStringType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getArrayType(ManagedContext* ctx, llvm::Type* elem, uint32_t count);
EXTERN_API(llvm::Type)* getPointerType(ManagedContext* ctx, llvm::Type* pointsTo);
EXTERN_API(llvm::Type)* getVoidPtr(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getVoidType(ManagedContext* ctx);
EXTERN_API(llvm::Type)* getOpaqueType(ManagedContext* ctx, const char* name);
EXTERN_API(llvm::Type)* getSizeTType(ManagedContext* ctx);

EXTERN_API(llvm::Type)* getTypeFromValue(ManagedContext* ctx, llvm::Value* val);

EXTERN_API(llvm::Function)* declareFunction(ManagedContext* ctx, const char* name, llvm::Type* retTy, llvm::Type* const* argTys, uint32_t argc, const char** argNames, uint32_t argNamec, bool isPublic);
EXTERN_API(llvm::Function)* declareFunctionOfType(ManagedContext* ctx, const char* name, llvm::Type* fnPtrTy, bool isPublic);
EXTERN_API(void) addFunctionAttributes(ManagedContext* ctx, llvm::Function* fn, const char** atts, uint32_t attc);
EXTERN_API(void) addParamAttributes(ManagedContext* ctx, llvm::Function* fn, const char** atts, uint32_t attc, uint32_t paramIdx);
EXTERN_API(void) addReturnNoAliasAttribute(ManagedContext* ctx, llvm::Function* fn);
EXTERN_API(void) addReturnNotNullAttribute(ManagedContext* ctx, llvm::Function* fn);
EXTERN_API(llvm::Function)* declareMallocFunction(ManagedContext* ctx, const char* name, llvm::Type* const* argTys, uint32_t argc, bool resultNotNull);
EXTERN_API(llvm::Type)* getFunctionPtrTypeFromFunction(ManagedContext* ctx, llvm::Function* fn);
EXTERN_API(llvm::Type)* getFunctionPtrType(ManagedContext* ctx, llvm::Type* retTy, llvm::Type** argTys, uint32_t argc);

EXTERN_API(llvm::BasicBlock)* createBasicBlock(ManagedContext* ctx, const char* name, llvm::Function* fn);
EXTERN_API(void) resetInsertPoint(ManagedContext* ctx, llvm::BasicBlock* bb, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* defineAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, const char* name);
EXTERN_API(llvm::Value)* defineZeroinitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, const char* name, llvm::IRBuilder<>* irb, bool addlifetime);
EXTERN_API(llvm::Value)* defineInitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, llvm::Value* initVal, const char* name, llvm::IRBuilder<>* irb, bool addlifetime);
EXTERN_API(llvm::Value)* defineTypeInferredInitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Value* initVal, const char* name, llvm::IRBuilder<>* irb, bool addlifetime);
EXTERN_API(void) endLifeTime(ManagedContext* ctx, llvm::Value* ptr, llvm::IRBuilder<>* irb);


EXTERN_API(llvm::IRBuilder<>)* CreateIRBuilder(ManagedContext* ctx);
EXTERN_API(void) DisposeIRBuilder(llvm::IRBuilder<>* irb);

EXTERN_API(llvm::Value)* loadField(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* loadFieldConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t geIdxCount, llvm::IRBuilder<>* irb);
EXTERN_API(void) storeField(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(void) storeFieldConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* GetElementPtr(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* GetElementPtrConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* GetElementPtrConstIdxWithType(ManagedContext* ctx, llvm::Type* pointeeTy, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* load(ManagedContext* ctx, llvm::Value* basePtr, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* loadAcquire(ManagedContext* ctx, llvm::Value* basePtr, llvm::IRBuilder<>* irb);
EXTERN_API(void) store(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(void) storeRelease(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* compareExchangeAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* cmp, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* exchangeAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* getArgument(ManagedContext* ctx, llvm::Function* fn, uint32_t index);

EXTERN_API(llvm::Value)* negate(ManagedContext* ctx, llvm::Value* subEx, char op, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* arithmeticBinOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* arithmeticUpdateAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* bitwiseBinop(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* bitwiseUpdateAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* shiftOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, bool leftShift, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* compareOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool orEqual, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* ptrDiff(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, llvm::IRBuilder<>* irb);
EXTERN_API(bool) tryCast(ManagedContext* ctx, llvm::Value* val, llvm::Type* dest, llvm::Value*& ret, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* forceCast(ManagedContext* ctx, llvm::Value* val, llvm::Type* dest, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(void) returnVoid(ManagedContext* ctx, llvm::IRBuilder<>* irb);
EXTERN_API(void) returnValue(ManagedContext* ctx, llvm::Value* retVal, llvm::IRBuilder<>* irb);
EXTERN_API(void) integerSwitch(ManagedContext* ctx, llvm::Value* compareInt, llvm::BasicBlock* defaultTarget, llvm::BasicBlock** conditionalTargets, uint32_t numCondTargets, llvm::Value** conditions, uint32_t numConditions, llvm::IRBuilder<>* irb);

EXTERN_API(llvm::Value)* marshalMainMethodCMDLineArgs(ManagedContext* ctx, llvm::Function* managedMain, llvm::Function* unmanagedMain, llvm::IRBuilder<>* irb);

EXTERN_API(llvm::Constant)* getInt128(ManagedContext* ctx, uint64_t hi, uint64_t lo);
EXTERN_API(llvm::Constant)* getInt64(ManagedContext* ctx, uint64_t val);
EXTERN_API(llvm::Constant)* getIntSZ(ManagedContext* ctx, uint64_t val);
EXTERN_API(llvm::Constant)* getInt32(ManagedContext* ctx, uint32_t val);
EXTERN_API(llvm::Constant)* getInt16(ManagedContext* ctx, uint16_t val);
EXTERN_API(llvm::Constant)* getInt8(ManagedContext* ctx, uint8_t val);
EXTERN_API(llvm::Constant)* getString(ManagedContext* ctx, const char* strlit, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Constant)* True(ManagedContext* ctx);
EXTERN_API(llvm::Constant)* False(ManagedContext* ctx);
EXTERN_API(llvm::Constant)* getFloat(ManagedContext* ctx, float val);
EXTERN_API(llvm::Constant)* getDouble(ManagedContext* ctx, double val);
EXTERN_API(llvm::Constant)* getNullPtr(ManagedContext* ctx);
EXTERN_API(llvm::Constant)* getAllZeroValue(ManagedContext* ctx, llvm::Type* ty);
EXTERN_API(llvm::Constant)* getAllOnesValue(ManagedContext* ctx, llvm::Type* ty);
EXTERN_API(llvm::Constant)* getConstantStruct(ManagedContext* ctx, llvm::Type* ty, llvm::Constant** values, uint32_t valuec);

EXTERN_API(llvm::Value)* getStructValue(ManagedContext* ctx, llvm::Type* ty, llvm::Value** values, uint32_t valuec, llvm::IRBuilder<>* irb);


EXTERN_API(llvm::Value)* getCall(ManagedContext* ctx, llvm::Function* fn, llvm::Value** args, uint32_t argc, llvm::IRBuilder<>* irb);
EXTERN_API(void) branch(ManagedContext* ctx, llvm::BasicBlock* dest, llvm::IRBuilder<>* irb);
EXTERN_API(void) conditionalBranch(ManagedContext* ctx, llvm::BasicBlock* destTrue, llvm::BasicBlock* destFalse, llvm::Value* cond, llvm::IRBuilder<>* irb);
EXTERN_API(void) unreachable(ManagedContext* ctx, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* conditionalSelect(ManagedContext* ctx, llvm::Value* valTrue, llvm::Value* valFalse, llvm::Value* cond, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* extractValue(ManagedContext* ctx, llvm::Value* aggregate, uint32_t index, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* extractNestedValue(ManagedContext* ctx, llvm::Value* aggregate, uint32_t* indices, uint32_t idxCount, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::PHINode)* createPHINode(ManagedContext* ctx, llvm::Type* ty, uint32_t numCases, llvm::IRBuilder<>* irb);
EXTERN_API(void) addMergePoint(llvm::PHINode* phi, llvm::Value* val, llvm::BasicBlock* prev);
EXTERN_API(llvm::Value)* isNotNull(ManagedContext* ctx, llvm::Value* val, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* getSizeOf(ManagedContext* ctx, llvm::Type* ty);
EXTERN_API(llvm::Value)* getI32SizeOf(ManagedContext* ctx, llvm::Type* ty);
EXTERN_API(llvm::Value)* getI64SizeOf(ManagedContext* ctx, llvm::Type* ty);

EXTERN_API(llvm::Value)* getMin(ManagedContext* ctx, llvm::Value* v1, llvm::Value* v2, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* getMax(ManagedContext* ctx, llvm::Value* v1, llvm::Value* v2, bool isUnsigned, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* memoryCopy(ManagedContext* ctx, llvm::Value* dest, llvm::Value* src, llvm::Value* byteCount, bool isVolatile, llvm::IRBuilder<>* irb);
EXTERN_API(llvm::Value)* memoryMove(ManagedContext* ctx, llvm::Value* dest, llvm::Value* src, llvm::Value* byteCount, bool isVolatile, llvm::IRBuilder<>* irb);

EXTERN_API(bool) currentBlockIsTerminated(ManagedContext* ctx, llvm::IRBuilder<>* irb);
EXTERN_API(bool) isTerminated(llvm::BasicBlock* bb);
EXTERN_API(bool) hasPredecessor(llvm::BasicBlock* bb);
EXTERN_API(bool) currentBlockHasPredecessor(ManagedContext* ctx, llvm::IRBuilder<>* irb);
EXTERN_API(uint32_t) currentBlockCountPredecessors(ManagedContext* ctx, llvm::IRBuilder<>* irb);
EXTERN_API(uint32_t) countPredecessors(llvm::BasicBlock* bb);
EXTERN_API(void) currentBlockGetPredecessors(ManagedContext* ctx, llvm::IRBuilder<>* irb, llvm::BasicBlock** bbs, uint32_t bbc);
EXTERN_API(void) removeBasicBlock(llvm::BasicBlock* bb);
EXTERN_API(void) getPredecessors(llvm::BasicBlock* curr, llvm::BasicBlock** bbs, uint32_t bbc);
EXTERN_API(llvm::BasicBlock)* getCurrentBasicBlock(ManagedContext* ctx, llvm::IRBuilder<>* irb);

EXTERN_API(void) optimize(ManagedContext* ctx, uint8_t optLvl, uint8_t maxIterations);
EXTERN_API(void) linkTimeOptimization(ManagedContext* ctx);
EXTERN_API(void) forceVectorizationForAllLoops(ManagedContext* ctx, llvm::Function* fn);
EXTERN_API(bool) forceVectorizationForCurrentLoop(ManagedContext* ctx, llvm::BasicBlock* loopBB, llvm::Function* fn);

EXTERN_API(llvm::Value)* createVTable(ManagedContext* ctx, const char* name, llvm::Type* vtableTy, llvm::Function** virtualMethods, uint32_t virtualMethodc);
EXTERN_API(llvm::Value)* getVTable(ManagedContext* ctx, const char* name, llvm::Type* vtableTy, llvm::Function** virtualMethods, uint32_t virtualMethodc, llvm::Value* superVtable);
EXTERN_API(llvm::Type)* getImplicitVTableType(ManagedContext* ctx, const char* name, llvm::Function** virtualMethods, uint32_t virtualMethodc);
EXTERN_API(llvm::Type)* getVTableType(ManagedContext* ctx, const char* name, llvm::Type** fnTypes, uint32_t fnTypec);
EXTERN_API(llvm::Value)* isInst(ManagedContext* ctx, llvm::Value* vtablePtr, const char* typeName, llvm::IRBuilder<>* irb);
