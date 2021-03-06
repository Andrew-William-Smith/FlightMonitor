﻿using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace FlightMonitor {
    /// <summary>
    /// A client for the Flight Simulator SimConnect API, provided by default with the Flight Simulator 2020 SDK.
    /// Handles all communication with a running instance of the game, providing data to the server and application.
    /// </summary>
    public class SimConnectClient {
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

        /// <summary>The frequency (in milliseconds) at which the timer will tick.</summary>
        public const int TIMER_TICK_RATE = 100;

        /// <summary>The window handle on whose event loop SimConnect is running.</summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;

        /// <summary>The messages emitted during SimConnect interaction.</summary>
        public ObservableCollection<SimConnectMessage> Messages { get; }

        /// <summary>The simulator variables currently being handled by this client.</summary>
        public ObservableCollection<ISimVariable> Variables { get; private set; }

        /// <summary>SimConnect handle for interacting with an instance of the simulator.</summary>
        private SimConnect connection;

        /// <summary>Whether this client is currently connected to the simulator.</summary>
        public bool Connected => connection != null;

        /// <summary>Timer used to limit the rate of SimConnect reads.</summary>
        private DispatcherTimer timer;

        /// <summary>The global lock used to synchronise access between this client and the main thread.</summary>
        private readonly object globalLock;

        public SimConnectClient(object globalLock) {
            Messages = new ObservableCollection<SimConnectMessage>();
            Variables = new ObservableCollection<ISimVariable>();

            // Synchronise both observable collections
            this.globalLock = globalLock;
            BindingOperations.EnableCollectionSynchronization(Messages, globalLock);
            BindingOperations.EnableCollectionSynchronization(Variables, globalLock);
        }

        private void TimerTick(object sender, EventArgs e) {
            // Fetch data for all variables
            lock (globalLock) {
                foreach (ISimVariable v in Variables) {
                    connection?.RequestDataOnSimObjectType(v.Id, v.Id, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                }
            }
        }

        /// <summary>
        /// Register the specified event handler to run whenever new simulator data becomes available for processing.
        /// Note that handlers are cleared when the client is connected, so this method should only be called on 
        /// </summary>
        /// <param name="handler">The event handler to run once data is available.</param>
        public void AddDataResponse(EventHandler handler) {
            timer.Tick += handler;
        }

        /// <summary>
        /// Add the specified message to the message log, to be displayed to the user.
        /// </summary>
        /// <param name="status">The status (urgency) of the message.</param>
        /// <param name="text">The descriptive text of the message.</param>
        private void LogMessage(SimConnectMessage.MessageStatus status, string text) {
            Messages.Add(new SimConnectMessage {
                Status = status,
                Text = text
            });
        }

        private void SimConnect_RecvOpenHandler(SimConnect sender, SIMCONNECT_RECV_OPEN data) {
            // Show a message in the simulator to show that the connection succeeded
            sender.Text(SIMCONNECT_TEXT_TYPE.PRINT_WHITE, TEXT_DURATION, DummyEnum.Dummy,
                "Flight Monitor is connected to this simulator instance.");
            LogMessage(SimConnectMessage.MessageStatus.Information,
                "Received SimConnect open message: displaying notification.");

            // Register all variables with SimConnect
            foreach (ISimVariable v in Variables) {
                v.Register(sender);
            }

            // Now that variables are registered, we can start the timer
            timer.Start();
        }

        private void SimConnect_RecvQuitHandler(SimConnect sender, SIMCONNECT_RECV data) {
            // Simulator has closed: disconnect SimConnect and show a notification
            LogMessage(SimConnectMessage.MessageStatus.Warning, "Received SimConnect quit message: disconnecting.");
            Disconnect();
        }

        private void SimConnect_RecvExceptionHandler(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) {
            SIMCONNECT_EXCEPTION ex = (SIMCONNECT_EXCEPTION)data.dwException;
            LogMessage(SimConnectMessage.MessageStatus.Warning, $"Received SimConnect exception: {ex}");
        }

        private void SimConnect_RecvSimobjectDataBytypeHandler(SimConnect sender,
                                                               SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data) {
            // Set the value of the variable indicated in the request
            int requestId = (int)data.dwRequestID;
            SimVariables.byId[requestId].Value = data.dwData[0];
        }

        /// <summary>
        /// Connect this instance to the active simulator's SimConnect handle.
        /// </summary>
        /// <returns>Whether the connection was established successfully.</returns>
        public bool Connect() {
            if (connection != null) {
                // If we already have a connection, there is no need to re-establish one
                LogMessage(SimConnectMessage.MessageStatus.Warning,
                    "Attempted to establish simultaneous SimConnect connections.  Retaining existing connection.");
                return true;
            }

            // If we are establishing a new connection, clear state from the previous one
            Messages.Clear();
            Variables.Clear();

            // Initialise a new timer with the correct tick rate
            timer = new DispatcherTimer {
                Interval = new TimeSpan(0, 0, 0, 0, TIMER_TICK_RATE)
            };
            timer.Tick += new EventHandler(TimerTick);

            try {
                // Establish a connection to the simulator on the current window's event loop
                connection = new SimConnect("Flight Monitor", WindowHandle, WM_USER_SIMCONNECT, null,
                    SimConnect.SIMCONNECT_OPEN_CONFIGINDEX_LOCAL);

                // Add connection event handlers
                connection.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_RecvOpenHandler);
                connection.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_RecvQuitHandler);
                connection.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_RecvExceptionHandler);
                connection.OnRecvSimobjectDataBytype +=
                    new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_RecvSimobjectDataBytypeHandler);

                // Inform the user that the connection was successful
                LogMessage(SimConnectMessage.MessageStatus.Information, "Connected to Flight Simulator.");
                return true;
            } catch (COMException ex) {
                LogMessage(SimConnectMessage.MessageStatus.Error, $"Error occurred while connecting: {ex.Message}");
                _ = MessageBox.Show($"An error occurred while connecting to Flight Simulator:\r\n{ex.Message}",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Disconnect this instance from the active simulator's SimConnect handle.
        /// </summary>
        public void Disconnect() {
            LogMessage(SimConnectMessage.MessageStatus.Information, "Disconnecting from Flight Simulator.");
            timer.Stop();
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

        /// <summary>
        /// Begin monitoring the variable with the specified name using this client.
        /// </summary>
        /// <param name="name">The name of the variable to monitor.</param>
        /// <returns>The variable being monitored if it was added successfully, or <c>null</c> otherwise.</returns>
        public ISimVariable AddVariable(string name) {
            if (SimVariables.byName.ContainsKey(name)) {
                // Do not re-add the variable if it was already added by another client
                ISimVariable addedVariable = SimVariables.byName[name];
                if (!Variables.Contains(addedVariable)) {
                    if (connection != null) {
                        addedVariable.Register(connection);
                    }
                    lock (globalLock) {
                        Variables.Add(addedVariable);
                        LogMessage(SimConnectMessage.MessageStatus.Information, $"Started monitoring variable {name}.");
                    }
                }
                return addedVariable;
            } else {
                LogMessage(SimConnectMessage.MessageStatus.Warning, $"Cannot monitor nonexistent variable {name}.");
                return null;
            }
        }

        /// <summary>
        /// A message sent from the SimConnect client, for display in the Flight Monitor window.
        /// </summary>
        public class SimConnectMessage {
            public enum MessageStatus {
                Information,
                Warning,
                Error
            }

            /// <summary>The status (urgency) with which the message was issued.</summary>
            public MessageStatus Status { get; set; }

            /// <summary>The user-facing status of the message.</summary>
            public string StatusText {
                get {
                    switch (Status) {
                        case MessageStatus.Information:
                            return "INFO";
                        case MessageStatus.Warning:
                            return "WARN";
                        case MessageStatus.Error:
                            return "ERR";
                        default:
                            return "????";
                    }
                }
            }

            /// <summary>The user-facing text of the message.</summary>
            public string Text { get; set; }
        }
    }
}
