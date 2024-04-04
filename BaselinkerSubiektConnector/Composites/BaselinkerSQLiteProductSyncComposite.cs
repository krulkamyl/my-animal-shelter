using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Repositories.SQLite;
using System.Collections.Generic;
using System.Windows.Controls;

namespace BaselinkerSubiektConnector.Composites
{
    public class BaselinkerSQLiteProductSyncComposite
    {
        public BaselinkerSQLiteProductSyncComposite() { }

        public async void Sync(TextBlock progressName, int inventory_id, ProgressBar progressBar)
        {

            BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey)
            );

            progressName.Text = "Zlecenie zostało przyjęte. Wykonywanie pętli.";

            List<InventoryProduct> allProducts = new List<InventoryProduct>();

            int length = 1000;
            int page = 1;

            while (length >= 1000)
            {
                progressName.Text = "Pobieranie strony: "+page.ToString();
                InventoryProductListResponse inventoryProductListResponse = await baselinkerAdapter.GetInventoryProductsListAsync(inventory_id, page);
                length = inventoryProductListResponse.products.Count;
                allProducts.AddRange(inventoryProductListResponse.products);
                page++;
            }
            progressName.Text = "Pobrano wszystkie produkty. Łączna ilość produktów w Baselinker: "+allProducts.Count.ToString();

        }
    }
}
