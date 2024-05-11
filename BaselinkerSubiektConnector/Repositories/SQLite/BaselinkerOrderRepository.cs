using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerOrderRepository
    {
        public static List<Record> GetAll()
        {
            return SQLiteService.ReadRecords(
                SQLiteDatabaseNames.GetBaselinkerOrdersTable()
                );
        }

        public static void CreateRecord(SQLiteBaselinkerOrderObject obj)
        {
            SQLiteService.CreateRecord(
                SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                obj
            );
        }

        public static void UpdateOrCreateRecord(string searchKey, string searchValue, SQLiteBaselinkerOrderObject objectToUpdate)
        {
            Record record = SQLiteService.ReadRecord(
            SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                searchKey,
                searchValue
            );
            if (record != null)
            {
                SQLiteService.UpdateRecord(
                    SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                    record.id,
                    objectToUpdate
                 );
            }
            else
            {
                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                    objectToUpdate
                );
            }
        }

        public static void CreateRecordWhenNotExist(string searchKey, string searchValue, SQLiteBaselinkerOrderObject objectToUpdate)
        {
            Record record = SQLiteService.ReadRecord(
            SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                searchKey,
                searchValue
            );
            if (record == null)
            {
                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerOrdersTable(),
                    objectToUpdate
                );
            }
        }
    }
}
