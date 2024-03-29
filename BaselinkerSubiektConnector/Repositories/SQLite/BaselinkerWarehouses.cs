using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerWarehouses
    {
        public static void UpdateExistingData(List<BaselinkerStoragesResponseStorage> warehouses)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerWarehousesDatabaseName()
            );

            foreach (var warehouse in warehouses)
            {
                SQLiteBaselinkerWarehouseObject record = new SQLiteBaselinkerWarehouseObject
                {
                    value = warehouse.storage_id,
                    key = warehouse.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerWarehousesDatabaseName(),
                    record
                );
            }
        }
    }
}
