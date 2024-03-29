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

        public const string Subiekt_Default_Warehouse = "SUBIEKT_DEFAULT_WAREHOUSE";
        public const string Subiekt_Default_Branch = "SUBIEKT_DEFAULT_BRANCH";


        public const string Subiekt_PrinterEnabled = "SUBIEKT_PRINTER_ENABLED";
        public const string Subiekt_PrinterName = "SUBIEKT_PRINTER_NAME";

        public const string Subiekt_CashRegisterEnabled = "SUBIEKT_CASH_REGISTER_ENABLED";
        public const string Subiekt_CashRegisterName = "SUBIEKT_CASH_REGISTER_NAME";

        // Baselinker
        public const string Baselinker_ApiKey = "BASELINKER_APIKEY";
        public const string Baselinker_StorageId = "BASELINKER_STORAGE_ID";
        public const string Baselinker_StorageName = "BASELINKER_STORAGE_Name";

        // Email

        public const string Config_EmailSendAuto = "CONFIG_EMAIL_SEND_AUTO";
        public const string Config_EmailServer = "CONFIG_EMAIL_SERVER";
        public const string Config_EmailPort = "CONFIG_EMAIL_PORT";
        public const string Config_EmailLogin = "CONFIG_EMAIL_LOGIN";
        public const string Config_EmailPassword = "CONFIG_EMAIL_PASSWORD";

    }
}
