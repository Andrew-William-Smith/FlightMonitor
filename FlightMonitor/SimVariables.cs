using System.Collections.Generic;

namespace FlightMonitor {
    /// <summary>
    /// Wrapper class for the base variable objects for all SimConnect variables supported by Flight Monitor.
    /// </summary>
    static class SimVariables {
        /// <summary>Cache of pre-instantiated variables.</summary>
        public static Dictionary<string, ISimVariable> vars;

        static SimVariables() {
            // TODO: Instantiate from file
            vars = new Dictionary<string, ISimVariable> {
                ["INDICATED ALTITUDE"] = new SimVariable<double>(0, "INDICATED ALTITUDE", "feet")
            };
        }
    }
}
