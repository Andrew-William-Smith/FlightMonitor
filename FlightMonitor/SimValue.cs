using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;

namespace FlightMonitor {
    /// <summary>
    /// A wrapped value usable for interoperation with SimConnect.
    /// </summary>
    interface ISimValue {
        /// <summary>The current value of the type.</summary>
        object Value { get; set; }

        /// <summary>Whether this type represents a string.</summary>
        bool IsString { get; }

        /// <summary>The SimConnect type of this type.</summary>
        SIMCONNECT_DATATYPE SimConnectType { get; }
    }

    /// <summary>
    /// A double-precision floating-point value defined as a SimConnect structure, equivalent to the type
    /// <c>FLOAT64</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimDouble : ISimValue {
        private double _value;
        public bool IsString => false;
        public SIMCONNECT_DATATYPE SimConnectType => SIMCONNECT_DATATYPE.FLOAT64;

        public object Value {
            get => _value;
            set => _value = ((SimDouble)value)._value;
        }
    }

    /// <summary>
    /// A boolean value defined as a SimConnect structure, equivalent to the type <c>INT32</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimBool : ISimValue {
        private int _value;
        public bool IsString => false;
        public SIMCONNECT_DATATYPE SimConnectType => SIMCONNECT_DATATYPE.INT32;

        public object Value {
            get => _value != 0;
            set => _value = ((SimBool)value)._value;
        }
    }

    /// <summary>
    /// An 8-character defined as a SimConnect structure, equivalent to the type <c>STRING8</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimString8 : ISimValue {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        private string _value;
        public bool IsString => true;
        public SIMCONNECT_DATATYPE SimConnectType => SIMCONNECT_DATATYPE.STRING8;

        public object Value {
            get => _value;
            set => _value = ((SimString8)value)._value;
        }
    }

    /// <summary>
    /// A 64-character defined as a SimConnect structure, equivalent to the type <c>STRING64</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct SimString64 : ISimValue {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        private string _value;
        public bool IsString => true;
        public SIMCONNECT_DATATYPE SimConnectType => SIMCONNECT_DATATYPE.STRING64;

        public object Value {
            get => _value;
            set => _value = ((SimString64)value)._value;
        }
    }
}
