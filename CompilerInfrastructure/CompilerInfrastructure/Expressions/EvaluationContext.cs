using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompilerInfrastructure.Expressions {
    public struct EvaluationContext {
        Dictionary<IExpression, ILiteral> facts;
        MultiMap<IType,IType> isTypeFacts;
        IDictionary<IExpression, ILiteral> Facts {
            get {
                if (facts is null) {
                    facts = new Dictionary<IExpression, ILiteral>();
                }
                return facts;
            }
        }
        MultiMap<IType, IType> IsTypeFacts {
            get {
                if (isTypeFacts is null)
                    isTypeFacts = new MultiMap<IType, IType>();
                return isTypeFacts;
            }
        }
        public EvaluationContext Clone() {
            var ret = default(EvaluationContext);
            if (facts != null)
                ret.facts = new Dictionary<IExpression, ILiteral>(facts);
            if (isTypeFacts != null)
                ret.isTypeFacts = new MultiMap<IType, IType>(isTypeFacts);
            return ret;
        }
        public bool TryGetFact(IExpression ex, out ILiteral value) => Facts.TryGetValue(ex, out value);
        public bool TryGetIsTypeFact(IType sub, IType sup) {
            if (IsTypeFacts.TryGetValue(sub, out var set)) {
                if (set.Contains(sup))
                    return true;
                var setValues = set.ToArray();
                foreach (var superTy in setValues) {
                    if (superTy.IsSubTypeOf(sup)) {
                        isTypeFacts.Add(sub, sup);
                        return true;
                    }
                }
                foreach (var superTy in setValues) {
                    if (TryGetIsTypeFact(superTy, sup)) {
                        isTypeFacts.Add(sub, sup);
                        return true;
                    }
                }
            }
            return false;
        }

        public ILiteral AssertFact(IExpression ex, ILiteral value) {
            Facts[ex] = value;
            return value;
        }
        public void AssertIsTypeFact(IType subTy, IType superTy) {
            IsTypeFacts.Add(subTy, superTy);
        }
        public ILiteral RetractFact(IExpression ex) {
            if (Facts.TryGetValue(ex, out var ret)) {
                facts.Remove(ex);
                return ret;
            }
            return null;
        }
        public ISet<IType> RetractIsTypeFact(IType subTy) {
            if (IsTypeFacts.TryGetValue(subTy, out var ret)) {
                isTypeFacts.Remove(subTy);
                return ret;
            }
            return Set.Empty<IType>();
        }
        public bool RetractIsTypeFact(IType subTy, IType superTy) {
            return IsTypeFacts.Remove(subTy, superTy);
        }

        public static EvaluationContext Intersect(EvaluationContext ctx1, EvaluationContext ctx2) {
            var ret = default(EvaluationContext);
            foreach (var kvp in ctx1.Facts) {
                if (ctx2.Facts.Contains(kvp))
                    ret.Facts.Add(kvp);
            }
            foreach (var kvp in ctx1.IsTypeFacts) {
                foreach (var sup in kvp.Value) {
                    if (ctx2.TryGetIsTypeFact(kvp.Key, sup))
                        ret.IsTypeFacts.Add(kvp.Key, kvp.Value);
                }
            }
            return ret;
        }
    }
}