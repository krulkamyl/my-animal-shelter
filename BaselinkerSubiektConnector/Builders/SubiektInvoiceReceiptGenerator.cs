using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using InsERT.Moria.Klienci;
using InsERT.Moria.ModelDanych;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector.Builders
{
    public class SubiektInvoiceReceiptBuilder
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();
        private int baselinkerOrderId;
        private BaselinkerAdapter blAdapter;
        public MainWindowViewModel mainWindowViewModel;
        private BaselinkerOrderResponse blOrderResponse;

        public SubiektInvoiceReceiptBuilder(int baselinkerOrderId, MainWindowViewModel mainWindowViewModel)
        {
            this.baselinkerOrderId = baselinkerOrderId;
            this.mainWindowViewModel = mainWindowViewModel;

            string blApiKey = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
            string storageId = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);

            this.blAdapter = new BaselinkerAdapter(blApiKey, storageId);
            InitializeOrderResponseAsync().Wait();
            Console.WriteLine(JsonConvert.SerializeObject(this.blOrderResponse));
            Podmiot customer = checkCustomerExist();
            if (customer == null)
            {
                this.createCustomer();
            }

        }
        private async Task InitializeOrderResponseAsync()
        {
            this.blOrderResponse = await blAdapter.GetOrderAsync(baselinkerOrderId);
        }

        private Podmiot checkCustomerExist()
        {
            IPodmioty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IPodmioty>();
            BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
            Podmiot customerNip = podmioty.Dane.Wszystkie().Where(pdm => pdm.NIP == blResponseOrder.invoice_nip).FirstOrDefault();
            if (customerNip != null)
            {
                return customerNip;
            }
            Podmiot customerPerson = podmioty.Dane.Wszystkie().Where(pdm => pdm.NazwaSkrocona == blResponseOrder.invoice_fullname)
                .Where(pdm => pdm.DomyslnyAdresZameldowania.Linia2 == blResponseOrder.invoice_postcode + " " + blResponseOrder.invoice_city).FirstOrDefault();
            if (customerPerson != null)
            {
                return customerPerson;
            }
            return null;
        }


        private async Task createCustomer()
        {

        }

        private bool checkReceiptInvoiceExist()
        {
            return false;
        }

        private void createReceipt()
        {

        }

        private void createInvoice()
        {

        }

    }
}
