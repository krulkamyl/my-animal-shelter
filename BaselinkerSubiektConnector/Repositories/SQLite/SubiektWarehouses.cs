using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class SubiektWarehouses
    {
        public static void UpdateExistingData(List<string> warehouses)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName()
            );

            foreach (string warehouse in warehouses)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    key = warehouse,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(),
                    record
                );
            }
        }
    }
}
