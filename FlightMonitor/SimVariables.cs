using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace FlightMonitor {
    /// <summary>
    /// Wrapper class for the base variable objects for all SimConnect variables supported by Flight Monitor.
    /// </summary>
    static class SimVariables {
        /// <summary>Name of the file containing the list of SimConnect variables.</summary>
        private const string VARIABLES_FILE_NAME = "FlightMonitor.SimVariables.txt";

        /// <summary>Cache of pre-instantiated variables, indexable by name.</summary>
        public static Dictionary<string, ISimVariable> byName;

        /// <summary>Cache of pre-instantiated variables, indexable by ID.</summary>
        public static List<ISimVariable> byId;

        static SimVariables() {
            byName = new Dictionary<string, ISimVariable>();
            byId = new List<ISimVariable>();

            // Read in variables from file
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(VARIABLES_FILE_NAME)) {
                using (StreamReader reader = new StreamReader(stream)) {
                    int varId = 0;
                    while (!reader.EndOfStream) {
                        // Variable definitions are of the format "name,unit"
                        string[] varComponents = reader.ReadLine().Split(',');
                        string varName = varComponents[0];
                        string varUnit = varComponents[1];

                        // Initialise variable with the type indicated by the unit
                        ISimVariable newVar;
                        switch (varUnit) {
                            case "String8":
                            case "String64":
                            case "Bool":
                                // Special types require generics generated from their units
                                Type genericType = Type.GetType("FlightMonitor.Sim" + varUnit);
                                Type newType = typeof(SimVariable<>).MakeGenericType(genericType);
                                newVar = (ISimVariable) Activator.CreateInstance(newType, varId, varName, varUnit);
                                break;
                            default:
                                // Treat all other units as float
                                newVar = new SimVariable<SimDouble>(varId, varName, varUnit);
                                break;
                        }

                        // Add variable to both indices
                        byName.Add(varName, newVar);
                        byId.Add(newVar);
                        varId++;
                    }
                }
            }
        }
    }
}
