using System;

namespace BaselinkerSubiektConnector.Validators
{
    public class ConfigValidator
    {
        public static bool Validate(ConfigValidatorModel model)
        {
            if (model.MssqlDatabaseName == null || (model.MssqlDatabaseName != null && !model.MssqlDatabaseName.Contains("Nexo_")))
            {
                throw new Exception("Wybrano niepoprawną bazę danych Subiekta. Powinna ona posiadać przedrostek \"Nexo_\"");
            }
            if (model.MsTeamsWebhookUrl.Length == 0 || (model.MsTeamsWebhookUrl != null && !model.MsTeamsWebhookUrl.Contains("https://") && !model.MsTeamsWebhookUrl.Contains(".webhook.office.com")))
            {
                throw new Exception("Podano nieprawidłowe adres URL do Webhook'a.");
            }
            if (model.BaselinkerStorage == null || (model.BaselinkerStorage != null && model.BaselinkerStorage.Length < 4))
            {
                throw new Exception("Wybrano niepoprawny katalog produktów baselinker Baselinker.");
            }
            if (model.BaselinkerInventoryWarehouse == null || (model.BaselinkerInventoryWarehouse != null && model.BaselinkerInventoryWarehouse.Length < 4))
            {
                throw new Exception("Wybrano niepoprawny magazyn Baselinker.");
            }
            if (model.SubiektWarehouse == null || (model.SubiektWarehouse != null && model.SubiektWarehouse.Length < 2))
            {
                throw new Exception("Wybrano niepoprawny magazyn w Subiekt.");
            }
            if (model.SubiektBranch == null || (model.SubiektBranch != null && model.SubiektBranch.Length < 2))
            {
                throw new Exception("Wybrano niepoprawny oddział w Subiekt.");
            }
            if (model.PrinterEnabled && (model.PrinterName == null || (model.PrinterName != null && model.PrinterName.Length < 2)))
            {
                throw new Exception("Zaznaczono wydruk automatyczny faktur, a nie wybrano drukarki.");
            }
            if (model.CashRegisterEnabled && (model.CashRegisterName == null || (model.CashRegisterName != null && model.CashRegisterName.Length < 2)))
            {
                throw new Exception("Zaznaczono wydruk automatyczny paragonów, a nie wybrano kasy fiskalnej.");
            }
            if (model.SendEmailEnabled && (
                (model.EmailLogin == null || (model.EmailLogin != null && model.EmailLogin.Length < 3))
                ||
                (model.EmailPassword == null || (model.EmailPassword != null && model.EmailPassword.Length < 3))
                )
            )
            {
                throw new Exception("Zaznaczono automatyczne wysyłanie e-maili a nie podano prawidłowych danych logowania do serwera SMTP.");
            }
            if (model.EmailReporting == null || (model.EmailReporting != null && model.EmailReporting.Length < 3) || (model.EmailReporting != null && !model.EmailReporting.Contains("@")))
            {
                throw new Exception("Nie podano poprawnego adresu e-mail do raportowania błędów.");
            }
            if (model.CompanyName == null || (model.CompanyName != null && model.CompanyName.Length < 3))
            {
                throw new Exception("Wybrano niepoprawną nazwę firmy.");
            }
            if (model.CompanyName == null || (model.CompanyName != null && model.CompanyName.Length < 3))
            {
                throw new Exception("Wybrano niepoprawną nazwę firmy.");
            }
            if (model.CompanyNip == null || (model.CompanyNip != null && model.CompanyNip.Length < 9))
            {
                throw new Exception("Podano niepoprawny numer NIP.");
            }
            if (model.CompanyEmail == null || (model.CompanyEmail != null && model.CompanyEmail.Length < 3) || (model.CompanyEmail != null && !model.CompanyEmail.Contains("@")))
            {
                throw new Exception("Nie podano poprawnego adresu e-mail firmowego.");
            }

            return true;
        }
    }

    public class ConfigValidatorModel
    {
        public string MssqlDatabaseName { get; set; }
        public string BaselinkerInventoryWarehouse { get; set; }
        public string BaselinkerStorage { get; set; }
        public string SubiektWarehouse { get; set; }
        public string SubiektBranch { get; set; }
        public bool PrinterEnabled { get; set; }
        public string PrinterName { get; set; }
        public bool CashRegisterEnabled { get; set; }
        public string CashRegisterName { get; set; }
        public bool SendEmailEnabled { get; set; }
        public string EmailLogin { get; set; }
        public string EmailPassword { get; set; }
        public string EmailReporting { get; set; }
        public string CompanyName { get; set; }
        public string CompanyNip { get; set; }
        public string CompanyEmail { get; set; }
        public string MsTeamsWebhookUrl { get; set; }
    }
}
