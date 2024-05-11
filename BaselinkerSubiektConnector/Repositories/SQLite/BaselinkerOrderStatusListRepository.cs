using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerOrderStatusListRepository
    {
        public static void UpdateExistingData(List<BaselinkerOrderStatusListStatus> statuses)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerOrderStatusesDatabaseName()
            );

            foreach (BaselinkerOrderStatusListStatus status in statuses)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = status.id.ToString(),
                    key = status.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerOrderStatusesDatabaseName(),
                    record
                );
            }
        }
    }
}
