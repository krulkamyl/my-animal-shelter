using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BaselinkerSubiektConnector.Composites
{
    public class BaselinkerSQLiteProductSyncComposite
    {
        public BaselinkerSQLiteProductSyncComposite() { }

        public async Task Sync(int inventory_id)
        {

            BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey)
            );

            MSSQLAdapter mSSQLAdapter = new MSSQLAdapter(
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password)
                );


            List<InventoryProduct> allProducts = new List<InventoryProduct>();

            int length = 1000;
            int page = 1;

            while (length >= 1000)
            {
                InventoryProductListResponse inventoryProductListResponse = await baselinkerAdapter.GetInventoryProductsListAsync(inventory_id, page);
                length = inventoryProductListResponse.products.Count;
                allProducts.AddRange(inventoryProductListResponse.products);
                page++;
            }

            int noEansBaselinker = 0;
            int index = 1;

            foreach (InventoryProduct inventoryProduct in allProducts)
            {
                if (inventoryProduct.ean != null)
                {
                    Record record = AssortmentRepository.GetRecordByEan(inventoryProduct.ean);
                    SQLiteAssortmentObject assortmentObject = new SQLiteAssortmentObject();
                    if (record != null)
                    {
                        assortmentObject.id = record.id;
                        assortmentObject.ean_code = record.ean_code;
                        assortmentObject.baselinker_id = record.baselinker_id;
                        assortmentObject.baselinker_name = record.baselinker_name;
                        assortmentObject.subiekt_id = record.subiekt_id;
                        assortmentObject.subiekt_symbol = record.subiekt_symbol;
                        assortmentObject.subiekt_name = record.subiekt_name;
                    }
                    else
                    {
                        assortmentObject.ean_code = inventoryProduct.ean;
                        assortmentObject.baselinker_id = inventoryProduct.id.ToString();
                        assortmentObject.baselinker_name = inventoryProduct.name;
                    }

                    Record assortment = mSSQLAdapter.GetRecordFromEan(
                        ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                        inventoryProduct.ean
                    );

                    if (assortment.subiekt_id != null)
                    {
                        assortmentObject.subiekt_id = assortment.subiekt_id;
                        assortmentObject.subiekt_symbol = assortment.subiekt_symbol;
                        assortmentObject.subiekt_name = assortment.subiekt_name;
                    }

                    AssortmentRepository.UpdateOrCreateRecord("ean_code", inventoryProduct.ean, assortmentObject);

                }
                else
                {
                    noEansBaselinker++;
                }

                index++;
            }
        }
    }
}
