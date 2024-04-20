using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class SalesDocsRepository
    {
        public static List<Record> GetAll()
        {
            return SQLiteService.ReadRecords(
                SQLiteDatabaseNames.GetSellerDocsDatabaseName()
                );
        }
    }
}
