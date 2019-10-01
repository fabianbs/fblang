#include "StringHelper.h"
#include <llvm/IR/IRBuilder.h>
#include <utf8.h>
namespace StringHelper {
    using namespace llvm;
    llvm::Constant *CreateIntSZ(Module &M, size_t val) {
        switch (sizeof(size_t)) {
            case 1:
                return Constant::getIntegerValue(Type::getInt8Ty(M.getContext()), APInt(8, val));
            case 2:
                return Constant::getIntegerValue(Type::getInt16Ty(M.getContext()), APInt(16, val));
            case 8:
                return Constant::getIntegerValue(Type::getInt64Ty(M.getContext()), APInt(64, val));
            case 16:
                return Constant::getIntegerValue(Type::getInt128Ty(M.getContext()), APInt(128, val));
            default:
                return Constant::getIntegerValue(Type::getInt32Ty(M.getContext()), APInt(32, val));
        }
    }
    llvm::Constant *CreateInt32(llvm::Module &M, int val) {
        return Constant::getIntegerValue(Type::getInt32Ty(M.getContext()), APInt(32, val));
    }
    llvm::Type *GetSizeType(llvm::Module &M) {
        switch (sizeof(size_t)) {
            case 1:
                return Type::getInt8Ty(M.getContext());
            case 2:
                return Type::getInt16Ty(M.getContext());
            case 8:
                return Type::getInt64Ty(M.getContext());
            case 16:
                return Type::getInt128Ty(M.getContext());
            default:
                return Type::getInt32Ty(M.getContext());
        }
    }
    llvm::Constant *CreateString(Module &M, llvm::StringRef strVal) {
        IRBuilder<> irb(M.getContext());
        auto gep = irb.CreateGlobalStringPtr(strVal);
        auto len = CreateIntSZ(M, strVal.size());
        

        return ConstantStruct::get((StructType *)CreateStringType(M), { gep, len });
    }
    llvm::Value *CreateString(llvm::Module &M, llvm::Value *strVal, llvm::Value *strLen, IRBuilder<> &irb) {
        auto ty = CreateStringType(M);
        llvm::Value *ret = llvm::UndefValue::get(ty);
        ret = irb.CreateInsertValue(ret, strVal, { 0 });
        ret = irb.CreateInsertValue(ret, strLen, { 1 });
        return ret;
    }
    llvm::Type *CreateStringType(llvm::Module &M) {
        auto ret = M.getTypeByName("string");
        if (!ret)
            ret = llvm::StructType::create({ llvm::PointerType::getInt8PtrTy(M.getContext()), GetSizeType(M) }, "string", false);
        return ret;
    }
}