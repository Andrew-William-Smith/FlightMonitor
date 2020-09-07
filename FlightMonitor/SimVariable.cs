using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FlightMonitor {
    /// <summary>
    /// An untyped simulator variable, used to allow variables to be stored in a collection.
    /// </summary>
    interface ISimVariable {
        /// <summary>The name of this SimConnect variable.</summary>
        string Name { get; }

        /// <summary>The SimConnect unit in which this variable is measured.</summary>
        string Unit { get; }

        /// <summary>The value of the variable as an untyped object.</summary>
        object UntypedValue { get; }
    }

    /// <summary>
    /// A type-agnostic representation of a SimConnect variable.
    /// </summary>
    /// <typeparam name="T">The .NET type of this variable.</typeparam>
    class SimVariable<T> : INotifyPropertyChanged, ISimVariable {
        /// <summary>
        /// Placeholder enum for variable request IDs.  Used only so that we have something to which to cast the
        /// integers by which variables are actually identified.
        /// </summary>
        public enum VariableId {
            Dummy
        }

        /// <summary>The numeric identifier of this variable as reported by SimConnect.</summary>
        public VariableId Id { get; }

        /// <summary>The value of this variable.</summary>
        public T Value {
            get {
                return simValue;
            }
            set {
                simValue = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>Internal representation of the variable value.</summary>
        private T simValue;

        #region ISimVariable implementation
        public string Name { get; }
        public string Unit { get; }
        public object UntypedValue => simValue;
        #endregion

        public SimVariable(int id, string name, string unit) {
            Id = (VariableId)id;
            Name = name;
            Unit = unit;
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
