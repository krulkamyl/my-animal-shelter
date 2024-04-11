using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Inventory
{
    public class InventoryWarehouseResponse
    {
        public string status { get; set; }
        public List<InventoryWarehouse> warehouses { get; set; }
    }

    public class InventoryWarehouse
    {
        public string warehouse_type { get; set; }
        public int warehouse_id { get; set; }
        public string name { get; set; }
        public object description { get; set; }
        public bool stock_edition { get; set; }
        public bool is_default { get; set; }
    }

}
