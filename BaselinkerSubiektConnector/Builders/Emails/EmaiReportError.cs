using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.EmailService;

namespace BaselinkerSubiektConnector.Builders.Emails
{
    public class EmaiReportError
    {
        public static void Build(
            string errorData,
            BaselinkerOrderResponse baselinkerOrderResponse
        )
        {
            BaselinkerOrderResponseOrder blResponseOrder = baselinkerOrderResponse.orders[0];
            string message = "Wystąpił problem z wygenerowaniem dokumentu sprzedaży dla zamówienia #<strong>" + blResponseOrder.order_id + "</strong>\n" +
                "Adres, którego dotyczy problem: <a href=\""+GetBaselinkerOrderUrl(blResponseOrder)+"\">"+ GetBaselinkerOrderUrl(blResponseOrder) + "</a>\n" +
                "Dotyczy klienta: " + blResponseOrder.invoice_company + "\n\n" +
                "<strong>Treść błędu</strong>: \n" + errorData;

            EmailService emailService = new EmailService();
            emailService.SendEmail(
                ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailReporting),
                "Zamówienie #"+blResponseOrder.order_id+" - problem z utworzeniem dokumentu sprzedaży",
                message
            );
        }

        public static string GetBaselinkerOrderUrl(BaselinkerOrderResponseOrder baselinkerOrderResponseOrder)
        {
            return "https://panel-e.baselinker.com/orders.php#order:" + baselinkerOrderResponseOrder.order_id;
        }
    }
}
