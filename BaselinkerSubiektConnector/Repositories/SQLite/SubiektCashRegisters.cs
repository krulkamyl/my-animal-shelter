using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class SubiektCashRegisters
    {
        public static void UpdateExistingData(List<string> cashRegistes)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName()
            );

            foreach (string cashRegister in cashRegistes)
            {
                SQLiteSubiektCashRegisterObject record = new SQLiteSubiektCashRegisterObject
                {
                    key = cashRegister,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(),
                    record
                );
            }
        }
    }
}
