using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerInventoryWarehouses
    {
        public static void UpdateExistingData(List<InventoryWarehouse> inventoryWarehouses)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName()
            );

            foreach (InventoryWarehouse inventoryWarehouse in inventoryWarehouses)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = "bl_"+inventoryWarehouse.warehouse_id.ToString(),
                    key = inventoryWarehouse.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName(),
                    record
                );
            }
        }
    }
}
