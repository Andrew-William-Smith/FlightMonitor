using Microsoft.FlightSimulator.SimConnect;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlightMonitor {
    /// <summary>
    /// Placeholder enum for variable request IDs.  Used only so that we have something to which to cast the
    /// integers by which variables are actually identified.
    /// </summary>
    enum VariableId {
        Dummy
    }

    /// <summary>
    /// An untyped simulator variable, used to allow variables to be stored in a collection.
    /// </summary>
    interface ISimVariable {
        /// <summary>The numeric identifier of this variable as reported by SimConnect.</summary>
        VariableId Id { get; }

        /// <summary>The name of this SimConnect variable.</summary>
        string Name { get; }

        /// <summary>The SimConnect unit in which this variable is measured.</summary>
        string Unit { get; }

        /// <summary>The value of the variable as an untyped object.</summary>
        object Value { get; set; }

        /// <summary>The SimConnect type of the value stored by this variable.</summary>
        SIMCONNECT_DATATYPE SimConnectType { get; }

        /// <summary>
        /// Register this variable with the specified SimConnect connection.
        /// </summary>
        /// <param name="connection">The SimConnect connection with which to regsiter this variable.</param>
        void Register(SimConnect connection);
    }

    /// <summary>
    /// A type-agnostic representation of a SimConnect variable.
    /// </summary>
    /// <typeparam name="T">The .NET type of this variable.</typeparam>
    class SimVariable<T> : INotifyPropertyChanged, ISimVariable where T : ISimValue, new() {
        /// <summary>The value of this variable.</summary>
        public object Value {
            get {
                return simValue.Value;
            }
            set {
                simValue.Value = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>Internal representation of the variable value.</summary>
        private readonly T simValue;

        #region ISimVariable implementation
        // Accessors
        public VariableId Id { get; }
        public string Name { get; }
        public string Unit { get; }
        public SIMCONNECT_DATATYPE SimConnectType => simValue.SimConnectType;

        public void Register(SimConnect connection) {
            // Per P3D documentation, if the variable is a string, then the unit should be left empty
            string unit = simValue.IsString ? "" : Unit;

            // Define a new structure for this variable
            connection.AddToDataDefinition(Id, Name, unit, SimConnectType, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            // Register it with the SimConnect managed marshaller to allow data fetch
            connection.RegisterDataDefineStruct<T>(Id);
        }
        #endregion

        public SimVariable(int id, string name, string unit) {
            Id = (VariableId)id;
            Name = name;
            Unit = unit;
            simValue = new T();
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
