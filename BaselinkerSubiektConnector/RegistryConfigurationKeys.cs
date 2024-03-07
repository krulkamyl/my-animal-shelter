using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector
{
    public class RegistryConfigurationKeys
    {
        // MSSQL
        public const string MSSQL_Host = "MSSQL_HOST";
        public const string MSSQL_Login = "MSSQL_LOGIN";
        public const string MSSQL_Password = "MSSQL_PASSWORD";
        public const string MSSQL_DB_NAME = "MSSQL_DATABASE_NAME";

        // Subiekt
        public const string Subiekt_Login = "SUBIEKT_LOGIN";
        public const string Subiekt_Password = "SUBIEKT_PASSWORD";

        // Baselinker
        public const string Baselinker_ApiKey = "BASELINKER_APIKEY";
        public const string Baselinker_StorageId = "BASELINKER_STORAGE_ID";
        public const string Baselinker_StorageName = "BASELINKER_STORAGE_Name";

    }
}
