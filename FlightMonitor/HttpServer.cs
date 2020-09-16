using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Data;

namespace FlightMonitor {
    /// <summary>
    /// A simple HTTP server running on port 80 of the local machine, used to serve static content to the web client.
    /// </summary>
    public class HttpServer {
        /// <summary>The URI prefix monitored by the server's listener.</summary>
        private const string LISTENER_URI_PREFIX = "http://+:8000/";

        /// <summary>The currently-active WebSocket sessions being handled by this server.</summary>
        public ObservableCollection<WebSocketSession> WebSocketSessions { get; }

        /// <summary>The SimConnect client through which to marshal simulator data.</summary>
        private readonly SimConnectClient simClient;

        /// <summary>The HTTP listener used to handle requests.</summary>
        private readonly HttpListener listener;

        /// <summary>Cancellation token generator used to stop the listener thread.</summary>
        private CancellationTokenSource cancelSource;

        /// <summary>The global lock used to synchronise access between this client and the main thread.</summary>
        private readonly object globalLock;

        /// <summary>
        /// Construct a new HTTP server that marshals data through the specified SimConnect client.
        /// </summary>
        /// <param name="client">The SimConnect client through which to marshal simulator data.</param>
        /// <param name="globalLock">The lock used to synchronise observable operations performed by the server.</param>
        public HttpServer(SimConnectClient client, object globalLock) {
            simClient = client;
            listener = new HttpListener();
            listener.Prefixes.Add(LISTENER_URI_PREFIX);

            // Initialise WebSocket session store
            this.globalLock = globalLock;
            WebSocketSessions = new ObservableCollection<WebSocketSession>();
            BindingOperations.EnableCollectionSynchronization(WebSocketSessions, globalLock);
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
                if (requestedFile.EndsWith(".svg")) {
                    // SVG is not in the default MIME registry and must be handled separately
                    response.ContentType = "image/svg+xml";
                } else {
                    response.ContentType = MimeMapping.GetMimeMapping(requestedFile);
                }
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
                        _ = Task.Run(() => {
                            WebSocketSession session = new WebSocketSession(wsContext, simClient, cancelToken);
                            lock (globalLock) {
                                WebSocketSessions.Add(session);
                            }
                            // Once the socket has closed, we should remove the session from the list
                            session.SocketClosed += (sender, evt) => {
                                lock (globalLock) {
                                    _ = WebSocketSessions.Remove(session);
                                }
                            };
                            return session.Run().ConfigureAwait(false);
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
        /// Timer event to broadcast the variables currently being monitored to all WebSocket sessions.
        /// </summary>
        private void BroadcastSimulatorState(object sender, EventArgs e) {
            // Generate a minimal JSON representation of the variables
            string state = JsonConvert.SerializeObject(new {
                type = "STATE_SNAPSHOT",
                state = simClient.Variables
                                 .Select(variable => new {
                                     id = (int)variable.Id,
                                     value = variable.Value
                                 }).ToDictionary(item => item.id, item => item.value)
            });

            // Send the state packet to all active sessions
            foreach (WebSocketSession session in WebSocketSessions) {
                session.SendMessage(state);
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
            // Register the broadcast task with the simulator client
            simClient.AddDataResponse(new EventHandler(BroadcastSimulatorState));
        }

        /// <summary>
        /// Stop the HTTP server and dispose of its running tasks.
        /// </summary>
        public void Stop() {
            cancelSource?.Cancel();
            listener.Stop();
            WebSocketSessions.Clear();
        }
    }
}
