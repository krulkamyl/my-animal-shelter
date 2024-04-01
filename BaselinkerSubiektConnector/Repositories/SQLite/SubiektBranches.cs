using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class SubiektBranches
    {
        public static void UpdateExistingData(List<string> branches)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetSubiektBranchesDatabaseName()
            );

            foreach (string branch in branches)
            {
                SQLiteSubiektBranchObject record = new SQLiteSubiektBranchObject
                {
                    key = branch,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(),
                    record
                );
            }
        }
    }
}
