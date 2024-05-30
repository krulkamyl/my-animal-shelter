using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Repositories.SQLite;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector.Builders.Baselinker
{
    public class FetchBaselinkerData
    {
        public static async Task<bool> GetDataAsync(string baselinkerApiKey, string storage_id = null, bool onlyCategories = false)
        {

            BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(baselinkerApiKey);

            if (storage_id != null || ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId) != null )
            {
                string storage = storage_id;
                if (storage == null)
                {
                    storage = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);
                }
                CategoryResponse baselinkerCategories = await baselinkerAdapter.GetCategoriesAsync(storage);
                BaselinkerCategories.UpdateExistingData(baselinkerCategories.categories);
            }


            if (onlyCategories)
            {
                return true;
            }


            BaselinkerStoragesResponse storagesList = await baselinkerAdapter.GetStoragesListAsync();
            BaselinkerStorages.UpdateExistingData(storagesList.storages);

            InventoryResponse inventoryResponse = await baselinkerAdapter.GetInventoriesAsync();
            BaselinkerIntentories.UpdateExistingData(inventoryResponse.inventories);

            InventoryManufactureResponse inventoryManufactureResponse = await baselinkerAdapter.GetInventoryManufacturersAsync();
            BaselinkerInventoryManufacturers.UpdateExistingData(inventoryManufactureResponse.manufacturers);

            InventoryPriceGroup inventoryPriceGroup = await baselinkerAdapter.GetInventoryPriceGroupsAsync();
            BaselinkerInventoryPriceGroups.UpdateExistingData(inventoryPriceGroup.price_groups);

            InventoryWarehouseResponse inventoryWarehouse = await baselinkerAdapter.GetInventoryWarehousesAsync();
            BaselinkerInventoryWarehouses.UpdateExistingData(inventoryWarehouse.warehouses);

            BaselinkerOrderStatusList orderStatusList = await baselinkerAdapter.GetOrderStatusList();
            BaselinkerOrderStatusListRepository.UpdateExistingData(orderStatusList.statuses);

            return true;
        }
    }
}
