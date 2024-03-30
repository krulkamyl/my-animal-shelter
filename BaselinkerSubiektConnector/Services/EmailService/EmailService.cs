using System;
using System.Net;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;
using System.Net.Mail;
using BaselinkerSubiektConnector.Repositories.SQLite;

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
                using (MailMessage mail = new MailMessage())
                {
                    if (!_senderEmail.Contains("@"))
                    {
                        mail.From = new MailAddress(_senderEmail+"@"+_smtpServer);
                    }
                    else
                    {
                        mail.From = new MailAddress(_senderEmail);
                    }
                    mail.To.Add(recipient);
                    mail.Subject = subject;

                    // Dodanie niestandardowego szablonu do treści wiadomości
                    string template = "\n\nWysłano przez BaselinkerToSubiektConnector\nby cichy.cloud";
                    body = $"{body}\n\n{template}";

                    mail.Body = body;

                    if (attachments != null)
                    {
                        foreach (string attachmentPath in attachments)
                        {
                            mail.Attachments.Add(new Attachment(attachmentPath));
                        }
                    }

                    using (SmtpClient smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mail);
                    }
                }
                Helpers.Log("Email sent successfully.");
            }
            catch (Exception ex)
            {
                Helpers.Log($"Error sending email: {ex.Message}");
            }
        }
    }
}
