using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class ConfigRepository
    {
        public static string GetValue(string key)
        {

            Record record = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetConfigDatabaseName(),
                "key",
                key
                );

            if (record != null)
            {
                return record.value;
            }

            return null;
        }

        public static void SetValue(string key, string value)
        {
            Record record = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetConfigDatabaseName(),
                "key",
                key
                );
            if (record != null)
            {

                SQLiteConfigObject newRecord = new SQLiteConfigObject
                {
                    id = record.id,
                    key = record.key,
                    value = record.value
                };


                SQLiteService.UpdateRecord(
                    SQLiteDatabaseNames.GetConfigDatabaseName(),
                    record.id,
                    newRecord
                 );
            }
            else
            {
                SQLiteConfigObject newRecord = new SQLiteConfigObject
                {
                    key = key,
                    value = value
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetConfigDatabaseName(),
                    newRecord
                    );
            }
        }
    }
}
