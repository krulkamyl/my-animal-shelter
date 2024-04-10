using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class AssortmentRepository
    {

        public static List<Record> GetAssortmentConnectedWithSubiekt()
        {
            return SQLiteService.GetAssortmentConnectedWithSubiekt();
        }

        public static Record GetRecordByEan(string ean)
        {
            Record record = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetAssortmentsDatabaseName(),
                "ean_code",
                ean
                );
            return record;
        }

        public static void UpdateOrCreateRecord(string searchKey, string searchValue, SQLiteAssortmentObject objectToUpdate)
        {
            Record record = SQLiteService.ReadRecord(
            SQLiteDatabaseNames.GetAssortmentsDatabaseName(),
                searchKey,
                searchValue
            );
            if (record != null)
            {
                SQLiteService.UpdateRecord(
                    SQLiteDatabaseNames.GetAssortmentsDatabaseName(),
                    record.id,
                    objectToUpdate
                 );
            }
            else
            {
                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetAssortmentsDatabaseName(),
                    objectToUpdate
                );
            }
        }
    }
}
