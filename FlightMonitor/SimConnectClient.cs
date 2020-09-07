using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace FlightMonitor {
    /// <summary>
    /// A client for the Flight Simulator SimConnect API, provided by default with the Flight Simulator 2020 SDK.
    /// Handles all communication with a running instance of the game, providing data to the server and application.
    /// </summary>
    class SimConnectClient {
        /// <summary>
        /// A placeholder for SimConnect enum types that don't accept integers.
        /// </summary>
        private enum DummyEnum {
            Dummy
        }

        /// <summary>Win32 event for SimConnect tasks.</summary>
        public const uint WM_USER_SIMCONNECT = 0x0402;

        /// <summary>The duration in seconds for which text messages should be shown in the simulator.</summary>
        private const float TEXT_DURATION = 3.0f;

        /// <summary>The window handle on whose event loop SimConnect is running.</summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>SimConnect handle for interacting with an instance of the simulator.</summary>
        private SimConnect connection;

        /// <summary>Whether this client is currently connected to the simulator.</summary>
        public bool Connected => connection != null;

        private void SimConnect_RecvOpenHandler(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            // Show a message in the simulator to show that the connection succeeded
            sender.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, TEXT_DURATION, DummyEnum.Dummy,
                "Flight Monitor is connected to this simulator instance.");
        }

        /// <summary>
        /// Connect this instance to the active simulator's SimConnect handle.
        /// </summary>
        /// <returns>Whether the connection was established successfully.</returns>
        public bool Connect() {
            if (connection != null) {
                // If we already have a connection, there is no need to re-establish one
                return true;
            }

            try {
                // Establish a connection to the simulator on the current window's event loop
                connection = new SimConnect("Flight Monitor", WindowHandle, WM_USER_SIMCONNECT, null,
                    SimConnect.SIMCONNECT_OPEN_CONFIGINDEX_LOCAL);

                // Add connection event handlers
                connection.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_RecvOpenHandler);
                return true;
            } catch (COMException ex) {
                _ = MessageBox.Show($"An error occurred while connecting to Flight Simulator:\r\n{ex.Message}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Disconnect this instance from the active simulator's SimConnect handle.
        /// </summary>
        public void Disconnect() {
            connection.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, TEXT_DURATION, DummyEnum.Dummy,
                "Flight Monitor disconnected!");
            connection.Dispose();
            connection = null;
        }

        /// <summary>
        /// Receive a message from the SimConnect API.  Should only be called from the Win32 message loop.
        /// </summary>
        public void ReceiveMessage() {
            connection?.ReceiveMessage();
        }
    }
}
