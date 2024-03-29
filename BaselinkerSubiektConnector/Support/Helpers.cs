using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace BaselinkerSubiektConnector.Support
{

    public class Helpers
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();

        public static string GetOrderId(string url)
        {
            Regex regex = new Regex(@"https://orders-e\.baselinker\.com/(\d+)/");

            Match match = regex.Match(url);

            if (match.Success)
            {
                string number = match.Groups[1].Value;
                Helpers.Log("[Baselinker GetOrderId] Order ID: " + number);
                return number;
            }
            else
            {
                Helpers.Log("[Baselinker GetOrderId] Nie znaleziono pasującego numeru.");
            }
            return url;
        }

       public static string ExtractDigits(string input)
       {
            string pattern = @"\D+";

            string result = Regex.Replace(input, pattern, "");

            return result;
        }



        public static string GetApplicationPath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return appDataFolder + "\\cichy.cloud\\Subiekt Baselinker Connector\\";
        }

        public static List<string> GetPrinters()
        {
            List<string> printers = new List<string>();
            ManagementScope objScope = new ManagementScope(ManagementPath.DefaultPath);
            objScope.Connect();

            SelectQuery selectQuery = new SelectQuery();
            selectQuery.QueryString = "Select * from win32_Printer";
            ManagementObjectSearcher MOS = new ManagementObjectSearcher(objScope, selectQuery);
            ManagementObjectCollection MOC = MOS.Get();
            foreach (ManagementObject mo in MOC)
            {
                printers.Add(mo["Name"].ToString());
            }
            return printers;
        }

        public static void Log(string message)
        {
            DateTime now = DateTime.Now;
            string timestamp = now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"{timestamp}: {message}";
            string logFilePath = Path.Combine(GetApplicationPath(), "Logs.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                Helpers.Log($"Błąd podczas zapisu do pliku logów: {ex.Message}");
            }

            Console.WriteLine(logMessage);
        }

        public static void StartLog()
        {
            Log("############################");
            Log("############################");
            Log("############################");
        }

        public static string GetExportApplicationPath()
        {
            return Path.Combine(GetApplicationPath(),"Export");
        }

        public static void EnsureExportFolderExists()
        {
            try
            {
                if (!Directory.Exists(GetExportApplicationPath()))
                {
                    Directory.CreateDirectory(GetExportApplicationPath());
                    Log("Utworzono folder 'Export'.");
                }
                else
                {
                    Log("Folder 'Export' już istnieje.");
                }
            }
            catch (Exception ex)
            {
                Log($"Wystąpił błąd podczas sprawdzania/utwarzania folderu 'Export': {ex.Message}");
            }
        }
    }
}
