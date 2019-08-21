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
#include <utility>
#include <tuple>
namespace {
    using namespace llvm;
    class StrMulRemovingPass :public FunctionPass {
        static char id;
        bool GetConstantStringFromGep(Value* gep, StringRef& ret) {
            if (auto strGep = dyn_cast<ConstantExpr>(gep)) {
                if (auto glob = dyn_cast<GlobalVariable>(strGep->getOperand(0))) {
                    if (glob->hasInitializer()) {
                        if (auto dat = dyn_cast<ConstantDataArray>(glob->getInitializer())) {
                            ret = dat->getAsCString();
                            return true;
                        }
                        //else errs() << "gep is not constantdatatarray\r\n";
                    }
                    //else errs() << "gep has no initializer\r\n";
                }
                //else errs() << "gep is not globalvariable\r\n";
            }
            else if (auto strGEP = dyn_cast<GetElementPtrInst>(gep)) {
                if (auto glob = dyn_cast<GlobalVariable>(strGEP->getPointerOperand())) {
                    if (glob->hasInitializer()) {
                        if (auto dat = dyn_cast<ConstantDataArray>(glob->getInitializer())) {
                            ret = dat->getAsCString();
                            return true;
                        }
                        //else errs() << "gep is not constantdatatarray\r\n";
                    }
                    //else errs() << "gep has no initializer\r\n";
                }
                //else errs() << "gep is not globalvariable\r\n";
            }
            //else errs() << "gep is no gep\r\n";
            return false;
        }
    public:
        StrMulRemovingPass() :FunctionPass(id) {

        }
        ~StrMulRemovingPass() {}
        virtual bool runOnFunction(Function& F)override {
            //errs() << "strmulreplace on " << F.getName() << "\r\n";
            //toReplace-array
            llvm::SmallVector<std::tuple<CallInst*, StringRef, uint32_t>, 5> strmulReplace;
            llvm::SmallVector<std::tuple<CallInst*, StringRef, StringRef>, 5> strconcatReplace;
            llvm::SmallVector<std::tuple<CallInst*, uint8_t>, 5> strconcatRemove;

            for (auto& bb : F.getBasicBlockList()) {
                for (auto& inst : bb) {
                    if (auto call = dyn_cast<CallInst>(&inst)) {
                        if (!call->getCalledFunction())
                            continue;
                        if (call->getCalledFunction()->getName() == "strmul") {
                            //errs() << "Found StrMul\r\n";
                            StringRef str;
                            uint64_t strLen, factor;
                            if (auto cLen = dyn_cast<ConstantInt>(call->getArgOperand(1)))
                                strLen = cLen->getLimitedValue();
                            else
                                continue;
                            if (auto cFactor = dyn_cast<ConstantInt>(call->getArgOperand(2)))
                                factor = cFactor->getLimitedValue();
                            else
                                continue;
                            if (factor > 256 || strLen > 256 || factor * strLen > 8192)
                                continue;
                            if (GetConstantStringFromGep(call->getArgOperand(0), str)) {
                                str = str.slice(0, strLen);
                                strmulReplace.emplace_back(call, str, factor);
                            }
                        }
                        else if (call->getCalledFunction()->getName() == "strconcat") {
                            //errs() << "FoundStrConcat\r\n";
                            StringRef lhs, rhs;
                            uint64_t lhsLen = 0, rhsLen = 0;
                            uint8_t hasConstant = 0;
                            uint8_t index = 0;
                            if (auto cLhsLen = dyn_cast<ConstantInt>(call->getArgOperand(1))) {
                                lhsLen = cLhsLen->getLimitedValue();
                                hasConstant++;
                                index = 2;
                            }
                            if (auto cRhsLen = dyn_cast<ConstantInt>(call->getArgOperand(3))) {
                                rhsLen = cRhsLen->getLimitedValue();
                                hasConstant++;
                                index = 0;
                            }
                            else if (!hasConstant)
                                continue;

                            if (hasConstant == 2) {
                                if (GetConstantStringFromGep(call->getArgOperand(0), lhs) && GetConstantStringFromGep(call->getArgOperand(2), rhs)) {
                                    lhs = lhs.slice(0, lhsLen);
                                    rhs = rhs.slice(0, rhsLen);
                                    strconcatReplace.emplace_back(call, lhs, rhs);
                                }
                            }
                            else {
                                if (index == 2 && lhsLen == 0 || index == 0 && rhsLen == 0)
                                    strconcatRemove.emplace_back(call, index);
                            }
                        }
                        //else errs() << "no strmul: " << *call << "\r\n";
                    }
                }
            }

            for (auto& kvp : strmulReplace) {
                uint32_t factor;
                StringRef str;
                CallInst* call;

                std::tie(call, str, factor) = kvp;

                std::string retStr;
                raw_string_ostream s(retStr);
                for (uint64_t i = 0; i < factor; ++i) {
                    s << str;
                }
                s.flush();
                auto& ctx = F.getContext();
                /*auto dataTy = ArrayType::get(Type::getInt8Ty(ctx), retStr.size());
                auto initializer = ConstantDataArray::getString(ctx, retStr);

                auto nwGlob = new GlobalVariable(dataTy, true, GlobalValue::PrivateLinkage, initializer);*/
                auto zero = ConstantInt::getNullValue(Type::getInt32Ty(ctx));
                auto one = ConstantInt::get(Type::getInt32Ty(ctx), APInt(32, 1));
                IRBuilder<> inserter(call);
                auto gep = inserter.CreateGlobalStringPtr(retStr);
                auto strAlloca = call->getArgOperand(3);
                auto strValueGEP = inserter.CreateGEP(strAlloca, { zero, zero });
                inserter.CreateStore(gep, strValueGEP);
                auto strLenGEP = inserter.CreateGEP(strAlloca, { zero, one });
                inserter.CreateStore(ConstantInt::get(Type::getInt32Ty(ctx), APInt(32, retStr.size())), strLenGEP);

                call->eraseFromParent();
                //auto repl = GetElementPtrInst::CreateInBounds(dataTy, nwGlob, { zero, zero });
                //BasicBlock::iterator ii(call);
                //ReplaceInstWithInst(ii->getParent()->getInstList(), ii, repl);
            }
            for (auto kvp : strconcatReplace) {
                StringRef lhs, rhs;
                CallInst* call;
                std::tie(call, lhs, rhs) = kvp;
                auto retStr = (lhs + rhs).str();

                auto & ctx = F.getContext();
                /*auto dataTy = ArrayType::get(Type::getInt8Ty(ctx), retStr.size());
                auto initializer = ConstantDataArray::getString(ctx, retStr);

                auto nwGlob = new GlobalVariable(dataTy, true, GlobalValue::PrivateLinkage, initializer);*/
                auto zero = ConstantInt::getNullValue(Type::getInt32Ty(ctx));
                auto one = ConstantInt::get(Type::getInt32Ty(ctx), APInt(32, 1));
                IRBuilder<> inserter(call);
                auto gep = inserter.CreateGlobalStringPtr(retStr);
                auto strAlloca = call->getArgOperand(4);
                auto strValueGEP = inserter.CreateGEP(strAlloca, { zero, zero });
                inserter.CreateStore(gep, strValueGEP);
                auto strLenGEP = inserter.CreateGEP(strAlloca, { zero, one });
                inserter.CreateStore(ConstantInt::get(Type::getInt32Ty(ctx), APInt(32, retStr.size())), strLenGEP);

                call->eraseFromParent();
                //auto repl = GetElementPtrInst::CreateInBounds(dataTy, nwGlob, { zero, zero });
                //BasicBlock::iterator ii(call);
                //ReplaceInstWithInst(ii->getParent()->getInstList(), ii, repl);
            }
            for (auto kvp : strconcatRemove) {
                CallInst* call;
                uint8_t index;
                std::tie(call, index) = kvp;

                IRBuilder<> inserter(call);
                auto strVal = call->getArgOperand(index);
                auto strLen = call->getArgOperand(index + 1);
                auto strRet = call->getArgOperand(4);
                auto strRetVal = inserter.CreateStructGEP(strRet, 0);
                auto strRetLen = inserter.CreateStructGEP(strRet, 1);
                inserter.CreateStore(strVal, strRetVal);
                inserter.CreateStore(strLen, strRetLen);
                call->eraseFromParent();
            }
            return !strmulReplace.empty() || !strconcatReplace.empty() || !strconcatRemove.empty();
        }

    };
    char StrMulRemovingPass::id = 0;
}