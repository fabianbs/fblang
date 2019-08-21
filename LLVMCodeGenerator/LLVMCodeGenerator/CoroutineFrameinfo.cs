/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure;
using CompilerInfrastructure.Expressions;
using CompilerInfrastructure.Structure.Types;
using System;
using System.Collections.Generic;
using System.Text;
using static LLVMCodeGenerator.InstructionGenerator;

namespace LLVMCodeGenerator {
    public struct CoroutineFrameInfo {
        public uint numSuspendPoints;
        public uint thisInd;
        public uint stateInd;
        public uint mutArgsInd;
        public bool hasMutArgs;
        public FunctionType coroTp;
        public CoroutineInfo.Kind kind;
        public IDictionary<IVariable, uint> localIdx;
        public IDictionary<IExpression, uint> otherIdx;
    }
}
