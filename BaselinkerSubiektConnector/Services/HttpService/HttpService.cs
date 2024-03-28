using System;
using System.Net;
using System.Threading;
using System.Net.NetworkInformation;
using System.Windows;
using BaselinkerSubiektConnector.Support;
using BaselinkerSubiektConnector.Builders;

namespace BaselinkerSubiektConnector.Services.HttpService
{
    public class HttpService
    {
        private HttpListener httpListener;
        private Thread listenerThread;
        public MainWindowViewModel mainWindowViewModel;

        public string port = "8913";

        public bool IsEnabled { get; private set; }
        public string ServerIpAddress { get; private set; }

        public void StartStop()
        {
            if (IsEnabled)
            {
                Stop();
            }
            else
            {
                Start(this.port);
            }
        }

        public void Start(string port)
        {
            try
            {
                httpListener = new HttpListener();
                if (port.Length > 1)
                {
                    port = this.port;
                } else
                {
                    this.port = port;
                }
                ServerIpAddress = GetLocalIPAddress();

                Helpers.Log($"IP: {ServerIpAddress}");
                Helpers.Log($"Port: {port}");

                httpListener.Prefixes.Add("http://" + ServerIpAddress + ":" + port + "/");
                httpListener.Start();
                listenerThread = new Thread(Listen);
                listenerThread.Start();
                IsEnabled = true;


                Helpers.Log($"Serwis HTTP został uruchomiony. Adres IP serwera: {ServerIpAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n Sprawdź czy aplikacja została uruchomiona jako \"ADMINISTRATOR\"", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Stop()
        {
            try
            {
                if (httpListener != null)
                {
                    httpListener.Stop();
                    listenerThread.Abort();
                    IsEnabled = false;
                    Helpers.Log("Serwis HTTP został zatrzymany.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (item.OperationalStatus == OperationalStatus.Up && item.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                if (ip.Address.ToString().StartsWith("192.") || ip.Address.ToString().StartsWith("10."))
                                {
                                    return ip.Address.ToString();
                                }
                            }
                        }
                    }
                }
                throw new Exception("Brak adresu IP");
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return "";
        }

        private void Listen()
        {
            try
            {
                while (httpListener.IsListening)
                {
                    var context = httpListener.GetContext();
                    var request = context.Request;

                    if (request.QueryString.HasKeys())
                    {
                        foreach (string key in request.QueryString.AllKeys)
                        {
                            if (key == "baselinker_order_href")
                            {
                                int orderId = int.Parse(Helpers.GetOrderId(request.QueryString[key]));
                                if (orderId > 0)
                                {
                                   new SubiektInvoiceReceiptBuilder(orderId, mainWindowViewModel);
                                }
                            }
                        }
                    }

                    var output = context.Response.OutputStream;
                    output.Close();
                }
            }
            catch (ThreadAbortException)
            {
                // Ignoruj, gdy wątek jest zatrzymywany
            }
            catch (Exception ex)
            {
                Helpers.Log($"Błąd serwera: {ex.Message}");
            }
        }
    }
}
