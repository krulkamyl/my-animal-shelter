using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Inventory
{
    public class PriceGroup
    {
        public int price_group_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string currency { get; set; }
        public bool is_default { get; set; }
    }


    public class InventoryPriceGroup
    {
        public string status { get; set; }
        public List<PriceGroup> price_groups { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

}
