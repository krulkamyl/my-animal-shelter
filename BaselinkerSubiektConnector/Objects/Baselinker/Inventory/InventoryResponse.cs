using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Inventory
{
    public class Inventory
    {
        public int inventory_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<string> languages { get; set; }
        public string default_language { get; set; }
        public List<int> price_groups { get; set; }
        public int default_price_group { get; set; }
        public List<string> warehouses { get; set; }
        public string default_warehouse { get; set; }
        public bool reservations { get; set; }
        public bool is_default { get; set; }
    }


    public class InventoryResponse
    {
        public string status { get; set; }
        public List<Inventory> inventories { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

}
