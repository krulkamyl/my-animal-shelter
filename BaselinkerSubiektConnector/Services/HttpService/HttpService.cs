using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using BaselinkerSubiektConnector.Support;

namespace BaselinkerSubiektConnector.Services.HttpService
{
    public class HttpService
    {
        private HttpListener httpListener;
        private Thread listenerThread;

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

                Console.WriteLine($"IP: {ServerIpAddress}");
                Console.WriteLine($"Port: {port}");

                httpListener.Prefixes.Add("http://" + ServerIpAddress + ":" + port + "/");
                httpListener.Start();
                listenerThread = new Thread(Listen);
                listenerThread.Start();
                IsEnabled = true;


                Console.WriteLine($"Serwis HTTP został uruchomiony. Adres IP serwera: {ServerIpAddress}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    Console.WriteLine("Serwis HTTP został zatrzymany.");
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
                                Helpers.GetOrderId(request.QueryString[key]);
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
                Console.WriteLine($"Błąd serwera: {ex.Message}");
            }
        }
    }
}
