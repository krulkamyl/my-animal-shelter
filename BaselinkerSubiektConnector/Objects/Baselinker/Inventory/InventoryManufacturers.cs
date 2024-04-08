using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Inventory
{
    public class InventoryManufacture
    {
        public int manufacturer_id { get; set; }
        public string name { get; set; }
    }


    public class InventoryManufactureResponse
    {
        public string status { get; set; }
        public List<InventoryManufacture> manufacturers { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

}
