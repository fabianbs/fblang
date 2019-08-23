/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

#pragma once
#include <llvm/Pass.h>
#include <llvm/IR/Function.h>
#include <llvm/Analysis/AliasAnalysis.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Transforms/Utils/BasicBlockUtils.h>
#include <string>
#include <sstream>
#include <unordered_set>
#include <unordered_map>
#include "StringHelper.h"
namespace {
    using namespace llvm;
    using namespace StringHelper;
    class ToStringRemovingPass :public FunctionPass {
        static char id;
        // toStrName -> type
        // types: i, u, l, U, L, f
        std::unordered_map<std::string, char> toStringNames;
        bool performToString(Constant *cst, char tosFn, std::string &ret) {
            llvm::errs() << ">PerformToString " << *cst << " with " << tosFn << "\r\n";
            switch (tosFn) {
                case 'i':
                    if (auto csi = dyn_cast<ConstantInt>(cst)) {
                        uint32_t _val = (uint32_t)csi->getLimitedValue((1ui64 << 32) - 1);
                        int32_t val = *((int32_t *)& _val);
                        ret = std::to_string(val);
                        return true;
                    }
                    break;
                case 'u':
                    if (auto csi = dyn_cast<ConstantInt>(cst)) {
                        uint32_t val = (uint32_t)csi->getLimitedValue((1ui64 << 32) - 1);
                        ret = std::to_string(val);
                        return true;
                    }
                    break;
                case 'l':
                    if (auto csi = dyn_cast<ConstantInt>(cst)) {
                        uint64_t _val = csi->getLimitedValue();
                        int64_t val = *((int64_t *)& _val);
                        ret = std::to_string(val);
                        return true;
                    }
                    break;
                case 'U':
                    if (auto csi = dyn_cast<ConstantInt>(cst)) {
                        uint64_t val = csi->getLimitedValue();
                        ret = std::to_string(val);
                        return true;
                    }
                    break;
                case 'L':
                    if (auto csi = dyn_cast<ConstantInt>(cst)) {
                        auto aval = csi->getValue();
                        raw_string_ostream s(ret);
                        s << aval;
                        ret = s.str();
                        return true;
                    }
                    break;
                case 'f':
                    if (auto csf = dyn_cast<ConstantFP>(cst)) {
                        auto val = csf->getValueAPF().convertToDouble();
                        ret = std::to_string(val);
                        return true;
                    }
                    break;
            }
            ret = std::string();
            return false;
        }
        
        void InsertConstantString(std::string &str, Module &M, IRBuilder<> &builder, Value *stringAlloca) {
            auto strVal = builder.CreateGlobalStringPtr(str);
            auto strLen = CreateIntSZ(M, str.size());
            auto zero = Constant::getIntegerValue(Type::getInt32Ty(M.getContext()), APInt(32, 0));

            //llvm::errs() << "GEP: " << *stringAlloca << " at " << *zero << ", " << *zero << "\n";
            //llvm::errs() << "GEP: " << *stringAlloca << " at " << *zero << ", " << *CreateIntSZ(M, 1) << "\n";
            //llvm::errs() << "GEPTy:" << *stringAlloca->getType() << " = " << *stringAlloca->getType()->getPointerElementType() << "* at " << *zero->getType() << ", " << *zero->getType() << "\n";
            auto valGEP = builder.CreateGEP(stringAlloca, { zero, zero });
            //llvm::errs() << ":::END1/2";
            auto lenGEP = builder.CreateGEP(stringAlloca, { zero, Constant::getIntegerValue(Type::getInt32Ty(M.getContext()), APInt(32, 1)) });
            //llvm::errs() << ":::END2/2";

            builder.CreateStore(strVal, valGEP);
            builder.CreateStore(strLen, lenGEP);
        }
    public:
        ToStringRemovingPass(std::unordered_map<std::string, char> &&tosNames) :FunctionPass(id), toStringNames(tosNames) { }
        ~ToStringRemovingPass() { }
        virtual bool runOnFunction(Function &F)override {
            bool changed = false;
            std::vector<Instruction *> toRemove;
            for (auto &bb : F.getBasicBlockList()) {
                for (auto &inst : bb) {
                    if (auto call = dyn_cast<CallInst>(&inst)) {

                        if (call->getNumArgOperands() == 2 && isa<Constant>(call->getArgOperand(0)) && call->getCalledFunction() != nullptr) {
                            auto it = toStringNames.find(call->getCalledFunction()->getName());
                            if (it != toStringNames.end()) {
                                //llvm::errs() << "performToString started\r\n";
                                std::string str;
                                if (performToString((Constant *)call->getArgOperand(0), it->second, str)) {
                                    //llvm::errs() << "performToString ended\r\n";

                                    simple_ilist<Instruction>::iterator ii(inst);
                                    IRBuilder<> irb(&bb, ii);
                                    //llvm::errs() << "insertconstantstring started\r\n";
                                    InsertConstantString(str, *F.getParent(), irb, call->getOperand(1));
                                    //llvm::errs() << "insertconstantstring ended\r\n";
                                    //BasicBlock::iterator ii(call);

                                //ReplaceInstWithInst(ii->getParent()->getInstList(), ii, glob);
                                //call->removeFromParent();
                                    toRemove.push_back(call);
                                    //llvm::errs() << "removeFromParent ended\r\n";
                                    changed = true;
                                }
                            }
                            /*else {

                                llvm::errs() << "No toString-method: " << call->getCalledFunction()->getName() << "\r\n";
                            }*/
                        }/*else{
                            llvm::errs() << "Wrong signature for toString-method: " << *call << "\r\n";
                        }*/
                    }
                }
            }
            //llvm::errs() << "start removing...\r\n";
            for (auto inst : toRemove) {
                //llvm::errs() << ">> remove " << *inst << "\r\n";
                //inst->removeFromParent();
                inst->eraseFromParent();
            }
            //llvm::errs() << "runonFunction ended\r\n";
            return changed;
        }
    };
    char ToStringRemovingPass::id = 0;
}
