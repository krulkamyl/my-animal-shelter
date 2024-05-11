using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Orders
{
    public class BaselinkerOrderStatusList
    {
        public string status { get; set; }
        public List<BaselinkerOrderStatusListStatus> statuses { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

    public class BaselinkerOrderStatusListStatus
    {
        public int id { get; set; }
        public string name { get; set; }
        public string name_for_customer { get; set; }
    }



}
