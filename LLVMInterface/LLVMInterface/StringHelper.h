#pragma once
#include <llvm/IR/Module.h>
namespace {
    llvm::Constant *CreateIntSZ(llvm::Module &M, size_t val);
    llvm::Type *GetSizeType(llvm::Module &M);
    llvm::Constant *CreateString(llvm::Module& M, llvm::StringRef strVal);
    llvm::Value *CreateString(llvm::Module &M, llvm::Value *strVal, llvm::Value *strLen, llvm::IRBuilder<> &irb);
    llvm::Type *CreateStringType(llvm::Module &M);
}