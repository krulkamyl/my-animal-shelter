using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerIntentories
    {
        public static void UpdateExistingData(List<Inventory> inventories)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerInventoriesDatabaseName()
            );

            foreach (Inventory inventory in inventories)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = inventory.inventory_id.ToString(),
                    key = inventory.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerInventoriesDatabaseName(),
                    record
                );
            }
        }
    }
}
