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
#include <llvm/Analysis/LoopInfo.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Transforms/Utils/BasicBlockUtils.h>
#include <llvm/IR/IRBuilder.h>
#include <string>
#include <unordered_set>
#include <unordered_map>
namespace {
    using namespace llvm;
    class AllocationRemovingPass :public llvm::FunctionPass {
        static char ID;
        bool changedLastRun = false;
        std::string mallocName;
        template<typename T>
        bool tryInsert(std::unordered_set<T>& set, const T& val) {
            auto it = set.find(val);
            if (it != set.end())
                return false;
            else {
                set.insert(val);
                return true;
            }
        }
        template<typename T>
        bool contains(std::unordered_set<T>& set, const T& val) {
            auto it = set.find(val);
            return it != set.end();
        }

    public:

        AllocationRemovingPass(StringRef name) :FunctionPass(ID) {
            mallocName = name.str();
        }

        // Geerbt �ber FunctionPass
        virtual bool runOnFunction(Function& F) override {
            bool ret = false;
            //AliasAnalysis *AA = &getAnalysis<AAResultsWrapperPass>().getAAResults();
            //llvm::errs() << "Optimize " << F.getName() << ": " << *F.getFunctionType() << "\r\n";
            bool isMain = F.getName() == "main";
            bool hasGCInit = false;
            //llvm::errs() << "Optimize " << F;
            auto* loop = &getAnalysis<LoopInfoWrapperPass>().getLoopInfo();
            bool changes;
            std::unordered_set<Value*> sinks;
            std::unordered_set<Value*> taints;
            std::unordered_set<Value*> taintMem;
            std::unordered_set<CallInst*> sources;
            //llvm::errs() << "Find all sources\r\n";
            llvm::SmallVector<Instruction*, 10> toRemove;

            // find all sources
            for (auto& bb : F.getBasicBlockList()) {
                for (auto& inst : bb) {
                    auto hasLoop = loop->getLoopFor(&bb) != nullptr;

                    if (auto call = dyn_cast<CallInst>(&inst)) {
                        //llvm::errs() << "Found call: " << *call << "\r\n";
                        if (call->getCalledFunction() != nullptr && call->getCalledFunction()->getName() == mallocName) {
                            if (auto constArg = dyn_cast<ConstantInt>(call->getArgOperand(0))) {
                                if (constArg->getValue().isNullValue()) {
                                    if (!(isMain && &bb == &F.getEntryBlock() && !hasGCInit)) {
                                        call->replaceAllUsesWith(Constant::getNullValue(Type::getInt8PtrTy(F.getContext())));

                                        //call->eraseFromParent();
                                        toRemove.push_back(call);
                                    }
                                    else {
                                        hasGCInit = true;
                                    }
                                }
                                else if (constArg->getValue().roundToDouble() <= 256) {
                                    // prevent too large stack-sizes
                                    // do not place allocas into loops
                                    if (!hasLoop) {
                                        sources.insert(call);
                                        //llvm::errs() << ">>" << "Detecting source: " << *call << "\r\n";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (auto inst : toRemove) {
                inst->eraseFromParent();
            }
            toRemove.clear();
            //llvm::errs() << "Find all sinks\r\n";
            // find all sinks
            for (auto& arg : F.args()) {
                if (arg.getType()->isPointerTy())
                    sinks.insert(&arg);

            }
            //llvm::errs() << "Reaching loop\r\n";
            // forall sources detect taint-flow
            for (auto src : sources) {
                bool canBeRemoved = true;

                taints.clear();
                taintMem.clear();
                // src is tainted at the beginning
                taints.insert(src);
                do {
                    changes = false;
                    for (auto& bb : F.getBasicBlockList()) {
                        for (auto& inst : bb) {
                            //llvm::errs() << ">>" << inst << "\r\n";
                            if (auto gep = dyn_cast<GetElementPtrInst>(&inst)) {
                                if (contains<Value*>(sinks, gep->getPointerOperand())) {
                                    changes |= tryInsert<Value*>(sinks, gep);
                                }
                                else if (contains<Value*>(sinks, gep)) {
                                    changes |= tryInsert<Value*>(sinks, gep->getPointerOperand());
                                }
                                if (contains<Value*>(taints, gep->getPointerOperand())) {
                                    changes |= tryInsert<Value*>(taints, gep);
                                }
                                else if (contains<Value*>(taints, gep)) {
                                    changes |= tryInsert<Value*>(taints, gep->getPointerOperand());
                                }
                                if (contains<Value*>(taintMem, gep->getPointerOperand())) {
                                    changes |= tryInsert<Value*>(taintMem, gep);
                                }
                                else if (contains<Value*>(taintMem, gep)) {
                                    changes |= tryInsert<Value*>(taintMem, gep->getPointerOperand());
                                }
                            }
                            else if (auto insert = dyn_cast<InsertValueInst>(&inst)) {



                                if (contains<Value*>(taints, insert->getInsertedValueOperand())) {
                                    changes |= tryInsert<Value*>(taints, insert);
                                }
                                if (contains<Value*>(taintMem, insert->getInsertedValueOperand())) {
                                    changes |= tryInsert<Value*>(taints, insert);
                                }
                                if (contains<Value*>(taints, insert->getAggregateOperand())) {
                                    changes |= tryInsert<Value*>(taints, insert);
                                }
                            }
                            else if (auto store = dyn_cast<StoreInst>(&inst)) {
                                if (contains<Value*>(taints, store->getValueOperand())) {
                                    if (contains<Value*>(sinks, store->getPointerOperand())) {
                                        canBeRemoved = false;
                                        //llvm::errs() << "crucial instruction: " << inst << "\r\n";
                                        goto loopEnd;
                                    }
                                    else {
                                        changes |= tryInsert(taintMem, store->getPointerOperand());
                                    }
                                }
                                else if (contains<Value*>(taintMem, store->getValueOperand())) {
                                    if (contains<Value*>(sinks, store->getPointerOperand())) {
                                        canBeRemoved = false;
                                        //llvm::errs() << "crucial instruction: " << inst << "\r\n";
                                        goto loopEnd;
                                    }
                                    else {
                                        changes |= tryInsert<Value*>(taintMem, store->getPointerOperand());
                                    }
                                }
                                /*else {
                                    llvm::errs() << ">> Store " << *store << " is ok\r\n";
                                }*/
                            }
                            else if (auto load = dyn_cast<LoadInst>(&inst)) {
                                if (contains<Value*>(taintMem, load->getPointerOperand())) {
                                    changes |= tryInsert<Value*>(taints, load);
                                }
                            }
                            else if (auto call = dyn_cast<CallInst>(&inst)) {
                                auto argc = call->getNumArgOperands();
                                for (decltype(argc) i = 0; i < argc; ++i) {
                                    if (contains<Value*>(taints, call->getArgOperand(i))
                                        || contains<Value*>(taintMem, call->getArgOperand(i))) {
                                        if (!call->paramHasAttr(i, llvm::Attribute::AttrKind::NoCapture)) {
                                            canBeRemoved = false;
                                            //llvm::errs() << "crucial instruction: " << inst << "\r\n";
                                            goto loopEnd;
                                        }
                                        else if(call->isTailCall()){
                                            // tail call requires the callee not to dereference stack-pointer-operands
                                            call->setTailCall(false);
                                        }
                                    }
                                }
                            }
                            else if (auto cast = dyn_cast<CastInst>(&inst)) {
                                if (contains<Value*>(sinks, cast->getOperand(0))) {
                                    changes |= tryInsert<Value*>(sinks, cast);
                                }
                                else if (contains<Value*>(sinks, cast)) {
                                    changes |= tryInsert<Value*>(sinks, cast->getOperand(0));
                                }
                                if (contains<Value*>(taints, cast->getOperand(0))) {
                                    changes |= tryInsert<Value*>(taints, cast);
                                }
                                else if (contains<Value*>(taints, cast)) {
                                    changes |= tryInsert<Value*>(taints, cast->getOperand(0));
                                }
                                if (contains<Value*>(taintMem, cast->getOperand(0))) {
                                    changes |= tryInsert<Value*>(taintMem, cast);
                                }
                                else if (contains<Value*>(taintMem, cast)) {
                                    changes |= tryInsert<Value*>(taintMem, cast->getOperand(0));
                                }
                            }
                            else if (auto ret = dyn_cast<ReturnInst>(&inst)) {
                                if (contains<Value*>(taints, ret->getReturnValue())
                                    || contains<Value*>(taintMem, ret->getReturnValue())) {
                                    canBeRemoved = false;
                                    //llvm::errs() << "crucial instruction: " << inst << "\r\n";
                                    goto loopEnd;
                                }
                            }

                        }
                    }
                } while (changes);
            loopEnd:
                if (canBeRemoved && !contains<Value*>(sinks, src)) {
                    auto i8Ty = Type::getInt8Ty(F.getContext());
                    auto allocaInst = new AllocaInst(i8Ty, 0, src->getArgOperand(0), mallocName);
                    /*llvm::errs() << "Replacing " << *src << " from function " << F.getName() << " with " << *allocaInst << "\r\n";
                    llvm::errs() << "Sinks:\r\n";
                    for (auto snk : sinks) {
                        llvm::errs() << ">>" << *snk << "\r\n";
                    }
                    llvm::errs() << "end sinks\r\n";
                    llvm::errs() << "Taints:\r\n";
                    for (auto tnt : taints) {
                        llvm::errs() << ">>" << *tnt << "\r\n";
                    }
                    llvm::errs() << "end taints\r\n";
                    llvm::errs() << "TaintMem:\r\n";
                    for (auto tnt : taintMem) {
                        llvm::errs() << ">>" << *tnt << "\r\n";
                    }
                    llvm::errs() << "end taintMem\r\n";*/
                    //llvm::errs() << F;
                    BasicBlock::iterator ii(src);
                    ReplaceInstWithInst(ii->getParent()->getInstList(), ii, allocaInst);
                    IRBuilder<> inserter(allocaInst->getNextNode());
                    auto byteArrayTy = ArrayType::get(i8Ty, cast<ConstantInt>(src->getArgOperand(0))->getLimitedValue());
                    auto bc = inserter.CreateBitCast(allocaInst, byteArrayTy->getPointerTo());
                    inserter.CreateStore(Constant::getNullValue(byteArrayTy), bc);
                    //llvm::errs() << F;
                    //changedLastRun = true;
                    ret = true;
                }
                /*else {
                    llvm::errs() << "Nothing left to optimize\r\n";
                }*/
            }
            //llvm::errs() << "End \r\n";
            //TODO
            return changedLastRun = ret;
        }
        void getAnalysisUsage(AnalysisUsage& AU) const override {
            //AU.addRequired<AAResultsWrapperPass>();
            AU.addRequired<LoopInfoWrapperPass>();
        }
        llvm::StringRef getMallocName() {
            return mallocName;
        }
        void setMallocName(llvm::StringRef name) {
            mallocName = name.str();
        }
        bool& changedInLastRuns() {
            return changedLastRun;
        }
    };
    char AllocationRemovingPass::ID = 0;

}