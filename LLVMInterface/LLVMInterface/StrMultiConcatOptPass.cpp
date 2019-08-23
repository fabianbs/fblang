#include "StrMultiConcatOptPass.h"
#include <memory>
#include <unordered_map>
#include <unordered_set>
namespace {
    struct StringConcatNode;
    struct StringValueNode;

    class StringNode {
    public:
        enum Kind { StringValue, StringConcat };
    private:
        const Kind kind;
    public:

        StringNode(Kind kind) :kind(kind) { }

        bool isa(Kind k) {
            return k == kind;
        }
        StringConcatNode *asStringConcatNode() {
            if (isa(StringConcat))
                return (StringConcatNode *)this;
            return nullptr;
        }
        StringValueNode *asStringValueNode() {
            if (isa(StringValue))
                return (StringValueNode *)this;
            return nullptr;
        }
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
    };
    struct StringValueNode :public StringNode {
        Value *Val;
        Value *Len;
        StringValueNode(Value *Val, Value *Len)
            :StringNode(StringValue), Val(Val), Len(Len) {
        }
    };

    char StrMultiConcatOptPass::id = 0;
    StrMultiConcatOptPass::StrMultiConcatOptPass() :FunctionPass(id) {
    }
    bool StrMultiConcatOptPass::runOnFunction(Function &F) {
        std::unordered_map<Value *, std::unique_ptr<StringNode>> nodes;
        std::unordered_set<StringNode *> roots;

        auto createNode = [&] (CallInst *call, bool left)->StringNode * {
            auto arg = call->getArgOperand(left ? 0 : 2);
            if (auto extract = dyn_cast<ExtractValueInst>(arg)) {
                if (extract->getNumIndices() == 1 && extract->getIndices()[0] == 0 && nodes.count(extract->getAggregateOperand())) {
                    return nodes[extract->getAggregateOperand()].get();
                }
            }
            return (nodes[arg] = std::make_unique<StringValueNode>(arg, call->getArgOperand(left ? 1 : 3))).get();
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
        return true;
    }

}