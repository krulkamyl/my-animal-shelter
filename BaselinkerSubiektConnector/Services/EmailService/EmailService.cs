using System;
using System.Net;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;
using System.Net.Mail;
using BaselinkerSubiektConnector.Repositories.SQLite;
using System.Text.RegularExpressions;

namespace BaselinkerSubiektConnector.Services.EmailService
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;

        public EmailService()
        {
            _smtpServer = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailServer);
            _smtpPort = int.Parse(ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPort));
            _senderEmail = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailLogin);
            _senderPassword = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPassword);

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        public void SendEmail(string recipient, string subject, string body, List<string> attachments = null)
        {
            try
            {
                using (var mail = new MailMessage())
                {
                    ConfigureMailSender(mail);
                    mail.To.Add(recipient);
                    mail.Subject = subject;
                    mail.IsBodyHtml = true;

                    AddBccIfNecessary(mail, recipient);
                    mail.Body = GenerateEmailBody(body);

                    AddAttachments(mail, attachments);
                    SendMail(mail);
                }

                Helpers.Log("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Helpers.Log($"Error sending email: {ex.Message}");
            }
        }

        private void ConfigureMailSender(MailMessage mail)
        {
            string emailAddress = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress);
            if (!string.IsNullOrEmpty(emailAddress) && emailAddress.Contains("@"))
            {
                mail.From = new MailAddress(emailAddress);
            }
            else if (!_senderEmail.Contains("@"))
            {
                mail.From = new MailAddress($"{_senderEmail}@{_smtpServer}");
            }
            else
            {
                mail.From = new MailAddress(_senderEmail);
            }
        }

        private void AddBccIfNecessary(MailMessage mail, string recipient)
        {
            if (recipient != ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailReporting))
            {
                mail.Bcc.Add(ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailReporting));
            }
        }

        private string GenerateEmailBody(string body)
        {
            string template = ConfigRepository.GetValue(RegistryConfigurationKeys.Email_Template) ?? GetDefaultEmailTemplate();
            string bodyHtml = Regex.Replace(body, "\n", "<br />");

            var replacements = new Dictionary<string, string>
        {
            { "[TRESC_WIADOMOSCI]", bodyHtml },
            { "[FIRMA_NAZWA]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyName) },
            { "[FIRMA_NIP]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyNip) },
            { "[FIRMA_ADRES]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyAddress) },
            { "[FIRMA_KOD_POCZTOWY]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyZipCode) },
            { "[FIRMA_MIEJSCOWOSC]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyCity) },
            { "[FIRMA_TELEFON]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress) },
            { "[FIRMA_EMAIL]", ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyPhone) }
        };

            return ReplacePlaceholders(template, replacements);
        }

        private string ReplacePlaceholders(string template, Dictionary<string, string> replacements)
        {
            foreach (var replacement in replacements)
            {
                template = ReplaceIgnoreCase(template, replacement.Key, replacement.Value);
            }

            return template;
        }

        private string ReplaceIgnoreCase(string input, string search, string replacement)
        {
            int index = input.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            while (index != -1)
            {
                input = input.Substring(0, index) + replacement + input.Substring(index + search.Length);
                index = input.IndexOf(search, index + replacement.Length, StringComparison.OrdinalIgnoreCase);
            }

            return input;
        }

        private void AddAttachments(MailMessage mail, List<string> attachments)
        {
            if (attachments != null)
            {
                foreach (var attachmentPath in attachments)
                {
                    mail.Attachments.Add(new Attachment(attachmentPath));
                }
            }
        }

        private void SendMail(MailMessage mail)
        {
            using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Send(mail);
            }
        }

        private string GetDefaultEmailTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""pl"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Wiadomość e-mail</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f2f2f2;
            margin: 0;
            padding: 0;
        }

        .container {
            max-width: 700px;
            margin: 20px auto;
            background-color: #ffffff;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0px 0px 10px 0px rgba(0,0,0,0.1);
        }

        .header {
            text-align: center;
            padding-bottom: 20px;
        }

        .logo {
            width: 250px;
        }

        .content {
            padding: 20px 0;
        }

        .footer {
            background-color: #007bff;
            color: #ffffff;
            text-align: center;
            padding: 10px 0;
            border-radius: 0 0 8px 8px;
        }

        .company-info {
            background-color: #f2f2f2;
            padding: 5px;
            border-radius: 8px;
            margin-top: 10px;
            color: #333333;
        }
        .company-info p {
            line-height: 2px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""color: #007bff;"">NexoLink</h1>
        </div>
        <div class=""content"">
            [TRESC_WIADOMOSCI]
        </div>

        <div class=""company-info"">
            <p>Dostawca wiadomości</p>
            <p><strong>[FIRMA_NAZWA]</strong></p>
            <p>[FIRMA_ADRES], [FIRMA_KOD_POCZTOWY] [FIRMA_MIEJSCOWOSC]</p>
            <p>[FIRMA_TELEFON]</p>
            <p>[FIRMA_EMAIL]</p>
        </div>
    </div>

    <div class=""footer"">
        <p style=""color: #ffffff;"">Email został wysłany przez NexoLink</p>
    </div>
</body>
</html>";

        }

    }
}
