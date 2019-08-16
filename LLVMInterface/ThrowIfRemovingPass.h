#pragma once
#include <llvm/Pass.h>
#include <llvm/IR/Function.h>
#include <llvm/Analysis/AliasAnalysis.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Transforms/Utils/BasicBlockUtils.h>
#include <string>
#include <sstream>
namespace {
    using namespace llvm;
    class ThrowIfRemovingPass :public FunctionPass
    {
        static char id;
    public:
        ThrowIfRemovingPass(Module& M) :FunctionPass(id) {
            // insert the functions such that runOnFunction does not modify M
            auto intTy = Type::getInt32Ty(M.getContext());
            auto longTy = Type::getInt64Ty(M.getContext());
            auto voidTy = Type::getVoidTy(M.getContext());
            M.getOrInsertFunction("throwIfOutOfBounds", voidTy, intTy, intTy);
            M.getOrInsertFunction("throwIfOutOfBounds64", voidTy, longTy, longTy);
        }
        ~ThrowIfRemovingPass() {}

        virtual bool runOnFunction(Function& F)override {
            SmallVector<Instruction*, 10> toRemove;
            SmallVector<CallInst*, 5> toReplace;
            for (auto& bb : F.getBasicBlockList()) {
                for (auto& inst : bb) {
                    if (auto call = dyn_cast<CallInst>(&inst)) {
                        if (call->getCalledFunction() != nullptr) {
                            if (call->getCalledFunction()->getName() == "throwIfNull") {
                                if (call->paramHasAttr(0, llvm::Attribute::NonNull)) {
                                    toRemove.push_back(call);
                                }
                            }
                            else if (call->getCalledFunction()->getName() == "throwIfOutOfBounds" || call->getCalledFunction()->getName() == "throwIfOutOfBounds64") {
                                if (auto index = dyn_cast<ConstantInt>(call->getArgOperand(0))) {
                                    if (auto length = dyn_cast<ConstantInt>(call->getArgOperand(1))) {

                                        if (index->getLimitedValue() < length->getLimitedValue()) {
                                            toRemove.push_back(call);
                                        }
                                    }
                                }
                            }
                            else if (call->getCalledFunction()->getName() == "throwIfNullOrOutOfBounds" || call->getCalledFunction()->getName() == "throwIfNullOrOutOfBounds64") {
                                if (call->paramHasAttr(1, llvm::Attribute::NonNull)) {
                                    toReplace.push_back(call);
                                }
                            }
                        }
                    }
                }
            }
            for (auto inst : toRemove) {
                //llvm::errs() << "Remove " << *inst << "\r\n";
                inst->eraseFromParent();
            }

            for (auto inst : toReplace) {
                //llvm::errs() << "Replace " << *inst << "\r\n";
                IRBuilder<> inserter(inst);
                bool is64Bit = inst->getCalledFunction()->getName() == "throwIfNullOrOutOfBounds64";
                auto voidTy = inserter.getVoidTy();
                auto intTy = is64Bit ? inserter.getInt64Ty() : inserter.getInt32Ty();
                auto throwFnName = is64Bit ? "throwIfOutOfBounds64" : "throwIfOutOfBounds";
                auto callee = cast<Function>(F.getParent()->getOrInsertFunction(throwFnName, voidTy, intTy, intTy));
                //inserter.CreateCall(callee, { inst->getArgOperand(0), loadInst });
                auto loadInst = inserter.CreateLoad(inst->getArgOperand(1));
                BasicBlock::iterator ii(inst);
                //auto loadInst = new LoadInst(inst->getArgOperand(1));
                auto callInst = CallInst::Create(callee, { inst->getArgOperand(0), loadInst });
                ReplaceInstWithInst(ii->getParent()->getInstList(), ii, callInst);

            }

            return !toRemove.empty() || !toReplace.empty();
        }
    };
    char ThrowIfRemovingPass::id = 0;
}