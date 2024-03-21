using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;

namespace BaselinkerSubiektConnector.Support 
{
    public class Helpers
    {
        public static string GetOrderId(string url)
        {
            Regex regex = new Regex(@"https://orders-e\.baselinker\.com/(\d+)/");

            Match match = regex.Match(url);

            if (match.Success)
            {
                string number = match.Groups[1].Value;
                Console.WriteLine("[Baselinker GetOrderId] Order ID: " + number);
                return number;
            }
            else
            {
                Console.WriteLine("[Baselinker GetOrderId] Nie znaleziono pasującego numeru.");
            }
            return url;
        }

       public static string ExtractDigits(string input)
       {
            string pattern = @"\D+";

            string result = Regex.Replace(input, pattern, "");

            return result;
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
    }
}
