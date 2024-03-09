using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
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
        private BaselinkerOrderResponse blOrderResponse;

        public SubiektInvoiceReceiptBuilder(int baselinkerOrderId)
        {
            this.baselinkerOrderId = baselinkerOrderId;

            string blApiKey = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
            string storageId = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);

            this.blAdapter = new BaselinkerAdapter(blApiKey, storageId);
            InitializeOrderResponseAsync().Wait();
            Console.WriteLine(JsonConvert.SerializeObject(this.blOrderResponse));

        }
        private async Task InitializeOrderResponseAsync()
        {
            this.blOrderResponse = await blAdapter.GetOrderAsync(baselinkerOrderId);
        }

        private bool checkCustomerExist()
        {
            return false;
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
