#include <llvm/IR/Module.h>

#include <llvm/ADT/SmallVector.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/Support/FileSystem.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Bitcode/BitcodeWriter.h>
#include <llvm/IR/Type.h>
#include <llvm/IR/IRBuilder.h>
#include <llvm/IR/BasicBlock.h>
#include <llvm/IR/Constants.h>
#include <llvm/IR/Metadata.h>

#include <llvm/IR/LegacyPassManager.h>
#include <llvm/Analysis/LoopPass.h>
#include <llvm/Analysis/LoopInfo.h>
#include <llvm/Analysis/LoopAnalysisManager.h>
#include <llvm/Transforms/Vectorize.h>
#include <llvm/Transforms/Vectorize/LoopVectorize.h>
#include <llvm/Transforms/Vectorize/LoopVectorizationLegality.h>
#include <llvm/Transforms/Utils/LoopUtils.h>
#include <llvm/Transforms/Scalar.h>
#include <llvm/Transforms/IPO/PassManagerBuilder.h>
#include <llvm/Transforms/IPO.h>
#include <llvm/Transforms/Instrumentation/PGOInstrumentation.h>

#include <llvm/IR/Verifier.h>
#include <llvm/IR/CFG.h>
#include "AllocationRemovingPass.h"
#include "ToStringRemovingPass.h"
#include "ThrowIfRemovingPass.h"
#include "StrMulRemovingPass.h"
#include "ManagedContext.h"

ManagedContext::ManagedContext(const char* name)
{
    context = new llvm::LLVMContext();
    M = new llvm::Module(name, *context);
    M->setSourceFileName(name);
    M->setModuleIdentifier(name);
    builder = new llvm::IRBuilder<>(*context);
}

ManagedContext::~ManagedContext()
{
    if (builder != nullptr) {
        delete builder;
        builder = nullptr;
    }
    if (M != nullptr) {
        delete M;
        M = nullptr;
    }
    if (context != nullptr) {
        delete context;
        context = nullptr;
    }
}


llvm::Value* primitiveUpCast(llvm::Value* val, llvm::Type* dest, bool isUnsigned, llvm::IRBuilder<>* irb) {
    if (isUnsigned)
        return irb->CreateZExtOrBitCast(val, dest);
    else
        return irb->CreateSExtOrBitCast(val, dest);
}

// lhs, rhs: primitives (integers or floats)
// ret: isFloatingpoint
bool bringToCommonSupertype(ManagedContext* ctx, llvm::Value*& lhs, llvm::Value*& rhs, llvm::IRBuilder<>* irb, bool isUnsigned = false) {
    if (lhs->getType()->isFloatingPointTy()) {
        if (rhs->getType() != lhs->getType()) {
            rhs = primitiveUpCast(rhs, llvm::Type::getDoubleTy(*ctx->context), isUnsigned, irb);
            if (!lhs->getType()->isDoubleTy())
                lhs = primitiveUpCast(lhs, llvm::Type::getDoubleTy(*ctx->context), isUnsigned, irb);
        }
        return true;
    }
    else if (rhs->getType()->isFloatingPointTy()) {
        if (lhs->getType() != rhs->getType()) {
            lhs = primitiveUpCast(lhs, llvm::Type::getDoubleTy(*ctx->context), isUnsigned, irb);
            if (!rhs->getType()->isDoubleTy())
                rhs = primitiveUpCast(rhs, llvm::Type::getDoubleTy(*ctx->context), isUnsigned, irb);
        }
        return true;
    }
    else if (lhs->getType() != rhs->getType()) {
        if (lhs->getType()->isPointerTy() && rhs->getType()->isPointerTy()) {
            rhs = irb->CreatePointerBitCastOrAddrSpaceCast(rhs, lhs->getType());
            return false;
        }
        // assume integer types
        if (lhs->getType()->getIntegerBitWidth() > rhs->getType()->getIntegerBitWidth()) {
            rhs = primitiveUpCast(rhs, lhs->getType(), isUnsigned, irb);
        }
        else if (lhs->getType() != rhs->getType()) {
            lhs = primitiveUpCast(lhs, rhs->getType(), isUnsigned, irb);
        }
        return false;
    }
    return lhs->getType()->isFloatingPointTy();
}

ManagedContext* CreateManagedContext(const char* name)
{

    return new ManagedContext(name);
}

void DisposeManagedContext(ManagedContext* ctx)
{
    if (ctx != nullptr)
        delete ctx;
}

void Save(ManagedContext* ctx, const char* filename)
{
    //llvm::errs() << *ctx->M;
    std::error_code ec;
    llvm::raw_fd_ostream os(filename, ec);
    llvm::WriteBitcodeToFile(*ctx->M, os);
    os.flush();
}

void DumpModule(ManagedContext* ctx, const char* filename)
{
    std::error_code ec;
    llvm::raw_fd_ostream os(filename, ec);

    os << *ctx->M;

    os.flush();
}

EXTERN_API(void) PrintValueDump(ManagedContext* ctx, llvm::Value* val)
{
    llvm::errs() << *val << "\r\n";
}

EXTERN_API(void) PrintTypeDump(ManagedContext* ctx, llvm::Type* ty)
{
    llvm::errs() << *ty << "\r\n";
}

EXTERN_API(void) PrintFunctionDump(ManagedContext* ctx, llvm::Function* fn)
{
    llvm::errs() << *fn << "\r\n";
}

EXTERN_API(void) PrintBasicBlockDump(ManagedContext* ctx, llvm::BasicBlock* bb)
{
    llvm::errs() << *bb << "\r\n";
}



llvm::Type* getStruct(ManagedContext* ctx, const char* name)
{
    return llvm::StructType::create(*ctx->context, name);
}

void completeStruct(ManagedContext* ctx, llvm::Type* str, llvm::Type* const* body, uint32_t bodyLen)
{

    if (bodyLen < 32) {
        ((llvm::StructType*)str)->setBody(llvm::ArrayRef<llvm::Type*>(body, body + bodyLen), false);
    }
}

llvm::Type* getUnnamedStruct(ManagedContext* ctx, llvm::Type* const* body, uint32_t bodyLen)
{
    return llvm::StructType::get(*ctx->context, llvm::ArrayRef<llvm::Type*>(body, body + bodyLen), false);
}

llvm::Type* getBoolType(ManagedContext* ctx)
{
    return llvm::Type::getInt1Ty(*ctx->context);
}

llvm::Type* getByteType(ManagedContext* ctx)
{
    return llvm::Type::getInt8Ty(*ctx->context);
}

llvm::Type* getShortType(ManagedContext* ctx)
{
    return llvm::Type::getInt16Ty(*ctx->context);
}

llvm::Type* getIntType(ManagedContext* ctx)
{
    return llvm::Type::getInt32Ty(*ctx->context);
}

llvm::Type* getLongType(ManagedContext* ctx)
{
    return llvm::Type::getInt64Ty(*ctx->context);
}

llvm::Type* getBiglongType(ManagedContext* ctx)
{
    return llvm::Type::getInt128Ty(*ctx->context);
}

llvm::Type* getFloatType(ManagedContext* ctx)
{
    return llvm::Type::getFloatTy(*ctx->context);
}

llvm::Type* getDoubleType(ManagedContext* ctx)
{
    return llvm::Type::getDoubleTy(*ctx->context);
}

llvm::Type* getStringType(ManagedContext* ctx)
{
    if (ctx->stringTy == nullptr) {
        ctx->stringTy = llvm::StructType::create({ llvm::PointerType::getInt8PtrTy(*ctx->context), getSizeTType(ctx) }, "string", false);
    }
    return ctx->stringTy;
}

llvm::Type* getArrayType(ManagedContext* ctx, llvm::Type* elem, uint32_t count)
{
    return llvm::ArrayType::get(elem, count);
}

llvm::Type* getPointerType(ManagedContext* ctx, llvm::Type* pointsTo)
{
    return llvm::PointerType::get(pointsTo, 0);
}

llvm::Type* getVoidPtr(ManagedContext* ctx)
{
    return llvm::PointerType::getInt8PtrTy(*ctx->context);
}

llvm::Type* getVoidType(ManagedContext* ctx)
{
    return llvm::Type::getVoidTy(*ctx->context);
}

EXTERN_API(llvm::Type)* getOpaqueType(ManagedContext* ctx, const char* name)
{
    return llvm::StructType::create(*ctx->context, name);
}

EXTERN_API(llvm::Type)* getSizeTType(ManagedContext* ctx)
{
    switch (sizeof(void*)) {
        case 1:
            return getByteType(ctx);
        case 2:
            return getShortType(ctx);
        case 4:
            return getIntType(ctx);
        case 8:
            return getLongType(ctx);
        case 16:
            return getBiglongType(ctx);
        default:
            return getIntType(ctx);
    }
}

EXTERN_API(llvm::Type)* getTypeFromValue(ManagedContext* ctx, llvm::Value* val)
{
    if (val == nullptr || !llvm::isa<llvm::Value>(val))
        return nullptr;
    return val->getType();
}

llvm::Function* declareFunction(ManagedContext* ctx, const char* name, llvm::Type* retTy, llvm::Type* const* argTys, uint32_t argc, const char** argNames, uint32_t argNamec, bool isPublic)
{
    
    auto fnTy = llvm::FunctionType::get(retTy, llvm::ArrayRef<llvm::Type*>(argTys, argTys + argc), false);
    auto ret = llvm::Function::Create(fnTy, isPublic ? llvm::GlobalValue::LinkageTypes::ExternalLinkage : llvm::GlobalValue::LinkageTypes::InternalLinkage, name, ctx->M);
    uint32_t skip;
    if (argc > argNamec)
        skip = argc - argNamec;
    else
        skip = 0;
    uint32_t ind = 0;
    for (auto& arg : ret->args()) {
        if (!skip)
            arg.setName(argNames[ind++]);
        else {
            // use for instance-methods, where the first formalparameter is the thisptr
            arg.setName("this");
            --skip;
        }
    }
    return ret;
}

EXTERN_API(llvm::Function)* declareFunctionOfType(ManagedContext* ctx, const char* name, llvm::Type* fnPtrTy, bool isPublic)
{
    assert(llvm::isa<llvm::PointerType>(fnPtrTy));
    auto fnTy = fnPtrTy->getPointerElementType();
    assert(llvm::isa<llvm::FunctionType>(fnTy));

    auto ret = llvm::Function::Create(llvm::cast<llvm::FunctionType>(fnTy), isPublic ? llvm::GlobalValue::LinkageTypes::ExternalLinkage : llvm::GlobalValue::LinkageTypes::InternalLinkage, name, ctx->M);
    return ret;
}
llvm::Attribute getAttribute(ManagedContext* ctx, const char* name) {
    using namespace llvm;
    auto kind = StringSwitch<llvm::Attribute::AttrKind>(name)
        .Case("align", Attribute::Alignment)
        .Case("allocsize", Attribute::AllocSize)
        .Case("alwaysinline", Attribute::AlwaysInline)
        .Case("argmemonly", Attribute::ArgMemOnly)
        .Case("builtin", Attribute::Builtin)
        .Case("byval", Attribute::ByVal)
        .Case("cold", Attribute::Cold)
        .Case("convergent", Attribute::Convergent)
        .Case("dereferenceable", Attribute::Dereferenceable)
        .Case("dereferenceable_or_null", Attribute::DereferenceableOrNull)
        .Case("inalloca", Attribute::InAlloca)
        .Case("inreg", Attribute::InReg)
        .Case("inaccessiblememonly", Attribute::InaccessibleMemOnly)
        .Case("inaccessiblemem_or_argmemonly", Attribute::InaccessibleMemOrArgMemOnly)
        .Case("inlinehint", Attribute::InlineHint)
        .Case("jumptable", Attribute::JumpTable)
        .Case("minsize", Attribute::MinSize)
        .Case("naked", Attribute::Naked)
        .Case("nest", Attribute::Nest)
        .Case("noalias", Attribute::NoAlias)
        .Case("nobuiltin", Attribute::NoBuiltin)
        .Case("nocapture", Attribute::NoCapture)
        .Case("nocf_check", Attribute::NoCfCheck)
        .Case("noduplicate", Attribute::NoDuplicate)
        .Case("noimplicitfloat", Attribute::NoImplicitFloat)
        .Case("noinline", Attribute::NoInline)
        .Case("norecurse", Attribute::NoRecurse)
        .Case("noredzone", Attribute::NoRedZone)
        .Case("noreturn", Attribute::NoReturn)
        .Case("nounwind", Attribute::NoUnwind)
        .Case("nonlazybind", Attribute::NonLazyBind)
        .Case("nonnull", Attribute::NonNull)
        .Case("optforfuzzing", Attribute::OptForFuzzing)
        .Case("optsize", Attribute::OptimizeForSize)
        .Case("optnone", Attribute::OptimizeNone)
        .Case("readnone", Attribute::ReadNone)
        .Case("readonly", Attribute::ReadOnly)
        .Case("returned", Attribute::Returned)
        .Case("returns_twice", Attribute::ReturnsTwice)
        .Case("signext", Attribute::SExt)
        .Case("safestack", Attribute::SafeStack)
        .Case("sanitize_address", Attribute::SanitizeAddress)
        .Case("sanitize_hwaddress", Attribute::SanitizeHWAddress)
        .Case("sanitize_memory", Attribute::SanitizeMemory)
        .Case("sanitize_thread", Attribute::SanitizeThread)
        .Case("shadowcallstack", Attribute::ShadowCallStack)
        .Case("speculatable", Attribute::Speculatable)
        .Case("alignstack", Attribute::StackAlignment)
        .Case("ssp", Attribute::StackProtect)
        .Case("sspreq", Attribute::StackProtectReq)
        .Case("sspstrong", Attribute::StackProtectStrong)
        .Case("strictfp", Attribute::StrictFP)
        .Case("sret", Attribute::StructRet)
        .Case("swifterror", Attribute::SwiftError)
        .Case("swiftself", Attribute::SwiftSelf)
        .Case("uwtable", Attribute::UWTable)
        .Case("writeonly", Attribute::WriteOnly)
        .Case("zeroext", Attribute::ZExt)
        .Default(Attribute::None);
    if (kind == Attribute::None) {
        return Attribute::get(*ctx->context, name);
    }
    else {
        return Attribute::get(*ctx->context, kind);
    }
}
EXTERN_API(void) addFunctionAttributes(ManagedContext* ctx, llvm::Function* fn, const char** atts, uint32_t attc)
{
    llvm::AttrBuilder abuilder;
    for (uint32_t i = 0; i < attc; ++i) {
        abuilder.addAttribute(getAttribute(ctx, atts[i]));
    }
    fn->addAttributes(llvm::AttributeList::FunctionIndex, abuilder);
}

EXTERN_API(void) addParamAttributes(ManagedContext* ctx, llvm::Function* fn, const char** atts, uint32_t attc, uint32_t paramIdx)
{
    llvm::AttrBuilder abuilder;
    for (uint32_t i = 0; i < attc; ++i) {
        abuilder.addAttribute(getAttribute(ctx, atts[i]));
    }
    fn->addParamAttrs(paramIdx, abuilder);
}


EXTERN_API(void) addReturnNoAliasAttribute(ManagedContext* ctx, llvm::Function* fn)
{
    fn->setReturnDoesNotAlias();
}

EXTERN_API(void) addReturnNotNullAttribute(ManagedContext* ctx, llvm::Function* fn)
{
    fn->addAttribute(llvm::AttributeList::ReturnIndex, llvm::Attribute::get(*ctx->context, llvm::Attribute::NonNull));
}


EXTERN_API(llvm::Function)* declareMallocFunction(ManagedContext* ctx, const char* name, llvm::Type* const* argTys, uint32_t argc, bool resultNotNull)
{
    auto fnTy = llvm::FunctionType::get(llvm::Type::getInt8PtrTy(*ctx->context), llvm::ArrayRef<llvm::Type*>(argTys, argTys + argc), false);

    auto ret = llvm::cast<llvm::Function>(ctx->M->getOrInsertFunction(name, fnTy));
    llvm::AttrBuilder abuilder;
    //abuilder.addAttribute(llvm::Attribute::get(*ctx->context, llvm::Attribute::AttrKind::ReadOnly));
    if (!resultNotNull)
        abuilder.addAttribute(llvm::Attribute::get(*ctx->context, llvm::Attribute::AttrKind::NoUnwind));
    else
        ret->addAttribute(llvm::AttributeList::ReturnIndex, llvm::Attribute::get(*ctx->context, llvm::Attribute::AttrKind::NonNull));
    abuilder.addAttribute(llvm::Attribute::get(*ctx->context, llvm::Attribute::AttrKind::NoRecurse));
    ret->addAttributes(llvm::AttributeList::FunctionIndex, abuilder);
    ret->setReturnDoesNotAlias();
    return ret;
}

EXTERN_API(llvm::Type)* getFunctionPtrTypeFromFunction(ManagedContext* ctx, llvm::Function* fn)
{
    return fn == nullptr ? nullptr : fn->getType();
}

EXTERN_API(llvm::Type)* getFunctionPtrType(ManagedContext* ctx, llvm::Type* retTy, llvm::Type** argTys, uint32_t argc)
{
    auto fnTy = llvm::FunctionType::get(retTy, llvm::ArrayRef<llvm::Type*>(argTys, argc), false);
    return fnTy->getPointerTo();
}

llvm::BasicBlock* createBasicBlock(ManagedContext* ctx, const char* name, llvm::Function* fn)
{
    return llvm::BasicBlock::Create(*ctx->context, name, fn);
}

EXTERN_API(void) resetInsertPoint(ManagedContext* ctx, llvm::BasicBlock* bb, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    irb->SetInsertPoint(bb);
}

EXTERN_API(llvm::Value)* defineAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, const char* name)
{
    llvm::IRBuilder<> inserter(&fn->getEntryBlock(), fn->getEntryBlock().begin());

    return inserter.CreateAlloca(ty, nullptr, name);
}

EXTERN_API(llvm::Value)* defineZeroinitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, const char* name, llvm::IRBuilder<>* irb, bool addlifetime)
{
    return defineInitializedAlloca(ctx, fn, ty, llvm::Constant::getNullValue(ty), name, irb, addlifetime);
}

EXTERN_API(llvm::Value)* defineInitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Type* ty, llvm::Value* initVal, const char* name, llvm::IRBuilder<>* irb, bool addlifetime)
{
    if (irb == nullptr)
        irb = ctx->builder;
    llvm::Value* ret;
    {
        llvm::IRBuilder<> inserter(&fn->getEntryBlock(), fn->getEntryBlock().begin());
        ret = inserter.CreateAlloca(ty, nullptr, name);
    }
    if (initVal->getType() != ty) {
        //assume, that this only happens for pointers such that unsigned is always false
        initVal = forceCast(ctx, initVal, ty, false, irb);
    }
    if (addlifetime)
        irb->CreateLifetimeStart(ret);
    irb->CreateStore(initVal, ret);
    return ret;
}

EXTERN_API(llvm::Value)* defineTypeInferredInitializedAlloca(ManagedContext* ctx, llvm::Function* fn, llvm::Value* initVal, const char* name, llvm::IRBuilder<>* irb, bool addlifetime)
{
    return defineInitializedAlloca(ctx, fn, initVal->getType(), initVal, name, irb, addlifetime);
}

EXTERN_API(void) endLifeTime(ManagedContext* ctx, llvm::Value* ptr, llvm::IRBuilder<>* irb)
{
    assert(ptr && llvm::isa<llvm::PointerType>(ptr->getType()));
    if (irb == nullptr)
        irb = ctx->builder;
    irb->CreateLifetimeEnd(ptr);
}

EXTERN_API(llvm::IRBuilder<>)* CreateIRBuilder(ManagedContext* ctx)
{
    return new llvm::IRBuilder<>(*ctx->context);
}

EXTERN_API(void) DisposeIRBuilder(llvm::IRBuilder<>* irb)
{
    delete(irb);
}


EXTERN_API(llvm::Value)* loadField(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    /*llvm::errs() << "GEP: " << *basePtr << " ";
    for (uint32_t i = 0; i < gepIdxCount; ++i) {
        llvm::errs() << *gepIdx[i] << ", ";
    }
    llvm::errs() << "\n";
    llvm::errs() << "GEP-Type: " << *basePtr->getType() << " ";
    for (uint32_t i = 0; i < gepIdxCount; ++i) {
        llvm::errs() << *gepIdx[i]->getType() << ", ";
    }
    llvm::errs() << ":\n:::END\n";*/
    auto fldPtr = irb->CreateGEP(basePtr, llvm::ArrayRef<llvm::Value*>(gepIdx, gepIdxCount));
    return irb->CreateLoad(fldPtr);
}

EXTERN_API(llvm::Value)* loadFieldConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t geIdxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto fldPtr = GetElementPtrConstIdx(ctx, basePtr, gepIdx, geIdxCount, irb);
    return irb->CreateLoad(fldPtr);
}

EXTERN_API(void) storeField(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto fldPtr = irb->CreateGEP(basePtr, llvm::ArrayRef<llvm::Value*>(gepIdx, gepIdxCount));
    store(ctx, fldPtr, nwVal, irb);
}

EXTERN_API(void) storeFieldConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto fldPtr = GetElementPtrConstIdx(ctx, basePtr, gepIdx, gepIdxCount, irb);
    //irb->CreateStore(nwVal, fldPtr);
    store(ctx, fldPtr, nwVal, irb);
}

EXTERN_API(llvm::Value)* GetElementPtr(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value** gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;

    //assert(llvm::isa<llvm::PointerType>(basePtr->getType()));
    //llvm::errs() << "GEP Value from (type: " << *basePtr->getType()->getPointerElementType() << " = *" << *basePtr->getType() << ") " << *basePtr << " at ";
    //for (auto idx : llvm::ArrayRef<llvm::Value*>(gepIdx, gepIdxCount)) {
    //	llvm::errs() << *idx << " ";
    //}
    //llvm::errs() << ":::end\r\n";

    return irb->CreateGEP(basePtr, llvm::ArrayRef<llvm::Value*>(gepIdx, gepIdxCount));
}

EXTERN_API(llvm::Value)* GetElementPtrConstIdx(ManagedContext* ctx, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    //assert(llvm::isa<llvm::PointerType>(basePtr->getType()));
    //llvm::errs() << "GEP Value from (type: " << *basePtr->getType()->getPointerElementType() << " = *" << *basePtr->getType() << ") " << *basePtr << " at ";

    auto pointeeTy = llvm::cast<llvm::PointerType>(basePtr->getType())->getElementType();
    llvm::SmallVector<llvm::Value*, 5> idx;
    auto intTy = llvm::Type::getInt32Ty(*ctx->context);
    //llvm::errs() << "GEP " << *pointeeTy << " => " << *basePtr << ":::";
    for (uint32_t i = 0; i < gepIdxCount; ++i) {
        idx.push_back(llvm::Constant::getIntegerValue(intTy, llvm::APInt(32, gepIdx[i])));
        //llvm::errs() << *idx.back() << " ";
    }
    //llvm::errs() << ":::end\r\n";
    //llvm::errs() << ":::";
    auto ret = irb->CreateGEP(pointeeTy, basePtr, idx);
    //llvm::errs() << "end\r\n";
    return ret;
}

EXTERN_API(llvm::Value)* GetElementPtrConstIdxWithType(ManagedContext* ctx, llvm::Type* pointeeTy, llvm::Value* basePtr, uint32_t* gepIdx, uint32_t gepIdxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    llvm::SmallVector<llvm::Value*, 5> idx;
    auto intTy = llvm::Type::getInt32Ty(*ctx->context);
    //llvm::errs() << "GEP " << *pointeeTy << " => " << *basePtr << ":::";

    for (uint32_t i = 0; i < gepIdxCount; ++i) {
        idx.push_back(llvm::Constant::getIntegerValue(intTy, llvm::APInt(32, gepIdx[i])));
        //llvm::errs() << *idx.back() << " ";
    }
    //llvm::errs() << ":::";
    //llvm::errs() << "end\r\n";
    auto ret = irb->CreateGEP(pointeeTy, basePtr, idx);

    return ret;
}

EXTERN_API(llvm::Value)* load(ManagedContext* ctx, llvm::Value* basePtr, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreateLoad(basePtr);
}

EXTERN_API(llvm::Value)* loadAcquire(ManagedContext* ctx, llvm::Value* basePtr, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto ret = irb->CreateLoad(basePtr);
    ret->setAtomic(llvm::AtomicOrdering::Acquire);
    return ret;
}

EXTERN_API(void) store(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    assert(llvm::isa<llvm::Value>(nwVal));
    //llvm::errs() << "store " << *basePtr << " <= " << *nwVal << ":::end\r\n";
    tryCast(ctx, nwVal, basePtr->getType()->getPointerElementType(), nwVal, false, irb);
    irb->CreateStore(nwVal, basePtr);
}
EXTERN_API(void) storeRelease(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    assert(llvm::isa<llvm::Value>(nwVal));
    //llvm::errs() << "storeRelease " << *basePtr << " <= " << *nwVal << ":::end\r\n";
    tryCast(ctx, nwVal, basePtr->getType()->getPointerElementType(), nwVal, false, irb);
    auto st = irb->CreateStore(nwVal, basePtr);
    st->setAtomic(llvm::AtomicOrdering::Release);
}
EXTERN_API(llvm::Value)* compareExchangeAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* cmp, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto vCmp = irb->CreateLoad(cmp);
    auto cmpxchng = irb->CreateAtomicCmpXchg(basePtr, vCmp, nwVal, llvm::AtomicOrdering::AcquireRelease, llvm::AtomicOrdering::Monotonic);

    auto oldVal = irb->CreateExtractValue(cmpxchng, { 0u });
    auto ret = irb->CreateExtractValue(cmpxchng, { 1u });
    irb->CreateStore(oldVal, cmp);
    return ret;
}
EXTERN_API(llvm::Value)* exchangeAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* nwVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreateAtomicRMW(llvm::AtomicRMWInst::BinOp::Xchg, basePtr, nwVal, llvm::AtomicOrdering::AcquireRelease);
}
EXTERN_API(llvm::Value)* getArgument(ManagedContext* ctx, llvm::Function* fn, uint32_t index)
{
    if (fn->arg_size() > index)
        return &fn->args().begin()[index];
    return nullptr;
}

EXTERN_API(llvm::Value)* negate(ManagedContext* ctx, llvm::Value* subEx, char op, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    switch (op) {
        case '-': {
            if (subEx->getType()->isFloatingPointTy()) {
                return irb->CreateFNeg(subEx);
            }
            else {
                return irb->CreateNeg(subEx);
            }
        }
        case '~':
            return irb->CreateNot(subEx);
        case '!':
            return irb->CreateIsNull(subEx);
    }
    return nullptr;
}

EXTERN_API(llvm::Value)* arithmeticBinOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    //llvm::errs() << "Process " << *lhs << " " << op << " " << *rhs << "\r\n";
    bool isFloatingPoint = bringToCommonSupertype(ctx, lhs, rhs, irb, isUnsigned);
    //llvm::errs() << ">> " << (isFloatingPoint ? "as Float" : "as Int") << "\r\n";
    switch (op) {
        case '+':
            return !isFloatingPoint ? irb->CreateAdd(lhs, rhs) : irb->CreateFAdd(lhs, rhs);
        case'-':
            return !isFloatingPoint ? irb->CreateSub(lhs, rhs) : irb->CreateFSub(lhs, rhs);
        case'*':
            return !isFloatingPoint ? irb->CreateMul(lhs, rhs) : irb->CreateFMul(lhs, rhs);
        case'/':
            if (isUnsigned) {
                return irb->CreateUDiv(lhs, rhs);
            }
            else {
                return !isFloatingPoint ? irb->CreateSDiv(lhs, rhs) : irb->CreateFDiv(lhs, rhs);
            }
        case '%':
            if (isUnsigned) {
                return irb->CreateURem(lhs, rhs);
            }
            else {
                return !isFloatingPoint ? irb->CreateSRem(lhs, rhs) : irb->CreateFRem(lhs, rhs);
            }
            break;
    }

    return nullptr;
}

EXTERN_API(llvm::Value)* arithmeticUpdateAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    //llvm::errs() << "Process " << *lhs << " atomic " << op << " " << *rhs << "\r\n";
    assert(llvm::isa<llvm::PointerType>(basePtr->getType()));
    auto type = ((llvm::PointerType*)basePtr->getType())->getPointerElementType();
    tryCast(ctx, rhs, type, rhs, isUnsigned, irb);
    bool isFloatingPoint = type->isFloatingPointTy();

    llvm::AtomicRMWInst::BinOp inst;
    switch (op) {
        case '+':
            inst = llvm::AtomicRMWInst::BinOp::Add; break;
        case '-':
            inst = llvm::AtomicRMWInst::BinOp::Sub; break;
        default:return nullptr;
    }
    return irb->CreateAtomicRMW(inst, basePtr, rhs, llvm::AtomicOrdering::AcquireRelease);

}

EXTERN_API(llvm::Value)* bitwiseBinop(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    bool isFloatingPoint = bringToCommonSupertype(ctx, lhs, rhs, irb, isUnsigned);
    switch (op) {
        case '&':
            return irb->CreateAnd(lhs, rhs);
        case '|':
            return irb->CreateOr(lhs, rhs);
        case'^':
            return irb->CreateXor(lhs, rhs);
    }
    return nullptr;
}

EXTERN_API(llvm::Value)* bitwiseUpdateAcqRel(ManagedContext* ctx, llvm::Value* basePtr, llvm::Value* rhs, char op, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    assert(llvm::isa<llvm::PointerType>(basePtr->getType()));
    auto type = ((llvm::PointerType*)basePtr->getType())->getPointerElementType();
    tryCast(ctx, rhs, type, rhs, isUnsigned, irb);
    bool isFloatingPoint = type->isFloatingPointTy();
    if (isFloatingPoint)
        return nullptr;
    llvm::AtomicRMWInst::BinOp inst;
    switch (op) {
        case '&':
            inst = llvm::AtomicRMWInst::BinOp::And; break;
        case'|':
            inst = llvm::AtomicRMWInst::BinOp::Or; break;
        case'^':
            inst = llvm::AtomicRMWInst::BinOp::Xor; break;
        default:
            return nullptr;
    }
    return irb->CreateAtomicRMW(inst, basePtr, rhs, llvm::AtomicOrdering::AcquireRelease);
}

EXTERN_API(llvm::Value)* shiftOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, bool leftShift, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    bool isFloatingPoint = bringToCommonSupertype(ctx, lhs, rhs, irb, isUnsigned);
    if (leftShift) {
        return irb->CreateShl(lhs, rhs);
    }
    else {
        return isUnsigned ? irb->CreateLShr(lhs, rhs) : irb->CreateAShr(lhs, rhs);
    }
}

EXTERN_API(llvm::Value)* compareOp(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, char op, bool orEqual, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    //llvm::errs() << "compare " << *lhs << " " << op << " " << *rhs << "\r\n";
    bool isFloatingPoint = bringToCommonSupertype(ctx, lhs, rhs, irb, isUnsigned);
    //llvm::errs() << ">>" << *lhs << " " << op << " " << *rhs << "\r\n";
    switch (op) {
        case'!':
            if (isFloatingPoint) {
                if (orEqual)
                    return irb->CreateFCmpOEQ(lhs, rhs);
                else
                    return irb->CreateFCmpONE(lhs, rhs);
            }
            else if (lhs->getType()->isPointerTy()) {
                auto diff = irb->CreatePtrDiff(lhs, rhs);
                if (orEqual)
                    return irb->CreateICmpEQ(diff, llvm::ConstantInt::getNullValue(llvm::Type::getInt64Ty(*ctx->context)));
                else
                    return irb->CreateICmpNE(diff, llvm::ConstantInt::getNullValue(llvm::Type::getInt64Ty(*ctx->context)));

            }
            else {

                if (orEqual)
                    return irb->CreateICmpEQ(lhs, rhs);
                else
                    return irb->CreateICmpNE(lhs, rhs);
            }
        case'<':
            if (isFloatingPoint) {
                if (orEqual)
                    return irb->CreateFCmpOLE(lhs, rhs);
                else
                    return irb->CreateFCmpOLT(lhs, rhs);
            }
            else if (isUnsigned) {
                if (orEqual)
                    return irb->CreateICmpULE(lhs, rhs);
                else
                    return irb->CreateICmpULT(lhs, rhs);
            }
            else {
                if (orEqual)
                    return irb->CreateICmpSLE(lhs, rhs);
                else
                    return irb->CreateICmpSLT(lhs, rhs);
            }
        case'>':
            if (isFloatingPoint) {
                if (orEqual)
                    return irb->CreateFCmpOGE(lhs, rhs);
                else
                    return irb->CreateFCmpOGT(lhs, rhs);
            }
            else if (isUnsigned) {
                if (orEqual)
                    return irb->CreateICmpUGE(lhs, rhs);
                else
                    return irb->CreateICmpUGT(lhs, rhs);
            }
            else {
                if (orEqual)
                    return irb->CreateICmpSGE(lhs, rhs);
                else
                    return irb->CreateICmpSGT(lhs, rhs);
            }
        default:
            return nullptr;
    }
}

EXTERN_API(llvm::Value)* ptrDiff(ManagedContext* ctx, llvm::Value* lhs, llvm::Value* rhs, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto voidPtrTy = getVoidPtr(ctx);
    lhs = forceCast(ctx, lhs, voidPtrTy, false, irb);
    rhs = forceCast(ctx, rhs, voidPtrTy, false, irb);
    return irb->CreatePtrDiff(lhs, rhs);
}

EXTERN_API(bool) tryCast(ManagedContext* ctx, llvm::Value* val, llvm::Type* dest, llvm::Value*& _ret, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    auto ret = &_ret;
    if (irb == nullptr)
        irb = ctx->builder;
    //llvm::errs() << "Try Cast " << *val << " to " << *dest << ":::\r\n";
    if (val->getType()->isPointerTy()) {
        if (dest->isPointerTy()) {
            //llvm::errs() << "PointerCast(irb = " << irb << "): ";

            //*ret = irb->CreatePointerBitCastOrAddrSpaceCast(val, dest);
            auto cst = //llvm::CastInst::CreatePointerBitCastOrAddrSpaceCast(val, dest, "", irb->GetInsertBlock());
                llvm::CastInst::CreatePointerBitCastOrAddrSpaceCast(val, dest, "", irb->GetInsertBlock());
            //llvm::errs() << *cst << ":::end\n";
            *ret = cst;
            return true;
        }
        else if (dest->isIntegerTy()) {
            //llvm::errs() << "Ptrtoint";
            *ret = irb->CreatePtrToInt(val, dest);

            return true;
        }
        else
            return false;
    }
    else if (val->getType()->isIntegerTy()) {
        if (dest->isPointerTy()) {
            *ret = irb->CreateIntToPtr(val, dest);
            return true;
        }
        else if (dest->isFloatingPointTy()) {
            if (isUnsigned)
                * ret = irb->CreateUIToFP(val, dest);
            else
                *ret = irb->CreateSIToFP(val, dest);
            return true;
        }
        else if (dest->isIntegerTy()) {
            if (val->getType()->getIntegerBitWidth() > dest->getIntegerBitWidth()) {
                *ret = irb->CreateTruncOrBitCast(val, dest);
            }
            else if (isUnsigned) {
                *ret = irb->CreateZExtOrBitCast(val, dest);
            }
            else {
                *ret = irb->CreateSExtOrBitCast(val, dest);
            }
            return true;
        }
        else if (val->getType()->canLosslesslyBitCastTo(dest)) {
            *ret = irb->CreateBitCast(val, dest);
            return true;
        }
        else
            return false;
    }
    else if (val->getType()->isFloatingPointTy()) {
        if (dest->isFloatingPointTy()) {
            *ret = irb->CreateFPCast(val, dest);
            return true;
        }
        else if (dest->isIntegerTy()) {
            if (isUnsigned)
                * ret = irb->CreateFPToSI(val, dest);
            else
                *ret = irb->CreateFPToUI(val, dest);
            return true;
        }
        else if (val->getType()->canLosslesslyBitCastTo(dest)) {
            *ret = irb->CreateBitCast(val, dest);
            return true;
        }
        else
            return false;
    }
    else if (val->getType()->canLosslesslyBitCastTo(dest)) {
        //llvm::errs() << "lossyless bitcast";
        *ret = irb->CreateBitCast(val, dest);
        return true;
    }
    return false;
}

EXTERN_API(llvm::Value)* forceCast(ManagedContext* ctx, llvm::Value* val, llvm::Type* dest, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    llvm::Value* ret;
    //llvm::errs() << "Force cast " << *val << " to " << *dest << "\r\n";
    if (!tryCast(ctx, val, dest, ret, isUnsigned, irb)) {
        if (irb == nullptr)
            irb = ctx->builder;
        //llvm::errs() << ">> TryCast failed => use bitcast\r\n";
        ret = irb->CreateBitOrPointerCast(val, dest);
    }
    /*else {
        llvm::errs() << ">> TryCast succeeded\r\n";
    }*/
    return ret;
}

EXTERN_API(void) returnVoid(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    irb->CreateRetVoid();
}

EXTERN_API(void) returnValue(ManagedContext* ctx, llvm::Value* retVal, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto retTy = irb->getCurrentFunctionReturnType();
    if (retTy && retVal->getType()->canLosslesslyBitCastTo(retTy))
        retVal = irb->CreateBitOrPointerCast(retVal, retTy);
    irb->CreateRet(retVal);
}

EXTERN_API(void) integerSwitch(ManagedContext* ctx, llvm::Value* compareInt, llvm::BasicBlock* defaultTarget, llvm::BasicBlock** conditionalTargets, uint32_t numCondTargets, llvm::Value** conditions, uint32_t numConditions, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto numCases = std::min(numConditions, numCondTargets);
    auto sw = irb->CreateSwitch(compareInt, defaultTarget, numCases);
    for (uint32_t i = 0; i < numCases; ++i) {
        if (llvm::isa<llvm::ConstantInt>(conditions[i])) {
            sw->addCase((llvm::ConstantInt*)conditions[i], conditionalTargets[i]);
        }
    }
}

EXTERN_API(llvm::Constant)* getInt32(ManagedContext* ctx, uint32_t val)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt32Ty(*ctx->context), llvm::APInt(32, val));
}

EXTERN_API(llvm::Constant)* getInt16(ManagedContext* ctx, uint16_t val)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt16Ty(*ctx->context), llvm::APInt(16, val));
}

EXTERN_API(llvm::Constant)* getInt8(ManagedContext* ctx, uint8_t val)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt8Ty(*ctx->context), llvm::APInt(8, val));
}

EXTERN_API(llvm::Constant)* getString(ManagedContext* ctx, const char* strlit, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto strVal = irb->CreateGlobalStringPtr(strlit);
    auto strLen = llvm::Constant::getIntegerValue(getIntType(ctx), llvm::APInt(sizeof(size_t) << 3, ::strnlen_s(strlit, 1 << 16)));
    return llvm::ConstantStruct::get((llvm::StructType*)getStringType(ctx), { strVal, strLen });
}



EXTERN_API(llvm::Constant)* True(ManagedContext* ctx)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt1Ty(*ctx->context), llvm::APInt(1, 1));
}

EXTERN_API(llvm::Constant)* False(ManagedContext* ctx)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt1Ty(*ctx->context), llvm::APInt(1, 0));
}

EXTERN_API(llvm::Constant)* getFloat(ManagedContext* ctx, float val)
{
    return llvm::ConstantFP::get(*ctx->context, llvm::APFloat(val));
}

EXTERN_API(llvm::Constant)* getDouble(ManagedContext* ctx, double val)
{
    return llvm::ConstantFP::get(*ctx->context, llvm::APFloat(val));
}

EXTERN_API(llvm::Constant)* getNullPtr(ManagedContext* ctx)
{
    return llvm::Constant::getNullValue(llvm::Type::getInt8PtrTy(*ctx->context));
}

EXTERN_API(llvm::Constant)* getAllZeroValue(ManagedContext* ctx, llvm::Type* ty)
{
    return Constant::getNullValue(ty);
}

EXTERN_API(llvm::Constant)* getAllOnesValue(ManagedContext* ctx, llvm::Type* ty)
{
    return Constant::getAllOnesValue(ty);
}

EXTERN_API(llvm::Constant)* getConstantStruct(ManagedContext* ctx, llvm::Type* ty, llvm::Constant** values, uint32_t valuec)
{
    assert(ctx && ty && values);
    assert(llvm::isa<llvm::StructType>(ty));
    for (uint32_t i = 0; i < valuec; ++i) {
        assert(llvm::isa<llvm::Constant>(values[i]));
        if (ty->getStructElementType(i) != values[i]->getType()) {
            values[i] = llvm::ConstantExpr::getBitCast(values[i], ty->getStructElementType(i));
        }
    }

    return llvm::ConstantStruct::get(llvm::cast<llvm::StructType>(ty), llvm::ArrayRef<llvm::Constant*>(values, valuec));
}

EXTERN_API(llvm::Value)* getStructValue(ManagedContext* ctx, llvm::Type* ty, llvm::Value** values, uint32_t valuec, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    assert(ctx && ty && values);
    assert(llvm::isa<llvm::StructType>(ty));

    llvm::Value* ret = llvm::UndefValue::get(ty);

    for (uint32_t i = 0; i < valuec; ++i) {
        assert(llvm::isa<llvm::Value>(values[i]));
        if (ty->getStructElementType(i) != values[i]->getType()) {
            values[i] = forceCast(ctx, values[i], ty->getStructElementType(i), false, irb);
        }
        ret = irb->CreateInsertValue(ret, values[i], { i });
    }

    return ret;

}

EXTERN_API(llvm::Value)* getCall(ManagedContext* ctx, llvm::Function* fn, llvm::Value** args, uint32_t argc, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    if (!llvm::isa<llvm::Function>(fn)) {
        llvm::errs() << "The callee is not a function!\r\n";
    }
    //else {
        //llvm::errs() << "The callee is a function\r\n";
    //}
    auto elemTy = fn->getType()->getElementType();
    //llvm::errs() << "The function is " << *fn << "\r\n";
    //llvm::errs() << "The function-type is *" << elemTy << "=" << *elemTy << "\r\n";
    auto fnTy = llvm::cast<llvm::FunctionType>(elemTy);
    //llvm::errs() << "Casted function-type\r\n";
    uint32_t i = 0;
    for (auto argTy : fnTy->params()) {
        if (args[i]->getType() != argTy) {
            //llvm::errs() << "cast argument " << i << " = " << *args[i] << " to *" << argTy << "=" << *argTy << "\r\n";
            args[i] = forceCast(ctx, args[i], argTy, false, irb);
            //llvm::errs() << "cast done\r\n";
        }
        i++;
    }
    //llvm::errs() << "createCall\r\n";
    auto ret = llvm::CallInst::Create(fnTy, fn, llvm::ArrayRef<llvm::Value*>(args, argc));
    //llvm::errs() << ":::end\r\n";
    irb->Insert(ret);
    return// irb->CreateCall(fn, llvm::ArrayRef<llvm::Value*>(args, argc));
        ret;
}

EXTERN_API(void) branch(ManagedContext* ctx, llvm::BasicBlock* dest, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    irb->CreateBr(dest);
}
llvm::Value* NotNull(llvm::Value* val, llvm::IRBuilder<>* irb) {
    if (val->getType()->isIntegerTy() && val->getType()->getIntegerBitWidth() == 1)
        return val;
    else
        return irb->CreateIsNotNull(val);
}
EXTERN_API(void) conditionalBranch(ManagedContext* ctx, llvm::BasicBlock* destTrue, llvm::BasicBlock* destFalse, llvm::Value* cond, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    irb->CreateCondBr(NotNull(cond, irb), destTrue, destFalse);
}

EXTERN_API(void) unreachable(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    irb->CreateUnreachable();
}

EXTERN_API(llvm::Value)* conditionalSelect(ManagedContext* ctx, llvm::Value* valTrue, llvm::Value* valFalse, llvm::Value* cond, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreateSelect(NotNull(cond, irb), valTrue, valFalse);
}

EXTERN_API(llvm::Value)* extractValue(ManagedContext* ctx, llvm::Value* aggregate, uint32_t index, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;

    return irb->CreateExtractValue(aggregate, { index });
}

EXTERN_API(llvm::Value)* extractNestedValue(ManagedContext* ctx, llvm::Value* aggregate, uint32_t* indices, uint32_t idxCount, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreateExtractValue(aggregate, llvm::ArrayRef<uint32_t>(indices, idxCount));
}

EXTERN_API(llvm::PHINode)* createPHINode(ManagedContext* ctx, llvm::Type* ty, uint32_t numCases, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreatePHI(ty, numCases);
}

EXTERN_API(void) addMergePoint(llvm::PHINode* phi, llvm::Value* val, llvm::BasicBlock* prev)
{
    phi->addIncoming(val, prev);
}

EXTERN_API(llvm::Value)* isNotNull(ManagedContext* ctx, llvm::Value* val, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return NotNull(val, irb);
}

EXTERN_API(llvm::Value)* getSizeOf(ManagedContext* ctx, llvm::Type* ty)
{
    return llvm::ConstantExpr::getSizeOf(ty);
}

EXTERN_API(llvm::Value)* getI32SizeOf(ManagedContext* ctx, llvm::Type* ty)
{
    return llvm::ConstantExpr::getTruncOrBitCast(llvm::ConstantExpr::getSizeOf(ty), llvm::Type::getInt32Ty(*ctx->context));
}

EXTERN_API(llvm::Value)* getI64SizeOf(ManagedContext* ctx, llvm::Type* ty)
{
    return llvm::ConstantExpr::getZExtOrBitCast(llvm::ConstantExpr::getSizeOf(ty), llvm::Type::getInt64Ty(*ctx->context));
}

EXTERN_API(llvm::Value)* getMin(ManagedContext* ctx, llvm::Value* v1, llvm::Value* v2, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    bringToCommonSupertype(ctx, v1, v2, irb, isUnsigned);
    if (v1->getType()->isFloatingPointTy())
        return irb->CreateMinNum(v1, v2);
    else if (v1->getType()->isIntegerTy()) {
        llvm::Value* cmp;
        if (isUnsigned) {
            cmp = irb->CreateICmpULT(v1, v2);
        }
        else {
            cmp = irb->CreateICmpSLT(v1, v2);
        }
        return irb->CreateSelect(cmp, v1, v2);
    }
    return nullptr;
}

EXTERN_API(llvm::Value)* getMax(ManagedContext* ctx, llvm::Value* v1, llvm::Value* v2, bool isUnsigned, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    bringToCommonSupertype(ctx, v1, v2, irb, isUnsigned);
    if (v1->getType()->isFloatingPointTy())
        return irb->CreateMaxNum(v1, v2);
    else if (v1->getType()->isIntegerTy()) {
        llvm::Value* cmp;
        if (isUnsigned) {
            cmp = irb->CreateICmpULT(v1, v2);
        }
        else {
            cmp = irb->CreateICmpSLT(v1, v2);
        }
        return irb->CreateSelect(cmp, v2, v1);
    }
    return nullptr;
}

EXTERN_API(llvm::Value)* memoryCopy(ManagedContext* ctx, llvm::Value* dest, llvm::Value* src, llvm::Value* byteCount, bool isVolatile, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->CreateMemCpy(dest, 0, src, 0, byteCount, isVolatile);
}

EXTERN_API(llvm::Value)* memoryMove(ManagedContext* ctx, llvm::Value* dest, llvm::Value* src, llvm::Value* byteCount, bool isVolatile, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;

    return irb->CreateMemMove(dest, 0, src, 0, byteCount, isVolatile);
}

EXTERN_API(bool) currentBlockIsTerminated(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    assert(irb->GetInsertBlock() != nullptr);
    //llvm::errs() << irb->GetInsertBlock() << "\r\n";

    return irb->GetInsertBlock()->getTerminator() != nullptr;
}

EXTERN_API(bool) isTerminated(llvm::BasicBlock* bb)
{
    return bb->getTerminator() != nullptr;
}

EXTERN_API(bool) hasPredecessor(llvm::BasicBlock* bb)
{
    //auto it = llvm::predecessors(bb);
    //return it.begin() != it.end(); 
    for (auto pred : predecessors(bb)) {
        return true;
    }
    return false;
}

EXTERN_API(bool) currentBlockHasPredecessor(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return hasPredecessor(irb->GetInsertBlock());
}

EXTERN_API(uint32_t) currentBlockCountPredecessors(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return countPredecessors(irb->GetInsertBlock());
}

EXTERN_API(uint32_t) countPredecessors(llvm::BasicBlock* bb)
{
    uint32_t ret = 0;
    for (auto pred : predecessors(bb)) {
        ret++;
    }
    return ret;
}

EXTERN_API(void) currentBlockGetPredecessors(ManagedContext* ctx, llvm::IRBuilder<>* irb, llvm::BasicBlock** bbs, uint32_t bbc)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return getPredecessors(irb->GetInsertBlock(), bbs, bbc);
}

EXTERN_API(void) removeBasicBlock(llvm::BasicBlock* bb)
{
    bb->removeFromParent();
}

EXTERN_API(void) getPredecessors(llvm::BasicBlock* curr, llvm::BasicBlock** bbs, uint32_t bbc)
{
    for (auto it = llvm::pred_begin(curr); bbc > 0 && it != llvm::pred_end(curr); it++, bbc--) {
        bbs[bbc - 1] = *it;
    }
}

EXTERN_API(llvm::BasicBlock)* getCurrentBasicBlock(ManagedContext* ctx, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    return irb->GetInsertBlock();
}

llvm::Value* one(ManagedContext* ctx) {
    return llvm::Constant::getIntegerValue(llvm::Type::getInt32Ty(*ctx->context), llvm::APInt(32, 1));
}
llvm::Value* zero(ManagedContext* ctx) {
    return llvm::Constant::getIntegerValue(llvm::Type::getInt32Ty(*ctx->context), llvm::APInt(32, 0));
}

EXTERN_API(llvm::Value)* marshalMainMethodCMDLineArgs(ManagedContext* ctx, llvm::Function* managedMain, llvm::Function* fn, llvm::IRBuilder<>* irb)
{
    int counter = 0;
    // loop over all argv and create string {i32, i8*} from them; then call managedMain
    if (irb == nullptr)
        irb = ctx->builder;
    llvm::Value* argc, * argv;


    if (fn->arg_size() < 2) {
        llvm::errs() << "error: invalid arg_size";
        return nullptr;
    }

    argc = &fn->arg_begin()[0];
    argv = &fn->arg_begin()[1];

    auto doEnterLoop = irb->CreateICmpSGT(argc, one(ctx), "enter_marshal_loop");

    //llvm::BasicBlock * beforeLoop = llvm::BasicBlock::Create(*ctx->context, "before_loop", fn);
    llvm::BasicBlock* entry = irb->GetInsertBlock();
    llvm::BasicBlock* loop = llvm::BasicBlock::Create(*ctx->context, "marshal_loop", fn);
    llvm::BasicBlock* loopEnd = llvm::BasicBlock::Create(*ctx->context, "marshal_loop_end", fn);

    llvm::BasicBlock* startingBlock = irb->GetInsertBlock();

    //irb->CreateCondBr(doEnterLoop, beforeLoop, loopEnd);

    //irb->SetInsertPoint(beforeLoop);

    auto intTy = llvm::Type::getInt32Ty(*ctx->context);
    auto charPtrTy = llvm::Type::getInt8PtrTy(*ctx->context);
    llvm::FunctionType* fn_strlen_ty = llvm::FunctionType::get(intTy, { charPtrTy }, false);
    llvm::Function* fn_strlen = llvm::cast<llvm::Function>(ctx->M->getOrInsertFunction("strlen", fn_strlen_ty));

    auto fn_gcnew_ty = llvm::FunctionType::get(llvm::Type::getInt8PtrTy(*ctx->context), { intTy }, false);
    auto fn_gcnew = llvm::cast<llvm::Function>(ctx->M->getOrInsertFunction("gc_new", fn_gcnew_ty));
    auto string_ty = getStringType(ctx);
    auto stringArray = llvm::StructType::get(*ctx->context, { intTy, llvm::ArrayType::get(string_ty, 0) });

    llvm::Value* argcM1 = getMax(ctx, zero(ctx), irb->CreateSub(argc, one(ctx)), false, irb);
    llvm::Value* retLen;
    {
        auto string_sz = irb->CreateTruncOrBitCast(llvm::ConstantExpr::getSizeOf(string_ty), intTy);
        auto stringArray_sz = irb->CreateTruncOrBitCast(llvm::ConstantExpr::getSizeOf(stringArray), intTy);
        //llvm::errs() << *stringArray_sz;
        retLen = irb->CreateAdd(stringArray_sz, irb->CreateMul(string_sz, argcM1));
    }
    llvm::Value* ret = irb->CreateCall(fn_gcnew, { retLen }, "new_string(argc-1)");
    ret = irb->CreatePointerCast(ret, stringArray->getPointerTo());
    auto retLenPtr = irb->CreateGEP(ret, { zero(ctx), zero(ctx) });

    irb->CreateStore(argcM1, retLenPtr);
    //irb->CreateBr(loop);
    irb->CreateCondBr(doEnterLoop, loop, loopEnd);
    irb->SetInsertPoint(loop);

    auto index = irb->CreatePHI(intTy, 2, "index");
    index->addIncoming(one(ctx), entry);
    // 2nd incoming at the end of loop
    auto argPtr = irb->CreateGEP(argv, index, "argPtr");
    auto arg = irb->CreateLoad(argPtr, "argv[index]");
    auto len = irb->CreateCall(fn_strlen, { arg }, "argv[index].length");
    auto destPtr = irb->CreateGEP(ret, { zero(ctx), one(ctx), irb->CreateSub(index, one(ctx)) });
    auto destValuePtr = irb->CreateGEP(destPtr, { zero(ctx), zero(ctx) });
    auto destLenPtr = irb->CreateGEP(destPtr, { zero(ctx), one(ctx) });
    irb->CreateStore(arg, destValuePtr);
    irb->CreateStore(len, destLenPtr);
    auto indexP1 = irb->CreateAdd(index, one(ctx), "index++");
    index->addIncoming(indexP1, loop);
    auto doContinue = irb->CreateICmpSGT(argc, indexP1, "argc_LT_index");
    irb->CreateCondBr(doContinue, loop, loopEnd);
    irb->SetInsertPoint(loopEnd);
    //auto args = irb->CreatePHI(stringArray->getPointerTo(), 2);
    //args->addIncoming(ret, loop);
    //args->addIncoming(llvm::Constant::getNullValue(stringArray->getPointerTo()), startingBlock);
    return irb->CreateCall(managedMain, { ret });
}
EXTERN_API(llvm::Constant)* getInt128(ManagedContext* ctx, uint64_t hi, uint64_t lo)
{
    short test = 1;
    char* testp = (char*)& test;
    bool isBigEndian = testp[0] == 0;
    if (isBigEndian)
        return llvm::Constant::getIntegerValue(llvm::Type::getInt128Ty(*ctx->context), llvm::APInt(128, { hi, lo }));
    else
        return llvm::Constant::getIntegerValue(llvm::Type::getInt128Ty(*ctx->context), llvm::APInt(128, { lo ,hi }));
}
EXTERN_API(llvm::Constant)* getInt64(ManagedContext* ctx, uint64_t val)
{
    return llvm::Constant::getIntegerValue(llvm::Type::getInt64Ty(*ctx->context), llvm::APInt(64, val));
}

EXTERN_API(llvm::Constant)* getIntSZ(ManagedContext* ctx, uint64_t val)
{
    switch (sizeof(size_t)) {
        case 1:
            return getInt8(ctx, (uint8_t)val);
        case 2:
            return getInt16(ctx, (uint16_t)val);
        case 4:
            return getInt32(ctx, (uint32_t)val);
        case 8:
            return getInt64(ctx, val);
        case 16:
            return getInt128(ctx, 0, val);
        default:
            return getInt32(ctx, (uint32_t)val);
    }
}

EXTERN_API(bool) VerifyModule(ManagedContext* ctx)
{
    return !llvm::verifyModule(*ctx->M, &llvm::errs());
}
static void optExtension(const llvm::PassManagerBuilder& builder, llvm::legacy::PassManagerBase& pm) {
    if (builder.OptLevel > 0) {
        pm.add(llvm::createInductiveRangeCheckEliminationPass());

    }
}

static void optExtensionAllocationRemoving(const llvm::PassManagerBuilder& builder, llvm::legacy::PassManagerBase& pm) {
    if (builder.OptLevel > 2) {
        pm.add(new AllocationRemovingPass("gc_new"));
    }
}
static void optExtensionToStringRemoving(const llvm::PassManagerBuilder& builder, llvm::legacy::PassManagerBase& pm) {
    if (builder.OptLevel > 1) {
        std::unordered_map<std::string, char> m;

        m["to_str"] = 'i';
        m["uto_str"] = 'u';
        m["lto_str"] = 'l';
        m["ulto_str"] = 'U';
        m["llto_str"] = 'L';
        m["fto_str"] = 'f';
        pm.add(new ToStringRemovingPass(std::move(m)));
    }

}
EXTERN_API(void) optimize(ManagedContext* ctx, uint8_t optLvl, uint8_t maxIterations)
{
    bool changed, repeat;
    uint8_t it = 0;

    do
    {
        changed = false;

        llvm::PassManagerBuilder builder;
        builder.SLPVectorize = true;
        builder.LoopVectorize = true;

        builder.RerollLoops = true;
        builder.VerifyOutput = true;
        builder.OptLevel = optLvl;
        //builder.SizeLevel = 0;
        //builder.NewGVN = it == 0;
        if (it == 0) {
            builder.Inliner = llvm::createFunctionInliningPass(optLvl, 0, false);
            builder.addExtension(llvm::PassManagerBuilder::EP_LoopOptimizerEnd, optExtension);

        }


        builder.addExtension(llvm::PassManagerBuilder::EP_OptimizerLast, [ctx, &it](const llvm::PassManagerBuilder& builder, llvm::legacy::PassManagerBase& pm) {
            
            pm.add(llvm::createTailCallEliminationPass());
            pm.add(llvm::createMemCpyOptPass());
            pm.add(llvm::createPartialInliningPass());


            if (builder.OptLevel > 1) {
                std::unordered_map<std::string, char> m;

                m["to_str"] = 'i';
                m["uto_str"] = 'u';
                m["lto_str"] = 'l';
                m["ulto_str"] = 'U';
                m["llto_str"] = 'L';
                m["fto_str"] = 'f';
                pm.add(new ToStringRemovingPass(std::move(m)));
                pm.add(new StrMulRemovingPass());

                pm.add(new ThrowIfRemovingPass(*ctx->M));
                pm.add(llvm::createPartiallyInlineLibCallsPass());
            }
            if (builder.OptLevel > 2 && it > 1)
                pm.add(llvm::createLoadStoreVectorizerPass());
            if (it == 0) {
                if (builder.OptLevel > 2) {
                    pm.add(ctx->arp = new AllocationRemovingPass("gc_new"));
                }
            }
        });


        llvm::errs() << "optimize O" << (int)optLvl << "\r\n";


        {
            llvm::legacy::FunctionPassManager fpm(ctx->M);
            builder.populateFunctionPassManager(fpm);
            //fpm.doInitialization();
            for (auto& fn : ctx->M->functions()) {
                changed |= fpm.run(fn);
            }
            //fpm.doFinalization();
        }
        //llvm::errs() << ">>optimize functions done\r\n";
        {
            llvm::legacy::PassManager pm;
            builder.populateModulePassManager(pm);
            //builder.populateLTOPassManager(pm);
            changed |= pm.run(*ctx->M);
        }
        /*if (it == 0) {
            llvm::legacy::PassManager pm;
            builder.populateThinLTOPassManager(pm);
            changed |= pm.run(*ctx->M);
        }*/
        //llvm::errs() << ">>optimize module done\r\n";
        //llvm::errs() << *ctx->M;

        it++;
        /*if (auto arp = ctx->arp) {
            llvm::errs() << ">>start arg-changedInLastRuns\r\n";
            changed |= arp->changedInLastRuns();
            llvm::errs() << ">>done arg-changedInLastRuns\r\n";
        }*/
        repeat = optLvl > 2 && changed && it < maxIterations;

    } while (repeat);
    // automatic deallocation inside of the passmanager, so set ctx->arp to null to avoid misuse
    ctx->arp = nullptr;
    //llvm::errs() << "optimize ended\r\n";
    if (changed && optLvl > 2)
        llvm::errs() << "There might be still potential to optimize further\r\n";
}

EXTERN_API(void) linkTimeOptimization(ManagedContext* ctx)
{
    llvm::errs() << "perform LTO\r\n";
    llvm::legacy::PassManager pm;
    pm.add(llvm::createLowerTypeTestsPass(nullptr, nullptr));
    pm.run(*ctx->M);
}

EXTERN_API(void) forceVectorizationForAllLoops(ManagedContext* ctx, llvm::Function* fn)
{
    assert(fn && llvm::isa<llvm::Function>(fn));
    //llvm::errs() << "Vectorize all loops for function " << fn->getName() << "\r\n";

    llvm::DominatorTree dt;
    dt.recalculate(*fn);
    llvm::LoopInfo li;
    li.releaseMemory();
    li.analyze(dt);

    //llvm::errs() << "Detected all loops\r\n";

    for (auto loop : li) {
        //llvm::errs() << "Vectorize loop " << loop << "\r\n";
        llvm::addStringMetadataToLoop(loop, "llvm.loop.vectorize.enable", 1);
        llvm::addStringMetadataToLoop(loop, "llvm.loop.vectorize.width", llvm::VectorizerParams::MaxVectorWidth >> 3);
    }
    //llvm::errs() << "::end\r\n";
}

EXTERN_API(bool) forceVectorizationForCurrentLoop(ManagedContext* ctx, llvm::BasicBlock* loopBB, llvm::Function* fn)
{
    assert(loopBB && llvm::isa<llvm::BasicBlock>(loopBB));

    llvm::DominatorTree dt;
    dt.recalculate(*fn);
    llvm::LoopInfo li;
    li.releaseMemory();
    li.analyze(dt);

    auto loop = li.getLoopFor(loopBB);
    if (!loop)
        return false;
    llvm::addStringMetadataToLoop(loop, "llvm.loop.vectorize.enable", 1);
    llvm::addStringMetadataToLoop(loop, "llvm.loop.vectorize.width", llvm::VectorizerParams::MaxVectorWidth >> 3);
    return true;
}

llvm::GlobalVariable* createGlobalVTable(ManagedContext* ctx, const char* typeName, llvm::Type* _vtableTy, llvm::Function** virtualMethods, uint32_t virtualMethodc) {
    if (!llvm::isa<llvm::StructType>(_vtableTy)) {
        llvm::errs() << *_vtableTy << " is not a structtype";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "the vtable is of a structtype\r\n";
    }*/
    auto vtableTy = llvm::cast<llvm::StructType>(_vtableTy);
    auto vtableVal = llvm::ConstantStruct::get(vtableTy, llvm::ArrayRef<llvm::Constant*>((llvm::Constant * *)virtualMethods, virtualMethodc));

    auto glob = llvm::dyn_cast<llvm::GlobalVariable>(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), vtableTy));
    if (!glob) {
        llvm::errs() << *(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), vtableTy)) << " is not a globalVariable";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "Global Variable successfully created\r\n";
    }*/

    glob->setInitializer(vtableVal);
}

EXTERN_API(llvm::Value)* createVTable(ManagedContext* ctx, const char* typeName, llvm::Type* _vtableTy, llvm::Function** virtualMethods, uint32_t virtualMethodc)
{

    if (!llvm::isa<llvm::StructType>(_vtableTy)) {
        llvm::errs() << *_vtableTy << " is not a structtype";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "the vtable is of a structtype\r\n";
    }*/
    auto vtableTy = llvm::cast<llvm::StructType>(_vtableTy);
    auto vtableVal = llvm::ConstantStruct::get(vtableTy, llvm::ArrayRef<llvm::Constant*>((llvm::Constant * *)virtualMethods, virtualMethodc));

    auto glob = llvm::dyn_cast<llvm::GlobalVariable>(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), vtableTy));
    if (!glob) {
        llvm::errs() << *(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), vtableTy)) << " is not a globalVariable";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "Global Variable successfully created\r\n";
    }*/

    glob->setInitializer(vtableVal);
    //glob->setConstant(true);
    glob->addTypeMetadata(0, llvm::MDString::get(*ctx->context, typeName));
    return glob;
}

EXTERN_API(llvm::Value)* getVTable(ManagedContext* ctx, const char* typeName, llvm::Type* _vtableTy, llvm::Function** virtualMethods, uint32_t virtualMethodc, llvm::Value* superVtable)
{
    if (!llvm::isa<llvm::StructType>(_vtableTy) && virtualMethodc > 0) {

        llvm::errs() << *_vtableTy << " is not a structtype";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "the vtable is of a structtype\r\n";
    }*/
    llvm::Constant* vtableVal;
    if (auto vtableTy = llvm::dyn_cast<llvm::StructType>(_vtableTy)) {


        auto virtMetConstants = (llvm::Constant * *)virtualMethods;
        virtualMethodc = std::min(virtualMethodc, vtableTy->getNumElements());
        {
            uint32_t ind = 0;
            for (auto elemTp : vtableTy->elements()) {
                if (elemTp != virtMetConstants[ind]->getType()) {
                    virtMetConstants[ind] = llvm::ConstantExpr::getPointerBitCastOrAddrSpaceCast(virtMetConstants[ind], elemTp);
                }
                ind++;
            }
        }
        vtableVal = llvm::ConstantStruct::get(vtableTy, llvm::ArrayRef<llvm::Constant*>((llvm::Constant * *)virtMetConstants, virtualMethodc));
    }
    else {
        vtableVal = getNullPtr(ctx);
    }

    //llvm::errs() << "&vtableVal = " << vtableVal;
    /*if (vtableVal != nullptr) {
        llvm::errs() << "; vtableVal = " << *vtableVal << ":::end\r\n";
    }*/
    auto glob = llvm::dyn_cast<llvm::GlobalVariable>(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), _vtableTy));
    if (!glob) {
        llvm::errs() << *(ctx->M->getOrInsertGlobal((llvm::StringRef(typeName) + "_vtable").str(), _vtableTy)) << " is not a globalVariable";
        //getchar();
        exit(255);
    }
    /*else {
        llvm::errs() << "Global Variable successfully created\r\n";
    }*/

    glob->setInitializer(vtableVal);
    glob->setConstant(true);
    if (superVtable != nullptr) {
        assert(llvm::isa<llvm::GlobalVariable>(superVtable));
        //llvm::errs() << "Adding superMetadata\r\n";
        glob->copyMetadata(llvm::cast<llvm::GlobalVariable>(superVtable), 0);
    }
    //llvm::errs() << "Adding custom type metadata\r\n";
    glob->addTypeMetadata(0, llvm::MDString::get(*ctx->context, typeName));
    return glob;
}

EXTERN_API(llvm::Type)* getImplicitVTableType(ManagedContext* ctx, const char* name, llvm::Function** virtualMethods, uint32_t virtualMethodc)
{
    //llvm::errs() << "GetVTableType::virtualMethodc=" << virtualMethodc << "\r\n";
    if (virtualMethodc == 0)
        return llvm::Type::getInt8PtrTy(*ctx->context);

    std::vector<llvm::Type*> fnTys(virtualMethodc);

    for (uint32_t i = 0; i < virtualMethodc; ++i) {
        auto ty = virtualMethods[i]->getType();
        //llvm::errs() << "i = " << i << "; virtualMethods[i] = " << *ty << " at " << ty << "\r\n";
        //fnTys.push_back(ty);
        fnTys[i] = ty;
    }
    /*llvm::errs() << "The vtableTypes are: \r\n";
    for (uint32_t i = 0; i < virtualMethodc; ++i) {
        llvm::errs() << ">> " << fnTys[i] << "\r\n";
    }*/
    //llvm::errs() << "Created type-vector";
    //auto vtableTy = llvm::StructType::create(*ctx->context, fnTys, (llvm::StringRef(name) + "_vtableTy").str());
    auto ret = llvm::StructType::get(*ctx->context, fnTys);
    ret->setName((llvm::StringRef(name) + "_vtableTy").str());

    //llvm::errs() << ":::end\r\n";
    //llvm::errs() << "Created vtableType: " << *ret << ":::end\r\n";
    return ret;
}

EXTERN_API(llvm::Type)* getVTableType(ManagedContext* ctx, const char* name, llvm::Type** fnTypes, uint32_t fnTypec)
{
    if (fnTypec == 0)
        return llvm::Type::getInt8PtrTy(*ctx->context);
    auto ret = llvm::StructType::get(*ctx->context, llvm::ArrayRef<llvm::Type*>(fnTypes, fnTypec));
    ret->setName((llvm::StringRef(name) + "_vtableTy").str());
    return ret;
}

EXTERN_API(llvm::Value)* isInst(ManagedContext* ctx, llvm::Value* vtablePtr, const char* typeName, llvm::IRBuilder<>* irb)
{
    if (irb == nullptr)
        irb = ctx->builder;
    auto asVoidPtr = irb->CreateBitOrPointerCast(vtablePtr, llvm::Type::getInt8PtrTy(*ctx->context));
    auto typeTest = llvm::Intrinsic::getDeclaration(ctx->M, llvm::Intrinsic::type_test);

    return irb->CreateCall(typeTest, { asVoidPtr, llvm::MetadataAsValue::get(*ctx->context, llvm::MDString::get(*ctx->context,typeName)) });
}
