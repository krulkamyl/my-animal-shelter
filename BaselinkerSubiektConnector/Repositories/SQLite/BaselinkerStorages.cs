using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerStorages
    {
        public static void UpdateExistingData(List<BaselinkerStoragesResponseStorage> warehouses)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName()
            );

            foreach (var warehouse in warehouses)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = warehouse.storage_id,
                    key = warehouse.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName(),
                    record
                );
            }
        }
    }
}
