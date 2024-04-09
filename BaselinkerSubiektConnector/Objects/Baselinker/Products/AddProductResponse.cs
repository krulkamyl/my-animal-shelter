using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Products
{
    public class AddProductResponse
    {
        public string status { get; set; }
        public string storage_id { get; set; }
        public int product_id { get; set; }
        public AddBaselinkerObject warnings { get; set; }
    }
}
