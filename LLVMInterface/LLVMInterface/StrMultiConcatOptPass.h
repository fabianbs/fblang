#pragma once
#include <llvm/Pass.h>
#include <llvm/IR/Function.h>
#include <llvm/IR/Instructions.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Transforms/Utils/BasicBlockUtils.h>
#include <tuple>

namespace {
    using namespace llvm;
    class StrMultiConcatOptPass :public FunctionPass {
        static char id;
        StrMultiConcatOptPass();
        virtual bool runOnFunction(Function &F)override;
    };
    
}