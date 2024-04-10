using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Products
{
    public class UpdateInventoryProductsStockResponse
    {
        public string status { get; set; }
        public int counter { get; set; }
        public AddBaselinkerObject warnings { get; set; }
    }
}
