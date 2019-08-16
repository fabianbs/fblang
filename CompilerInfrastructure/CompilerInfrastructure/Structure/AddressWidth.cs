using System;
using System.Collections.Generic;
using System.Text;

namespace CompilerInfrastructure.Structure {
    /// <summary>
    /// Specifies the address-width (or pointer-size)
    /// </summary>
    [Serializable]
    public enum AddressWidth {
        /// <summary>
        /// Unknown address width
        /// </summary>
        Unspecified,
        /// <summary>
        /// 32 Bit
        /// </summary>
        _32,
        /// <summary>
        /// 64 Bit
        /// </summary>
        _64
    }
    /// <summary>
    /// Helper class for retrieving default address-widths
    /// </summary>
    public static class Address {
        /// <summary>
        /// The address-width for the current process
        /// </summary>
        public static AddressWidth CurrentProcessAddressWidth => Environment.Is64BitProcess ? AddressWidth._64 : AddressWidth._32;
        /// <summary>
        /// The address-width for the current operating system
        /// </summary>
        public static AddressWidth CurrentOSAddressWidth => Environment.Is64BitOperatingSystem ? AddressWidth._64 : AddressWidth._32;
    }
}
