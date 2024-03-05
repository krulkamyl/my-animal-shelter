using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Products
{
    public class BaselinkerProductsListResponse
    {
        public string status { get; set; }
        public string storage_id { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public List<BaselinkerProductsListResponseProduct> products { get; set; }
    }

    public class BaselinkerProductsListResponseProduct
    {
        public string product_id { get; set; }
        public string ean { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public int? quantity { get; set; }
        public double? price_brutto { get; set; }
    }
}
