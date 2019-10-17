using CompilerInfrastructure.Instructions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Analysis.TaintAnalysis {
    public readonly struct TaintSource {
        readonly IVariable srcVar;
        readonly IDeclaredMethod srcMet;
        readonly IStatement srcStmt;
        public TaintSource(IVariable _srcVar) {
            srcVar = _srcVar;
            srcMet = null;
            srcStmt = null;
        }
        public TaintSource(IDeclaredMethod _srcMet) {
            srcVar = null;
            srcMet = _srcMet;
            srcStmt = null;
        }
        public TaintSource(IStatement _srcStmt) {
            srcVar = null;
            srcMet = null;
            srcStmt = _srcStmt;
        }
        public bool IsSourceMethod => srcMet != null;
        public bool IsSourceVariable => srcVar != null;
        public bool IsSourceStatement => srcStmt != null;
        public IVariable SourceVariable => srcVar;
        public IDeclaredMethod SourceMethod => srcMet;
        public IStatement SourceStatement => srcStmt;

        public bool TryGetSourceVariable(out IVariable vr) {
            if (IsSourceVariable) {
                vr = srcVar;
                return true;
            }
            vr = default;
            return false;
        }
        public bool TryGetSourceMethod(out IDeclaredMethod met) {
            if (IsSourceMethod) {
                met = srcMet;
                return true;
            }
            met = default;
            return false;
        }
        public bool TryGetSourceStatement(out IStatement stmt) {
            if (IsSourceStatement) {
                stmt = srcStmt;
                return true;
            }
            stmt = default;
            return false;
        }
    }
}
