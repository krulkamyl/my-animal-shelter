using System;

namespace BaselinkerSubiektConnector.Validators
{
    public class ConfigValidator
    {
        public static bool Validate(ConfigValidatorModel model)
        {
            ValidateNotNullAndContains(model.MssqlDatabaseName, "Nexo_", "Wybrano niepoprawną bazę danych Subiekta. Powinna ona posiadać przedrostek \"Nexo_\"");
            ValidateUrl(model.MsTeamsWebhookUrl, "Podano nieprawidłowe adres URL do Webhook'a.");
            ValidateNotNullAndMinLength(model.BaselinkerStorage, 4, "Wybrano niepoprawny katalog produktów baselinker Baselinker.");
            ValidateNotNullAndMinLength(model.BaselinkerInventoryWarehouse, 4, "Wybrano niepoprawny magazyn Baselinker.");
            ValidateNotNullAndMinLength(model.SubiektWarehouse, 2, "Wybrano niepoprawny magazyn w Subiekt.");
            ValidateNotNullAndMinLength(model.SubiektBranch, 2, "Wybrano niepoprawny oddział w Subiekt.");

            if (model.PrinterEnabled)
                ValidateNotNullAndMinLength(model.PrinterName, 2, "Zaznaczono wydruk automatyczny faktur, a nie wybrano drukarki.");
            if (model.CashRegisterEnabled)
                ValidateNotNullAndMinLength(model.CashRegisterName, 2, "Zaznaczono wydruk automatyczny paragonów, a nie wybrano kasy fiskalnej.");
            if (model.SendEmailEnabled)
            {
                ValidateNotNullAndMinLength(model.EmailLogin, 3, "Zaznaczono automatyczne wysyłanie e-maili a nie podano prawidłowych danych logowania do serwera SMTP.");
                ValidateNotNullAndMinLength(model.EmailPassword, 3, "Zaznaczono automatyczne wysyłanie e-maili a nie podano prawidłowych danych logowania do serwera SMTP.");
            }
            ValidateEmail(model.EmailReporting, "Nie podano poprawnego adresu e-mail do raportowania błędów.");
            ValidateNotNullAndMinLength(model.CompanyName, 3, "Wybrano niepoprawną nazwę firmy.");
            ValidateNotNullAndMinLength(model.CompanyNip, 9, "Podano niepoprawny numer NIP.");
            ValidateEmail(model.CompanyEmail, "Nie podano poprawnego adresu e-mail firmowego.");

            return true;
        }

        private static void ValidateNotNullAndContains(string value, string substring, string errorMessage)
        {
            if (string.IsNullOrEmpty(value) || !value.Contains(substring))
            {
                throw new Exception(errorMessage);
            }
        }

        private static void ValidateUrl(string url, string errorMessage)
        {
            if (string.IsNullOrEmpty(url) || (!url.Contains("https://") && !url.Contains(".webhook.office.com")))
            {
                throw new Exception(errorMessage);
            }
        }

        private static void ValidateNotNullAndMinLength(string value, int minLength, string errorMessage)
        {
            if (string.IsNullOrEmpty(value) || value.Length < minLength)
            {
                throw new Exception(errorMessage);
            }
        }

        private static void ValidateEmail(string email, string errorMessage)
        {
            if (string.IsNullOrEmpty(email) || email.Length < 3 || !email.Contains("@"))
            {
                throw new Exception(errorMessage);
            }
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
