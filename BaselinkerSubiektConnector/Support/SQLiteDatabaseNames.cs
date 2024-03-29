using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector.Support
{
    public class SQLiteDatabaseNames
    {

        public static string GetConfigDatabaseName()
        {
            return "config";
        }

        public static string GetAssortmentsDatabaseName()
        {
            return "assortments";
        }


        public static string GetBaselinkerWarehousesDatabaseName()
        {
            return "baselinker_warehouses";
        }

        public static string GetSubiektWarehousesDatabaseName()
        {
            return "subiekt_warehouses";
        }

        public static string GetSubiektBranchesDatabaseName()
        {
            return "subiekt_branches";
        }

        public static string GetSubiektCashRegistersDatabaseName()
        {
            return "subiekt_cashregisters";
        }
    }
}
