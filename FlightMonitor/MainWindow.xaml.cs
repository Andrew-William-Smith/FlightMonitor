using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;

namespace FlightMonitor {
    public partial class MainWindow : Window, INotifyPropertyChanged {
        /// <summary>Client used to access simulator data.</summary>
        private readonly SimConnectClient simClient;

        /// <summary>The HTTP server used to field file requests from the client.</summary>
        private readonly HttpServer httpServer;

        // Window bindings
        public string WindowTitleStatus => "Flight Monitor — " + (simClient.Connected ? "Connected" : "Disconnected");
        public string CurrentIpAddress { get; set; }

        // Connect button bindings
        public string ConnectButtonText => (simClient.Connected ? "Disconnect from" : "Connect to") + " Flight Simulator";
        public bool ConnectButtonChecked {
            get => simClient.Connected;
            set {
                if (simClient.Connected) {
                    httpServer.Stop();
                    simClient.Disconnect();
                } else {
                    if (simClient.Connect()) {
                        httpServer.Start();
                    }
                }
                NotifyPropertyChanged("ConnectButtonText");
                NotifyPropertyChanged("WindowTitleStatus");
            }
        }

        public MainWindow() {
            InitializeComponent();
            simClient = new SimConnectClient();

            // Watch IP address
            SetIpAddress();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler((sender, e) => SetIpAddress());

            // Initialise HTTP server
            httpServer = new HttpServer();

            // Connect WPF bindings once data sources are initialised
            variablesGrid.ItemsSource = simClient.Variables;
            messageGrid.ItemsSource = simClient.Messages;
            Topmost = true;
            DataContext = this;
        }

        /// <summary>
        /// Get the system's current IP address and set the <c>CurrentIpAddress</c> property.
        /// </summary>
        private void SetIpAddress() {
            CurrentIpAddress =
                Dns.GetHostEntry(Dns.GetHostName())
                   .AddressList
                   .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                   .ToString();
        }

        #region Win32 message loop handling
        /// <summary>Get the <c>HwndSource</c> for this window.</summary>
        private HwndSource GetHwndSource() => PresentationSource.FromVisual(this) as HwndSource;

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            // Set up Win32 message loop interop routines
            GetHwndSource().AddHook(WndProc);
            simClient.WindowHandle = GetHwndSource().Handle;
        }

        /// <summary>Win32 message loop interop.</summary>
        private IntPtr WndProc(IntPtr hWnd, int iMsg, IntPtr hWParam, IntPtr hLParam, ref bool bHandled) {
            try {
                if (iMsg == SimConnectClient.WM_USER_SIMCONNECT) {
                    // Whenever a SimConnect event occurs, allow the client to process the message
                    simClient.ReceiveMessage();
                }
            } catch {
                simClient.Disconnect();
            }
            return IntPtr.Zero;
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
