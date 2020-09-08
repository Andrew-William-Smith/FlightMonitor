using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
        /// Handle incoming HTTP connections.  Intended to be run in a separate thread cancellable with the specified
        /// token.
        /// </summary>
        /// <param name="cancelToken">The cancellation token used to stop the thread.</param>
        private void HandleConnections(CancellationToken cancelToken) {
            bool keepRunning = true;
            while (keepRunning) {
                if (cancelToken.IsCancellationRequested) {
                    // If the thread has been cancelled, stop running
                    keepRunning = false;
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
            _ = Task.Factory.StartNew(() => HandleConnections(cancelSource.Token), cancelSource.Token);
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
