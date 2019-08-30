#pragma once
#include <llvm/IR/Module.h>
#include <llvm/IR/IRBuilder.h>
namespace StringHelper{
    llvm::Constant *CreateIntSZ(llvm::Module &M, size_t val);
    llvm::Constant *CreateInt32(llvm::Module & M, int val);
    llvm::Type *GetSizeType(llvm::Module &M);
    llvm::Constant *CreateString(llvm::Module& M, llvm::StringRef strVal);
    llvm::Value *CreateString(llvm::Module &M, llvm::Value *strVal, llvm::Value *strLen, llvm::IRBuilder<> &irb);
    llvm::Type *CreateStringType(llvm::Module &M);
}