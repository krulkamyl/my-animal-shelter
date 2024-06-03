using BaselinkerSubiektConnector.Repositories.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;

namespace BaselinkerSubiektConnector.Support
{
    public static class Helpers
    {
        private const string OrdersUrlPattern = @"https://orders-e\.baselinker\.com/(\d+)/";
        private const string DigitsPattern = @"\D+";
        private const string ExportFolderName = "Export";
        private const string LogsFileName = "Logs.txt";

        public static string GetOrderId(string url)
        {
            var match = Regex.Match(url, OrdersUrlPattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            Log("[Helpers-GetOrderId] Nie znaleziono pasującego numeru.");
            return url;
        }

        public static string ExtractDigits(string input)
        {
            return Regex.Replace(input, DigitsPattern, string.Empty);
        }

        public static string GetApplicationPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cichy.cloud", "NexoLink");
        }

        public static void SendWebhook(string message)
        {
            var webhookUrl = ConfigRepository.GetValue(RegistryConfigurationKeys.MSTeams_Webhook_Url);
            if (!string.IsNullOrEmpty(webhookUrl) && webhookUrl.Length > 10)
            {
                var teamsWebhookClient = new TeamsWebhookClient(webhookUrl);
                _ = teamsWebhookClient.SendMessageAsync(message);
            }
        }

        public static List<string> GetPrinters()
        {
            var printers = new List<string>();
            var objScope = new ManagementScope(ManagementPath.DefaultPath);
            objScope.Connect();

            var query = new SelectQuery("Select * from win32_Printer");
            var searcher = new ManagementObjectSearcher(objScope, query);
            foreach (ManagementObject mo in searcher.Get())
            {
                printers.Add(mo["Name"].ToString());
            }

            return printers;
        }

        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"{timestamp}: {message}";
            var logFilePath = Path.Combine(GetApplicationPath(), LogsFileName);

            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception)
            {
                // Log exception if necessary
            }

            Console.WriteLine(logMessage);
        }

        public static void StartLog()
        {
            var startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Log("############################");
            Log($"# Uruchomiono program: {startTime} #");
            Log("############################");
        }

        public static string GetExportApplicationPath()
        {
            return Path.Combine(GetApplicationPath(), ExportFolderName);
        }

        public static void EnsureExportFolderExists()
        {
            try
            {
                var exportPath = GetExportApplicationPath();
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                    Log("[Helpers - EnsureExportFolderExists] Utworzono folder 'Export'.");
                }
                else
                {
                    Log("[Helpers - EnsureExportFolderExists] Folder 'Export' już istnieje.");
                }
            }
            catch (Exception ex)
            {
                Log($"[Helpers - EnsureExportFolderExists] Wystąpił błąd podczas sprawdzania/utwarzania folderu 'Export': {ex.Message}");
            }
        }
    }
}