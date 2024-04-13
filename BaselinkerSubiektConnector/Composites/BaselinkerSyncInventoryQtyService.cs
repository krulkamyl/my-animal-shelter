using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BaselinkerSubiektConnector.Composites
{
    public class BaselinkerSyncInventoryQtyService
    {
        public static bool Sync()
        {
            MSSQLAdapter mSSQLAdapter = new MSSQLAdapter(
                ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password)
            );

            Record subiektInventory = SQLiteService.ReadRecords(
                SQLiteDatabaseNames.GetBaselinkerInventoriesDatabaseName()
            ).First();

            List<Record> assortments = AssortmentRepository.GetAssortmentConnectedWithSubiekt();

            SyncInventory inventory = new SyncInventory();
            inventory.inventory_id = subiektInventory.value;
            inventory.products = new Dictionary<string, Dictionary<string, int>>();

            string warehouseKey = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_InventoryWarehouseId);
            int productCount = 0;

            foreach (Record item in assortments)
            {
                int assortmentId = Convert.ToInt32(item.subiekt_id);
                int quantity = mSSQLAdapter.GetWarehouseAssortmentQuantity(
                    ConfigRepository.GetValue(key: RegistryConfigurationKeys.MSSQL_DB_NAME),
                    assortmentId,
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse)
                );

                inventory.products.Add(item.baselinker_id, new Dictionary<string, int>() { { warehouseKey, quantity } });

                productCount++; 
                if (productCount >= 995)
                {
                    SendRequest(inventory); 
                    inventory.products.Clear();
                    productCount = 0; 
                }
            }

            if (productCount > 0)
            {
                SendRequest(inventory); 
            }
            return true;
        }

        public async static void SendRequest(SyncInventory inventory)
        {
            BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey),
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId)
                );
            string json = JsonConvert.SerializeObject(inventory); 
            UpdateInventoryProductsStockResponse inventoryProductsStockResponse = await baselinkerAdapter.UpdateInventoryProductsStock(inventory);

            if (inventoryProductsStockResponse.status != "SUCCESS")
            {
                Helpers.Log("Błąd synchronizacji stanów magazynowych: "+ json);
            }
            baselinkerAdapter = null;


        }
    }

    public class SyncInventory
    {
        public string inventory_id { get; set; }
        public Dictionary<string, Dictionary<string, int>> products { get; set; }
    }
}
