/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

namespace LLVMCodeGenerator {
    unsafe partial class ManagedContext : IDisposable {
        IntPtr ctx;
        public ManagedContext(string name) {
            ctx = NativeAPI.CreateManagedContext(name);
        }
        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    //  verwalteten Zustand (verwaltete Objekte) entsorgen.
                }

                NativeAPI.DisposeManagedContext(ctx);

                disposedValue = true;
            }
        }


        ~ManagedContext() {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(false);
        }

        // Dieser Code wird hinzugefügt, um das Dispose-Muster richtig zu implementieren.
        public void Dispose() {
            // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in Dispose(bool disposing) weiter oben ein.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        //TODO make api available
        public void Save(string filename) {
            NativeAPI.Save(ctx, filename);
        }
        public IntPtr GetStruct(string name) {
            return NativeAPI.getStruct(ctx, name);
        }
        public void CompleteStruct(IntPtr strct, params IntPtr[] body) {
            fixed (IntPtr* bodyptr = body) {
                NativeAPI.completeStruct(ctx, strct, bodyptr, (uint)body.Length);
            }
        }
        public IntPtr GetUnnamedStruct(params IntPtr[] body) {
            fixed (IntPtr* bodyptr = body) {
                return NativeAPI.getUnnamedStruct(ctx, bodyptr, (uint)body.Length);
            }
        }
        public IntPtr GetBoolType() {
            return NativeAPI.getBoolType(ctx);
        }
        public IntPtr GetByteType() {
            return NativeAPI.getByteType(ctx);
        }
        public IntPtr GetShortType() {
            return NativeAPI.getShortType(ctx);
        }
        public IntPtr GetIntType() {
            return NativeAPI.getIntType(ctx);
        }
        public IntPtr GetLongType() {
            return NativeAPI.getLongType(ctx);
        }
        public IntPtr GetBiglongType() {
            return NativeAPI.getBiglongType(ctx);
        }
        public IntPtr GetFloatType() {
            return NativeAPI.getFloatType(ctx);
        }
        public IntPtr GetDoubleType() {
            return NativeAPI.getDoubleType(ctx);
        }
        public IntPtr GetStringType() {
            return NativeAPI.getStringType(ctx);
        }
        public IntPtr GetArrayType(IntPtr elem, uint count) {
            return NativeAPI.getArrayType(ctx, elem, count);
        }
        public IntPtr GetPointerType(IntPtr pointsTo) {
            return NativeAPI.getPointerType(ctx, pointsTo);
        }
        public IntPtr GetVoidPtrType() {
            return NativeAPI.getVoidPtr(ctx);
        }
        public IntPtr GetVoidType() {
            return NativeAPI.getVoidType(ctx);
        }
        public IntPtr DeclareFunction(string name, IntPtr retTy, IntPtr[] argTys, bool isPublic) {
            fixed (IntPtr* argptr = argTys) {
                return NativeAPI.declareFunction(ctx, name, retTy, argptr, (uint)argTys.Length, isPublic);
            }
        }
    }
}
