using System.IO;
using System.Net;
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

        /// <summary>The HTTP listener used to handle requests.</summary>
        private readonly HttpListener listener;

        /// <summary>Cancellation token generator used to stop the listener thread.</summary>
        private CancellationTokenSource cancelSource;

        public HttpServer() {
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
        private void HandleHttpConnection(HttpListenerContext context) {
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
                response.OutputStream.Write(fileContents, 0, fileContents.Length);
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
        private void HandleConnections(CancellationToken cancelToken) {
            // Run until the cancellation token signals to stop
            while (!cancelToken.IsCancellationRequested) {
                // For HTTP requests, spawn a new HTTP thread to handle the request
                try {
                    HttpListenerContext context = listener.GetContext();
                    _ = Task.Run(() => {
                        try {
                            HandleHttpConnection(context);
                        } catch (HttpListenerException) {
                            /* This exception is almost definitely a result of the server being terminated, so there's no
                             * harm in ignoring it since we're going down anyway. */
                        }
                    }, cancelToken);
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
            _ = Task.Run(() => HandleConnections(cancelSource.Token), cancelSource.Token);
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
