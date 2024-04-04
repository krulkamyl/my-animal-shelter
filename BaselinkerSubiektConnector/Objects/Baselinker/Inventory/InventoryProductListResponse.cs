using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Inventory
{
    public class InventoryProduct
    {
        public int id { get; set; }
        public string ean { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public object stock { get; set; }
        public object prices { get; set; }
    }


    public class InventoryProductListResponse
    {
        public string status { get; set; }
        public List<InventoryProduct> products { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

}
