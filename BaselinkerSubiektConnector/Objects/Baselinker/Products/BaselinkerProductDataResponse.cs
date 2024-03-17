using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Products
{
    public class BaselinkerProductDataResponse
    {
        public string status { get; set; }
        public string storage_id { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public Dictionary<string, BaselinkerProductDataResponseProductDetail> products { get; set; }
    }

    public class BaselinkerProductDataResponseProductDetail
    {
        public int? product_id { get; set; }
        public string ean { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public int? quantity { get; set; }
        public double? price_netto { get; set; }
        public int? price_brutto { get; set; }
        public int? price_wholesale_netto { get; set; }
        public int? tax_rate { get; set; }
        public double? weight { get; set; }
        public string man_name { get; set; }
        public object man_image { get; set; }
        public int? category_id { get; set; }
        public List<string> images { get; set; }
        public List<List<string>> features { get; set; }
        public List<object> variants { get; set; }
        public string description { get; set; }
        public object description_extra1 { get; set; }
        public object description_extra2 { get; set; }
        public object description_extra3 { get; set; }
        public object description_extra4 { get; set; }
    }
}
