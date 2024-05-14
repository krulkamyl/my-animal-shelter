namespace BaselinkerSubiektConnector
{
    public class  RegistryConfigurationKeys
    {
        // MSSQL
        public const string MSSQL_Host = "MSSQL_HOST";
        public const string MSSQL_Login = "MSSQL_LOGIN";
        public const string MSSQL_Password = "MSSQL_PASSWORD";
        public const string MSSQL_DB_NAME = "MSSQL_DATABASE_NAME";

        // Subiekt
        public const string Subiekt_Login = "SUBIEKT_LOGIN";
        public const string Subiekt_Sfera_Autorun = "SUBIEKT_SFERA_AUTORUN";
        public const string Subiekt_Password = "SUBIEKT_PASSWORD";

        public const string Subiekt_Default_Warehouse = "SUBIEKT_DEFAULT_WAREHOUSE";
        public const string Subiekt_Default_Branch = "SUBIEKT_DEFAULT_BRANCH";


        public const string Subiekt_PrinterEnabled = "SUBIEKT_PRINTER_ENABLED";
        public const string Subiekt_PrinterName = "SUBIEKT_PRINTER_NAME";

        public const string Subiekt_CashRegisterEnabled = "SUBIEKT_CASH_REGISTER_ENABLED";
        public const string Subiekt_CashRegisterName = "SUBIEKT_CASH_REGISTER_NAME";


        public const string SyncServiceIntervalTime = "SYNC_SERVICE_INTERVAL_TIME";

        // Baselinker
        public const string Baselinker_ApiKey = "BASELINKER_APIKEY";
        public const string Baselinker_StorageId = "BASELINKER_STORAGE_ID";
        public const string Baselinker_StorageName = "BASELINKER_STORAGE_Name";
        public const string Baselinker_InventoryWarehouseId = "BASELINKER_INVENTORY_WAREHOUSE_ID";
        public const string Baselinker_InventoryWarehouseName = "BASELINKER_INVENTORY_WAREHOUSE_NAME";
        public const string Baselinker_AddCommentDocNumber = "BASELINKER_ADD_COMMENT_DOC_NUMBER";

        // Email

        public const string Config_EmailSendAuto = "CONFIG_EMAIL_SEND_AUTO";
        public const string Config_EmailServer = "CONFIG_EMAIL_SERVER";
        public const string Config_EmailPort = "CONFIG_EMAIL_PORT";
        public const string Config_EmailLogin = "CONFIG_EMAIL_LOGIN";
        public const string Config_EmailPassword = "CONFIG_EMAIL_PASSWORD";

        public const string Config_EmailReporting = "CONFIG_EMAIL_REPORTING";

        public const string Email_Template = "EMAIL_TEMPLATE";

        // Company
        public const string Config_CompanyName = "CONFIG_COMPANY_NAME";
        public const string Config_CompanyNip = "CONFIG_COMPANY_NIP";
        public const string Config_CompanyAddress = "CONFIG_COMPANY_ADDRESS";
        public const string Config_CompanyZipCode = "CONFIG_COMPANY_ZIP_CODE";
        public const string Config_CompanyCity = "CONFIG_COMPANY_CITY";
        public const string Config_CompanyEmailAddress = "CONFIG_COMPANY_EMAIL_ADDRESS";
        public const string Config_CompanyPhone = "CONFIG_COMPANY_PHONE";

        public const string AutoRun_IntervalSyncQtyWarehouse = "AUTO_RUN_INTERVAL_SYNC_QTY_WAREHOUSE";

        public const string MSTeams_Webhook_Url = "MSTEAMS_WEBHOOK_URL";

    }
}
