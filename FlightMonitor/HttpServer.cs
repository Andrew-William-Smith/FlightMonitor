using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace FlightMonitor {
    /// <summary>
    /// A simple HTTP server running on port 80 of the local machine, used to serve static content to the web client.
    /// </summary>
    class HttpServer {
        /// <summary>The URI prefix monitored by the server's listener.</summary>
        private const string LISTENER_URI_PREFIX = "http://localhost:8000/";

        /// <summary>Size of the WebSocket buffers used to store and construct messages.</summary>
        private const int WEBSOCKET_BUFFER_SIZE = 8192;

        /// <summary>The SimConnect client through which to marshal simulator data.</summary>
        private readonly SimConnectClient simClient;

        /// <summary>The HTTP listener used to handle requests.</summary>
        private readonly HttpListener listener;

        /// <summary>Cancellation token generator used to stop the listener thread.</summary>
        private CancellationTokenSource cancelSource;

        /// <summary>The number of currently active WebSocket connections.</summary>
        private int webSocketCount;

        /// <summary>
        /// Construct a new HTTP server that marshals data through the specified SimConnect client.
        /// </summary>
        /// <param name="client">The SimConnect client through which to marshal simulator data.</param>
        public HttpServer(SimConnectClient client) {
            simClient = client;
            listener = new HttpListener();
            listener.Prefixes.Add(LISTENER_URI_PREFIX);
        }

        ~HttpServer() {
            Stop();
            listener.Close();
        }

        /// <summary>
        /// Handle an incoming HTTP connection with the specified context.  Intended to be run in a separate thread off
        /// of the main server thread.
        /// </summary>
        /// <param name="context">The HTTP context of the connection.</param>
        private async Task HandleHttpConnection(HttpListenerContext context) {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Attempt to get the contents of the specified file
            string requestedFile = request.RawUrl == "/" ? "/index.html" : request.RawUrl;
            byte[] fileContents;
            try {
                fileContents = File.ReadAllBytes($"./Client{requestedFile}");
                response.ContentType = MimeMapping.GetMimeMapping(requestedFile);
            } catch {
                fileContents = Encoding.UTF8.GetBytes($"Error 404: File {requestedFile} could not be found.");
                response.ContentType = "text/plain";
                response.StatusCode = 404;
            }

            // Send response back to the client
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = fileContents.LongLength;
            try {
                await response.OutputStream.WriteAsync(fileContents, 0, fileContents.Length);
            } catch (HttpListenerException) {
                /* Occasionally, writing to the response's output stream will result in an HttpListenerException
                 * because the browser has closed the channel before HttpListener was expecting it to.  This is
                 * well-documented, harmless, and can be safely ignored. */
            }
            response.Close();
        }

        /// <summary>
        /// Send the specified message via the specified WebSocket.
        /// </summary>
        /// <param name="socket">The socket via which to send the message.</param>
        /// <param name="message">The message to send as an object of an anonymous type.</param>
        /// <param name="cancelToken">The cancellation token used to abort the sending of the message.</param>
        private void SendWebSocketMessage(WebSocket socket, object message, CancellationToken cancelToken) {
            string json = JsonConvert.SerializeObject(message);
            byte[] rawMessage = Encoding.UTF8.GetBytes(json);
            _ = socket.SendAsync(new ArraySegment<byte>(rawMessage, 0, rawMessage.Length), WebSocketMessageType.Text,
                true, cancelToken);
        }

        private void ExecuteAddVariable(WebSocket socket,
                                        Dictionary<string, object> request,
                                        CancellationToken cancelToken) {
            string name = (string)request["variable"];
            ISimVariable variable = simClient.AddVariable(name);
            if (variable != null) {
                // Variable was added successfully, send the client its information
                SendWebSocketMessage(socket, new {
                    type = "DECLARE_VARIABLE",
                    name = variable.Name,
                    id = variable.Id,
                    unit = variable.Unit
                }, cancelToken);
            } else {
                // If this did not succeed, then the user requested a nonexistent variable
                SendWebSocketMessage(socket, new {
                    type = "ERROR",
                    message = $"Cannot monitor unknown variable {variable}."
                }, cancelToken);
            }
        }

        /// <summary>
        /// Process the specified message received from a WebSocket.
        /// </summary>
        /// <param name="socket">The WebSocket from which the message was received.</param>
        /// <param name="message">The message to process.</param>
        /// <param name="cancelToken">The cancellation token used to abort the sending of message responses.</param>
        private void ProcessWebSocketMessage(WebSocket socket, string message, CancellationToken cancelToken) {
            // Deserialise JSON and determine which action to execute
            Dictionary<string, object> content;
            try {
                content = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            } catch (JsonReaderException ex) {
                SendWebSocketMessage(socket, new {
                    type = "ERROR",
                    message = $"Malformed JSON message: {ex.Message}"
                }, cancelToken);
                return;
            }

            // Every message must have a type declared
            if (!content.ContainsKey("type")) {
                SendWebSocketMessage(socket, new {
                    type = "ERROR",
                    message = "Malformed message: must contain a key \"type\"."
                }, cancelToken);
                return;
            }

            string type = (string)content["type"];
            switch (type) {
                case "ADD_VARIABLE":
                    // Start monitoring a variable
                    ExecuteAddVariable(socket, content, cancelToken);
                    break;
                default:
                    // Unknown message type, send an error
                    SendWebSocketMessage(socket, new {
                        type = "ERROR",
                        message = $"Cannot execute message of unknown type {type}."
                    }, cancelToken);
                    break;
            }
        }

        /// <summary>
        /// Handle an incoming WebSocket connection with the specified context.  Intended to be run in a separate thread
        /// off of the main server thread, with one thread per client.
        /// </summary>
        /// <param name="context">The WebSocket context of this connection.</param>
        /// <param name="socketId">A unique identifier for this connection's socket.</param>
        /// <param name="cancelToken">The cancellation token used to abort the sending of message responses.</param>
        private async Task HandleWebSocketConnection(HttpListenerWebSocketContext context,
                                                     int socketId,
                                                     CancellationToken cancelToken) {
            WebSocket socket = context.WebSocket;
            ArraySegment<byte> buffer = WebSocket.CreateServerBuffer(WEBSOCKET_BUFFER_SIZE);

            while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted
                    && !cancelToken.IsCancellationRequested) {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancelToken);

                // If the token is cancelled during result fetch, then the socket will be aborted
                if (!cancelToken.IsCancellationRequested) {
                    if (socket.State == WebSocketState.CloseReceived
                            && result.MessageType == WebSocketMessageType.Close) {
                        // If the client is closing the token, acknowledge
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge closure",
                            CancellationToken.None);
                    } else if (socket.State == WebSocketState.Open) {
                        // Handle client message
                        ProcessWebSocketMessage(socket, Encoding.UTF8.GetString(buffer.Array, 0, result.Count),
                            cancelToken);
                    }
                }
            }

            // Ensure that the socket is not still connected
            if (socket.State != WebSocketState.Closed) {
                socket.Abort();
            }
            socket.Dispose();
        }

        /// <summary>
        /// Handle incoming HTTP and WebSocket connections.  Intended to be run in a separate thread cancellable with
        /// the specified token.
        /// </summary>
        /// <param name="cancelToken">The cancellation token used to stop the thread.</param>
        private async Task HandleConnections(CancellationToken cancelToken) {
            // Run until the cancellation token signals to stop
            while (!cancelToken.IsCancellationRequested) {
                // For HTTP requests, spawn a new HTTP thread to handle the request
                try {
                    HttpListenerContext context = await listener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest) {
                        HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                        int wsId = Interlocked.Increment(ref webSocketCount);
                        _ = Task.Run(() => {
                            return HandleWebSocketConnection(wsContext, wsId, cancelToken).ConfigureAwait(false);
                        }, cancelToken);
                    } else {
                        _ = Task.Run(() => HandleHttpConnection(context).ConfigureAwait(false), cancelToken);
                    }
                } catch (HttpListenerException) {
                    // Safe to ignore
                }
            }
        }

        /// <summary>
        /// Start the HTTP server and run in a separate thread until the <c>Stop</c> method is called.
        /// </summary>
        public void Start() {
            listener.Start();

            // Start a new thread to handle requests
            cancelSource = new CancellationTokenSource();
            _ = Task.Run(() => HandleConnections(cancelSource.Token).ConfigureAwait(false), cancelSource.Token);
        }

        /// <summary>
        /// Stop the HTTP server and dispose of its running tasks.
        /// </summary>
        public void Stop() {
            cancelSource?.Cancel();
            listener.Stop();
        }
    }
}
