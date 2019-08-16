using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLVMCodeGenerator {
    public readonly struct VirtualMethodTable {

        public IntPtr VTableType {
            get;
        }
        public IntPtr VTablePointer {
            get;
        }
        public uint Count {
            get => VirtualMethods.Length;
        }
        public uint Level {
            get;
        }
        public bool IsVTable { get; }
        public uint TypeIdIndex { get; }
        public uint IsInstIndex { get; }
        public readonly Vector<IntPtr> VirtualMethods;
        public readonly Vector<IntPtr> VirtualMethodTypes;
        public VirtualMethodTable(IntPtr vtableTy, IntPtr vtablePtr, in Vector<IntPtr> virtMets, in Vector<IntPtr> virtMetTys, uint level, uint typeidIdx, uint isinstIdx) {
            IsVTable = true;
            VTableType = vtableTy;
            VTablePointer = vtablePtr;
            VirtualMethods = virtMets;
            VirtualMethodTypes = virtMetTys;
            Level = level;
            TypeIdIndex = typeidIdx;
            IsInstIndex = isinstIdx;
        }
    }
}
