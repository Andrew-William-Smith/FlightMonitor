using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlightMonitor {
    /// <summary>
    /// The state of a single running WebSocket session and some facilities for interacting with it.  Functionally, this
    /// class is an implementation of the Flight Monitor WebSocket protocol.
    /// </summary>
    public class WebSocketSession {
        /// <summary>Size of the WebSocket buffers used to load client messages.</summary>
        private const int WEBSOCKET_BUFFER_SIZE = 8192;

        /// <summary>The WebSocket through which all communication with clients will be performed.</summary>
        private readonly WebSocket socket;

        /// <summary>The cancellation token that may be used to externally terminate this session.</summary>
        private readonly CancellationToken cancelToken;

        /// <summary>The SimConnect client through which to interact with the simulator.</summary>
        private readonly SimConnectClient simClient;

        /// <summary>Event executed once the socket connection has been closed.</summary>
        public event EventHandler SocketClosed;

        /// <summary>
        /// Initialise a new WebSocket connection with the specified context.
        /// </summary>
        /// <param name="context">The WebSocket context of this connection.</param>
        /// <param name="simClient">The client through which this session can interact with the simulator.</param>
        /// <param name="cancelToken">The cancellation token used to abort asynchronous WebSocket actions.</param>
        public WebSocketSession(HttpListenerWebSocketContext context,
                                SimConnectClient simClient,
                                CancellationToken cancelToken) {
            socket = context.WebSocket;
            this.simClient = simClient;
            this.cancelToken = cancelToken;
        }

        public void OnSocketClosed() {
            SocketClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Send the specified message via this WebSocket.
        /// </summary>
        /// <param name="message">The message to send as an object of an anonymous type.</param>
        public void SendMessage(object message) {
            string json = JsonConvert.SerializeObject(message);
            byte[] rawJson = Encoding.UTF8.GetBytes(json);
            _ = socket.SendAsync(new ArraySegment<byte>(rawJson, 0, rawJson.Length), WebSocketMessageType.Text, true,
                cancelToken);
        }

        /// <summary>
        /// Send an error message (type <c>ERROR</c>) with the specified description via this WebSocket.
        /// </summary>
        /// <param name="description">A description of the error that occurred.</param>
        public void SendErrorMessage(string description) {
            SendMessage(new { type = "ERROR", description });
        }

        /// <summary>Execute a message of type <c>ADD_VARIABLE</c>.</summary>
        private void ExecuteAddVariable(Dictionary<string, object> request) {
            string name = (string)request["variable"];
            ISimVariable variable = simClient.AddVariable(name);
            if (variable != null) {
                // Variable was added successfully, send the client its information
                SendMessage(new {
                    type = "DECLARE_VARIABLE",
                    name = variable.Name,
                    id = variable.Id,
                    unit = variable.Unit
                });
            } else {
                // The user requested a nonexistent variable
                SendErrorMessage($"Cannot monitor unknown variable ${name}");
            }
        }

        /// <summary>
        /// Handle a JSON message sent by the client.
        /// </summary>
        /// <param name="message">The message string to process.  Must be a valid JSON object.</param>
        private void HandleMessage(string message) {
            // Attempt to deserialise JSON
            Dictionary<string, object> content;
            try {
                content = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            } catch (JsonReaderException ex) {
                SendErrorMessage($"Malformed JSON message: {ex.Message}");
                return;
            }

            // Every message must have a type declared
            if (!content.ContainsKey("type")) {
                SendErrorMessage("Malformed message: must contain a key \"type\".");
                return;
            }

            // Handle the message depending on its type
            string messageType = (string)content["type"];
            switch (messageType) {
                case "ADD_VARIABLE":
                    // Start monitoring a variable
                    ExecuteAddVariable(content);
                    break;
                default:
                    // Unknown message type: report an error
                    SendErrorMessage($"Malformed message: invalid type {messageType}");
                    break;
            }
        }

        /// <summary>
        /// Run the processing loop for this session.  Intended to be run in a separate thread off of the main WebSocket
        /// server thread.
        /// </summary>
        public async Task Run() {
            ArraySegment<byte> buffer = WebSocket.CreateServerBuffer(WEBSOCKET_BUFFER_SIZE);

            while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted
                    && !cancelToken.IsCancellationRequested) {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancelToken);

                // If the token is cancelled during result fetch, then the socket will be unusable
                if (!cancelToken.IsCancellationRequested) {
                    if (socket.State == WebSocketState.CloseReceived
                            && result.MessageType == WebSocketMessageType.Close) {
                        // If the client is closing the session, acknowledge
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge session closure",
                            CancellationToken.None);
                    } else if (socket.State == WebSocketState.Open) {
                        // Handle client message as a string
                        HandleMessage(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
                    }
                }
            }

            // Ensure that the socket is not still connected
            if (socket.State != WebSocketState.Closed) {
                socket.Abort();
            }
            socket.Dispose();
            OnSocketClosed();
        }
    }
}
