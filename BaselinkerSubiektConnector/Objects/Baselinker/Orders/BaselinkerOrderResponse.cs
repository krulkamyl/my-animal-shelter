using System;
using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Orders
{
    public class BaselinkerOrderResponseOrder
    {
        public int? order_id { get; set; }
        public int? shop_order_id { get; set; }
        public string external_order_id { get; set; }
        public string order_source { get; set; }
        public int? order_source_id { get; set; }
        public string order_source_info { get; set; }
        public int? order_status_id { get; set; }
        public bool? confirmed { get; set; }
        public int? date_confirmed { get; set; }
        public int? date_add { get; set; }
        public int? date_in_status { get; set; }
        public string user_login { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string user_comments { get; set; }
        public string admin_comments { get; set; }
        public string currency { get; set; }
        public string payment_method { get; set; }
        public string payment_method_cod { get; set; }
        public double? payment_done { get; set; }
        public string delivery_method { get; set; }
        public double? delivery_price { get; set; }
        public string delivery_package_module { get; set; }
        public string delivery_package_nr { get; set; }
        public string delivery_fullname { get; set; }
        public string delivery_company { get; set; }
        public string delivery_address { get; set; }
        public string delivery_city { get; set; }
        public string delivery_state { get; set; }
        public string delivery_postcode { get; set; }
        public string delivery_country_code { get; set; }
        public string delivery_point_id { get; set; }
        public string delivery_point_name { get; set; }
        public string delivery_point_address { get; set; }
        public string delivery_point_postcode { get; set; }
        public string delivery_point_city { get; set; }
        public string invoice_fullname { get; set; }
        public string invoice_company { get; set; }
        public string invoice_nip { get; set; }
        public string invoice_address { get; set; }
        public string invoice_city { get; set; }
        public string invoice_state { get; set; }
        public string invoice_postcode { get; set; }
        public string invoice_country_code { get; set; }
        public string want_invoice { get; set; }
        public string extra_field_1 { get; set; }
        public string extra_field_2 { get; set; }
        public string order_page { get; set; }
        public int? pick_state { get; set; }
        public int? pack_state { get; set; }
        public string delivery_country { get; set; }
        public string invoice_country { get; set; }
        public List<BaselinkerOrderResponseOrderProduct> products { get; set; }
    }

    public class BaselinkerOrderResponseOrderProduct
    {
        public string storage { get; set; }
        public int? storage_id { get; set; }
        public int? order_product_id { get; set; }
        public string product_id { get; set; }
        public string variant_id { get; set; }
        public string name { get; set; }
        public string attributes { get; set; }
        public string sku { get; set; }
        public string ean { get; set; }
        public string location { get; set; }
        public int? warehouse_id { get; set; }
        public string auction_id { get; set; }
        public double? price_brutto { get; set; }
        public int? tax_rate { get; set; }
        public int? quantity { get; set; }
        public double? weight { get; set; }
        public int? bundle_id { get; set; }

        public decimal price_netto()
        {
            decimal netPrice = Convert.ToDecimal(this.price_brutto / (1 + (this.tax_rate / 100)));
            return Math.Round(netPrice, 2);
        }
    }

    public class BaselinkerOrderResponse
    {
        public string status { get; set; }
        public List<BaselinkerOrderResponseOrder> orders { get; set; }

        public string error_code { get; set; }
        public string error_message { get; set; }
    }

}
