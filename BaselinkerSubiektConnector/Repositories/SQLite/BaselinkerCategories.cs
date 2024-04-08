using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Repositories.SQLite
{
    public class BaselinkerCategories
    {
        public static void UpdateExistingData(List<Category> baselinkerCategories)
        {
            SQLiteService.DeleteRecords(
                SQLiteDatabaseNames.GetBaselinkerCategoriesDatabaseName()
            );

            foreach (Category category in baselinkerCategories)
            {
                SQLiteBaselinkerObject record = new SQLiteBaselinkerObject
                {
                    value = category.category_id.ToString(),
                    key = category.name,
                };

                SQLiteService.CreateRecord(
                    SQLiteDatabaseNames.GetBaselinkerCategoriesDatabaseName(),
                    record
                );
            }
        }
    }
}
