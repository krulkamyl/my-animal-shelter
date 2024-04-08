using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Products
{
    public class CategoryResponse
    {
        public string status { get; set; }
        public string storage_id { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public List<Category> categories { get; set; }
    }

    public class Category
    {
        public int category_id { get; set; }
        public string name { get; set; }
        public int parent_id { get; set; }
    }
}
