using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class SubiektLogins
    {
        public static void UpdateExistingData(List<string> logins)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetSubiektLoginsDatabaseName()
            );

            foreach (string login in logins)
            {
                SQLiteSubiektLoginObject record = new SQLiteSubiektLoginObject
                {
                    key = login,
                    value = login,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetSubiektLoginsDatabaseName(),
                    record
                );
            }
        }
    }
}
