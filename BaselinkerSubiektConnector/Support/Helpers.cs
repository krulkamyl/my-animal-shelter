using System;
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
    }
}
