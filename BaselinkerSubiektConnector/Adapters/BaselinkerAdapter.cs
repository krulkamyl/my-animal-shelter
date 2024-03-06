using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector.Adapters
{
    public class BaselinkerAdapter : BaselinkerAdapterInterface
    {
        private readonly HttpClient _client;
        private readonly string _endpoint;
        private readonly string _apiKey;
        public string _storageId;

        public BaselinkerAdapter(string apiKey, string storageId = null)
        {
            _apiKey = apiKey;
            _storageId = storageId;
            _endpoint = "connector.php";
            _client = new HttpClient
            {
                BaseAddress = new Uri("https://api.baselinker.com/")
            };
            _client.DefaultRequestHeaders.Add("X-BLToken", _apiKey);
        }

        public async Task<BaselinkerProductsListResponse> GetProductDataAsync(string productIds)
        {
            string[] products = { productIds };

            var parameters = new Dictionary<string, object>
            {
                { "storage_id", _storageId },
                { "products", products }
            };

            var data = new Dictionary<string, string>
            {
                { "method", "getProductsData" },
                { "parameters", JsonConvert.SerializeObject(parameters) }
            };

            var response = await _client.PostAsync(_endpoint, new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BaselinkerProductsListResponse> (responseBody);
        }

        public async Task<BaselinkerProductsListResponse> GetProductsListAsync(int page = 1)
        {
            var parameters = new Dictionary<string, object>
            {
                { "storage_id", _storageId },
                { "page", page }
            };

            var data = new Dictionary<string, string>
            {
                { "method", "getProductsList" },
                { "parameters", JsonConvert.SerializeObject(parameters) }
            };

            var response = await _client.PostAsync(_endpoint, new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject <BaselinkerProductsListResponse> (responseBody);
        }

        public async Task<BaselinkerOrderResponse> GetOrderAsync(int orderId = 0)
        {
            var parameters = new Dictionary<string, object>
            {
                { "order_id", orderId },
            };

            var data = new Dictionary<string, string>
            {
                { "method", "getOrders" },
                { "parameters", JsonConvert.SerializeObject(parameters) }
            };

            var response = await _client.PostAsync(_endpoint, new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<BaselinkerOrderResponse>(responseBody);
        }

        public async Task<BaselinkerStoragesResponse> GetStoragesListAsync()
        {
            var parameters = new Dictionary<string, object>();

            var data = new Dictionary<string, string>
            {
                { "method", "getStoragesList" },
                { "parameters", JsonConvert.SerializeObject(parameters) }
            };

            var response = await _client.PostAsync(_endpoint, new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync(); 
            return JsonConvert.DeserializeObject<BaselinkerStoragesResponse>(responseBody);

        }
    }

    public interface BaselinkerAdapterInterface
    {
        Task<BaselinkerProductsListResponse> GetProductDataAsync(string productId);
        Task<BaselinkerProductsListResponse> GetProductsListAsync(int page = 1);
        Task<BaselinkerOrderResponse> GetOrderAsync(int orderId = 0);
        Task<BaselinkerStoragesResponse> GetStoragesListAsync();
    }
}
