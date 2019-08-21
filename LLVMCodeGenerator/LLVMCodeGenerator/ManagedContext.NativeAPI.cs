/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace LLVMCodeGenerator {
    partial class ManagedContext {
        unsafe class NativeAPI {
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr CreateManagedContext(string name);
            [DllImport("LLVMInterface.dll")]
            public extern static void DisposeManagedContext(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static void Save(IntPtr ctx, string filename);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getStruct(IntPtr ctx, string name);
            [DllImport("LLVMInterface.dll")]
            public extern static void completeStruct(IntPtr ctx, IntPtr strct, IntPtr* body, uint bodyLen);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getUnnamedStruct(IntPtr ctx, IntPtr* body, uint bodyLen);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getBoolType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getByteType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getShortType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getIntType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getLongType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getBiglongType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getFloatType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getDoubleType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getStringType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getArrayType(IntPtr ctx, IntPtr elem, uint count);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getPointerType(IntPtr ctx, IntPtr pointsTo);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getVoidPtr(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr getVoidType(IntPtr ctx);
            [DllImport("LLVMInterface.dll")]
            public extern static IntPtr declareFunction(IntPtr ctx, string name, IntPtr retTy, IntPtr* argTys, uint argc, bool isPublic);
        }

    }
}
