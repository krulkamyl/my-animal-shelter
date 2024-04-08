using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerInventoryManufacturers
    {
        public static void UpdateExistingData(List<InventoryManufacture> inventoryManufactures)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerInventoryManufacturersName()
            );

            foreach (InventoryManufacture inventoryManufacture in inventoryManufactures)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = inventoryManufacture.manufacturer_id.ToString(),
                    key = inventoryManufacture.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerInventoryManufacturersName(),
                    record
                );
            }
        }
    }
}
