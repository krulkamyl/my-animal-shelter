using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerInventoryPriceGroups
    {
        public static void UpdateExistingData(List<PriceGroup> inventoryPriceGroups)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerInventoryPriceGroupsName()
            );

            foreach (PriceGroup inventoryPriceGroup in inventoryPriceGroups)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = inventoryPriceGroup.price_group_id.ToString(),
                    key = inventoryPriceGroup.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerInventoryPriceGroupsName(),
                    record
                );
            }
        }
    }
}
