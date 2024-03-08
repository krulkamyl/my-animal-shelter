using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
            }
            else
            {
                Console.WriteLine("[Baselinker GetOrderId] Nie znaleziono pasującego numeru.");
            }
            return url;
        }
    }
}
