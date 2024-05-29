using BaselinkerSubiektConnector.Composites;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
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
    public class BaselinkerAdapter : IBaselinkerAdapter
    {
        private readonly HttpClient _client;
        private readonly string _endpoint = "connector.php";
        private readonly string _apiKey;
        private readonly string _storageId;

        public BaselinkerAdapter(string apiKey, string storageId = null)
        {
            _apiKey = apiKey;
            _storageId = storageId;
            _client = new HttpClient { BaseAddress = new Uri("https://api.baselinker.com/") };
            _client.DefaultRequestHeaders.Add("X-BLToken", _apiKey);
        }

        private async Task<T> PostAsync<T>(string method, object parameters)
        {
            var data = new Dictionary<string, string>
            {
                { "method", method },
                { "parameters", JsonConvert.SerializeObject(parameters) }
            };

            var response = await _client.PostAsync(_endpoint, new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        public Task<BaselinkerProductsListResponse> GetProductDataAsync(string productIds)
        {
            var parameters = new
            {
                storage_id = _storageId,
                products = new[] { productIds }
            };
            return PostAsync<BaselinkerProductsListResponse>("getProductsData", parameters);
        }

        public Task<BaselinkerProductsListResponse> GetProductsListAsync(int page = 1)
        {
            var parameters = new { storage_id = _storageId, page };
            return PostAsync<BaselinkerProductsListResponse>("getProductsList", parameters);
        }

        public Task<BaselinkerOrderResponse> GetOrderAsync(int orderId = 0)
        {
            var parameters = new { order_id = orderId };
            return PostAsync<BaselinkerOrderResponse>("getOrders", parameters);
        }

        public Task<BaselinkerOrderResponse> GetOrdersAsync()
        {
            long timestamp = ((DateTimeOffset)DateTime.Now.AddHours(-8)).ToUnixTimeSeconds();
            var parameters = new { date_confirmed_from = timestamp };
            return PostAsync<BaselinkerOrderResponse>("getOrders", parameters);
        }

        public Task<AddProductResponse> AddProductAsync(AddBaselinkerObject addBaselinkerObject)
        {
            return PostAsync<AddProductResponse>("addProduct", addBaselinkerObject);
        }

        public Task<UpdateInventoryProductsStockResponse> UpdateInventoryProductsStock(SyncInventory syncInventory)
        {
            return PostAsync<UpdateInventoryProductsStockResponse>("updateInventoryProductsStock", syncInventory);
        }

        public Task<InventoryWarehouseResponse> GetInventoryWarehousesAsync()
        {
            return PostAsync<InventoryWarehouseResponse>("getInventoryWarehouses", new { });
        }

        public Task<BaselinkerOrderStatusList> GetOrderStatusList()
        {
            return PostAsync<BaselinkerOrderStatusList>("getOrderStatusList", new { });
        }

        public Task<BaselinkerStoragesResponse> GetStoragesListAsync()
        {
            return PostAsync<BaselinkerStoragesResponse>("getStoragesList", new { });
        }

        public Task<InventoryResponse> GetInventoriesAsync()
        {
            return PostAsync<InventoryResponse>("getInventories", new { });
        }

        public Task<InventoryManufactureResponse> GetInventoryManufacturersAsync()
        {
            return PostAsync<InventoryManufactureResponse>("getInventoryManufacturers", new { });
        }

        public Task<InventoryPriceGroup> GetInventoryPriceGroupsAsync()
        {
            return PostAsync<InventoryPriceGroup>("getInventoryPriceGroups", new { });
        }

        public Task<CategoryResponse> GetCategoriesAsync(string storageId = null)
        {
            var parameters = new { storage_id = storageId };
            return PostAsync<CategoryResponse>("getCategories", parameters);
        }

        public async Task<InventoryProductListResponse> GetInventoryProductsListAsync(int inventoryId, int page = 1)
        {
            var parameters = new { inventory_id = inventoryId, page };
            var response = await PostAsync<dynamic>("getInventoryProductsList", parameters);

            var productList = new List<InventoryProduct>();
            if (response.products != null)
            {
                foreach (var productKey in response.products)
                {
                    var product = JsonConvert.DeserializeObject<InventoryProduct>(productKey.Value.ToString());
                    productList.Add(product);
                }
            }

            return new InventoryProductListResponse
            {
                status = response.status,
                products = productList,
                error_code = response.error_code ?? string.Empty,
                error_message = response.error_message ?? string.Empty
            };
        }

        public async Task UpdateOrderAsync(int orderId, Dictionary<string, string> postParams)
        {
            postParams.Add("order_id", orderId.ToString());
            await PostAsync<object>("setOrderFields", postParams);
        }
    }

    public interface IBaselinkerAdapter
    {
        Task<BaselinkerProductsListResponse> GetProductDataAsync(string productId);
        Task<BaselinkerProductsListResponse> GetProductsListAsync(int page = 1);
        Task<BaselinkerOrderResponse> GetOrderAsync(int orderId = 0);
        Task<BaselinkerStoragesResponse> GetStoragesListAsync();
        Task<InventoryResponse> GetInventoriesAsync();
    }
}
