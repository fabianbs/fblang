#include "StrMultiConcatOptPass.h"
#include <memory>
#include <unordered_map>
#include <unordered_set>
#include <llvm/IR/Module.h>
#include <llvm/IR/Type.h>
#include "StringHelper.h"
#include <llvm/IR/Verifier.h>
namespace StrOpt {
    struct StringConcatNode;
    struct StringValueNode;

    class StringNode {
    public:
        enum Kind { StringValue, StringConcat };
    private:
        const Kind kind;
    public:

        StringNode(Kind kind) :kind(kind) { }

        bool isa(Kind k)const {
            return k == kind;
        }
        StringConcatNode *asStringConcatNode()const {
            if (isa(StringConcat))
                return (StringConcatNode *)this;
            return nullptr;
        }
        StringValueNode *asStringValueNode()const {
            if (isa(StringValue))
                return (StringValueNode *)this;
            return nullptr;
        }
        virtual bool canBeRemoved()const = 0;
        virtual void print()const = 0;
        virtual void collectLeaves(llvm::SmallVector<StringNode const *, 5> & leaves, llvm::SmallVector<StringConcatNode const *, 3> & strconcatLeaves)const = 0;
    };
    struct StringConcatNode :public StringNode {
        CallInst *Call;
        Function *Callee;
        StringNode *Lhs, *Rhs;

        StringConcatNode(CallInst *Call,
                         Function *Callee,
                         StringNode *Lhs,
                         StringNode *Rhs)
            :StringNode(StringConcat), Call(Call), Callee(Callee), Lhs(Lhs), Rhs(Rhs) {
        }

        // Geerbt über StringNode
        virtual bool canBeRemoved() const override {
            // two uses are from the parent strconcat (extractvalue at 0 and at 1), so allow two, but not more

            bool ret = Call->getNumUses() <= 2;

            return ret;
        }

        // Geerbt über StringNode
        virtual void print() const override {
            llvm::errs() << "ConcNode: ";

            llvm::errs() << *Call << "\n";
        }

        // Geerbt über StringNode
        virtual void collectLeaves(llvm::SmallVector<StringNode const *, 5> & leaves, llvm::SmallVector<StringConcatNode const *, 3> & strconcatLeaves) const override {
            if (!canBeRemoved()) {
                leaves.push_back(this);
                strconcatLeaves.push_back(this);
            }
            else {

                Lhs->collectLeaves(leaves, strconcatLeaves);

                /* if (auto lconc = Lhs->asStringConcatNode()) {
                     if (lconc->canBeRemoved()) {
                         llvm::errs() << "Erase call "<<*lconc->Call<<"... "; llvm::errs().flush();
                         lconc->Call->eraseFromParent();
                         llvm::errs() << "done\n";
                     }
                 }*/

                Rhs->collectLeaves(leaves, strconcatLeaves);
                /* if (auto rconc = Rhs->asStringConcatNode()) {
                     if (rconc->canBeRemoved()) {
                         llvm::errs() << "Erase call"<<*rconc->Call<<"... "; llvm::errs().flush();
                         rconc->Call->eraseFromParent();
                         llvm::errs() << "done\n";
                     }
                 }*/
            }
        }
    };
    struct StringValueNode :public StringNode {
        Value *Val;
        Value *Len;
        StringValueNode(Value *Val, Value *Len)
            :StringNode(StringValue), Val(Val), Len(Len) {
        }

        // Geerbt über StringNode
        virtual bool canBeRemoved() const override {
            // We always preserve leaf nodes
            //llvm::errs() << "Leaf\n";
            llvm::errs().flush();
            return false;
        }

        // Geerbt über StringNode
        virtual void print() const override {
            llvm::errs() << "LeafNode: " << *Val << "\n";
        }

        // Geerbt über StringNode
        virtual void collectLeaves(llvm::SmallVector<StringNode const *, 5> & leaves, llvm::SmallVector<StringConcatNode const *, 3> & strconcatLeaves) const override {
            leaves.push_back(this);
        }
    };

    char StrMultiConcatOptPass::id = 0;

    void ReplaceStringConcat(Function &F, StringConcatNode const *root, const llvm::SmallVector<StringNode const *, 5> & leaves) {

        //llvm::errs() << "ReplaceStringConcat with " << leaves.size() << " leaves\n";
        auto stringTy = StringHelper::CreateStringType(*F.getParent());
        llvm::Function *strmulticoncat = F.getParent()->getFunction("strmulticoncat");
        if (!strmulticoncat) {
            auto voidTy = llvm::Type::getVoidTy(F.getParent()->getContext());
            auto sizeTy = StringHelper::GetSizeType(*F.getParent());
            auto stringTy = StringHelper::CreateStringType(*F.getParent());
            auto stringPtrTy = stringTy->getPointerTo();
            auto fnTy = FunctionType::get(voidTy, { stringPtrTy, sizeTy, stringPtrTy }, false);

            strmulticoncat = llvm::cast<llvm::Function>(F.getParent()->getOrInsertFunction("strmulticoncat", fnTy));
            strmulticoncat->addParamAttr(0, Attribute::ReadOnly);
            strmulticoncat->addParamAttr(0, Attribute::NoCapture);
            strmulticoncat->addParamAttr(2, Attribute::WriteOnly);
            strmulticoncat->addParamAttr(2, Attribute::NoCapture);
        }
        //auto strmulticoncat = F.getParent()->getFunction("strmulticoncat");


        llvm::Value *arrPtr;
        llvm::Value *retPtr;
        llvm::Instruction *ret;
        llvm::Value *sizeVal;
        {
            llvm::IRBuilder<> inserter(&F.getEntryBlock(), F.getEntryBlock().begin());
            arrPtr = inserter.CreateAlloca(stringTy, sizeVal = StringHelper::CreateIntSZ(*F.getParent(), leaves.size()));
            retPtr = inserter.CreateAlloca(stringTy, nullptr);
        }
        {
            llvm::IRBuilder<> irb(root->Call);
            auto zero = StringHelper::CreateInt32(*F.getParent(), 0);
            auto one = StringHelper::CreateInt32(*F.getParent(), 1);

            for (size_t i = 0; i < leaves.size(); ++i) {

                auto strVal = leaves[i]->asStringValueNode() ? leaves[i]->asStringValueNode()->Val : irb.CreateExtractValue(leaves[i]->asStringConcatNode()->Call, { 0 });
                auto strLen = leaves[i]->asStringValueNode() ? leaves[i]->asStringValueNode()->Len : irb.CreateExtractValue(leaves[i]->asStringConcatNode()->Call, { 1 });

                auto iVal = StringHelper::CreateIntSZ(*F.getParent(), i);
                //llvm::errs() << "##" << *arrPtr << "\n";
                auto valGep = irb.CreateGEP(arrPtr, { iVal, zero });
                //llvm::errs() << "##\n";
                auto lenGep = irb.CreateGEP(arrPtr, { iVal, one });
                //llvm::errs() << "##\n";
                irb.CreateStore(strVal, valGep);
                irb.CreateStore(strLen, lenGep);

            }
            irb.CreateCall(strmulticoncat, { arrPtr, sizeVal, retPtr });
            ret = irb.CreateLoad(retPtr);
        }
        BasicBlock::iterator ii(root->Call);
        for (auto &use : root->Call->uses()) {
            use.set(ret);
        }
        //root->Call->eraseFromParent();
        //ReplaceInstWithInst(ii->getParent()->getInstList(), ii, ret);
        //llvm::errs() << "done\n";
        //llvm::verifyFunction(F, &llvm::errs());
    }

    StrMultiConcatOptPass::StrMultiConcatOptPass() :FunctionPass(id) {
    }
    bool StrMultiConcatOptPass::runOnFunction(Function &F) {
        bool ret = false;
        {
            std::unordered_map<Value *, std::unique_ptr<StringNode>> nodes;
            std::unordered_set<StringNode *> roots;

            auto createNode = [&] (CallInst *call, bool left)->StringNode * {
                auto arg = call->getArgOperand(left ? 0 : 2);
                if (auto extract = dyn_cast<ExtractValueInst>(arg)) {
                    if (extract->getNumIndices() == 1 && extract->getIndices()[0] == 0 && nodes.count(extract->getAggregateOperand())) {
                        return nodes[extract->getAggregateOperand()].get();
                    }
                }

                auto find = nodes.find(arg);
                if (find == nodes.end())
                    return (nodes[arg] = std::make_unique<StringValueNode>(arg, call->getArgOperand(left ? 1 : 3))).get();
                else
                    return find->second.get();
            };

            for (auto &bb : F.getBasicBlockList()) {
                for (auto &inst : bb) {
                    if (auto call = dyn_cast<CallInst>(&inst)) {
                        if (!call->getCalledFunction())
                            continue;
                        auto callee = call->getCalledFunction();
                        if (callee->getName() == "strconcat_ret") {
                            auto Lhs = createNode(call, true);
                            auto Rhs = createNode(call, false);
                            roots.erase(Lhs);
                            roots.erase(Rhs);

                            auto nwRoot = (nodes[call] = std::make_unique<StringConcatNode>(call, callee, Lhs, Rhs)).get();
                            roots.insert(nwRoot);
                        }
                    }
                }
            }
            // TODO: handle the roots (check, whether or not the whole subtree can be replaced, or only parts of it) 
            // (replace the subtree from root by an array of the leaves + strmulticoncat call)
            bool ret = false;
            llvm::errs() << "FOUND ROOTS----------------------------------------\n";
            llvm::SmallVector<StringConcatNode const *, 3> strconcatLeaves;
            for (auto &root : roots) {
                if (auto strconc = root->asStringConcatNode()) {

                    if (!strconc || !strconc->Lhs || !strconc->Rhs) {
                        llvm::errs() << "FATAL ERROR: null dereference\n";
                        continue;
                    }
                    if (strconc->Lhs->canBeRemoved() || strconc->Rhs->canBeRemoved()) {
                        // strconc is a candidate for optimization
                        llvm::errs() << "::ROOT: " << *strconc->Call << "\n";
                        llvm::SmallVector<const StringNode *, 5> leaves;
                        strconc->collectLeaves(leaves, strconcatLeaves);
                        ret = true;
                        ReplaceStringConcat(F, strconc, leaves);
                    }
                }
            }
            for (size_t i = 0; i < strconcatLeaves.size(); ++i) {
                auto strconc = strconcatLeaves[i];
                if (!strconc || !strconc->Lhs || !strconc->Rhs) {
                    llvm::errs() << "FATAL ERROR: null dereference\n";
                    continue;
                }
                if (strconc->Lhs->canBeRemoved() || strconc->Rhs->canBeRemoved()) {
                    // strconc is a candidate for optimization
                    llvm::errs() << "::ROOT: " << *strconc->Call << "\n";
                    llvm::SmallVector<const StringNode *, 5> leaves;
                    strconc->collectLeaves(leaves, strconcatLeaves);
                    ReplaceStringConcat(F, strconc, leaves);
                }
            }
            llvm::errs() << "ROOTS END------------------------------------------\n";
        }
        if (ret) {
            llvm::errs() << F;
        }
        return ret;
    }

}