using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using System.Collections.Generic;
using System.Linq;

namespace BaselinkerSubiektConnector.Composites
{
    public class GetMissingInBaselinkerSubiektProducts
    {
        public GetMissingInBaselinkerSubiektProducts() { }

        public List<Record> Sync(List<AssortmentTableItem> baselinkerProducts, MSSQLAdapter mSSQLAdapter)
        {
            List<Record> missingProducts = new List<Record>();

            List<Record> subiektProducts = mSSQLAdapter.GetAllAssortmentsFromWarehouse(
                ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse)
            );

            foreach (Record subiektProduct in subiektProducts)
            {
                AssortmentTableItem baselinkerProduct = baselinkerProducts.Where(bp => bp.Barcode == subiektProduct.ean_code).FirstOrDefault();
                if (baselinkerProduct == null)
                {
                    missingProducts.Add(subiektProduct);
                }
            }


            return missingProducts;
        }
    }
}
