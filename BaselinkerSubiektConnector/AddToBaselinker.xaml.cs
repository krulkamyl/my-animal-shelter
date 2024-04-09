using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace BaselinkerSubiektConnector
{
    public partial class AddToBaselinker : Window
    {
        public int qty = 0;
        public string subiekt_id = null;
        public AssortmentTableItem itemObject = null;

        public AddToBaselinker(AssortmentTableItem item)
        {
            InitializeComponent();
            itemObject = item;

            List<string> vats = new List<string>();
            vats.Add("23");
            vats.Add("5");
            vats.Add("8");
            vats.Add("0");

            foreach (string vat in vats)
            {
                VatSelect.Items.Add(vat);
            }
            VatSelect.SelectedItem = "23";

            AddBaselinkerHeader.Text = "Dodawanie produktu: " + item.SubiektSymbol;
            ProductNameText.Text = item.SubiektName;
            SKUText.Text = item.SubiektSymbol;
            EANText.Text = item.Barcode;
            DescriptionText.Text = item.SubiektDescription;

            subiekt_id = item.SubiektId;


            WeightText.Text = "0.00";

            LoadItemsAndSetDefault(CatalogSelect, SQLiteDatabaseNames.GetBaselinkerInventoriesDatabaseName());
            LoadItemsAndSetDefault(ManufacturerSelect, SQLiteDatabaseNames.GetBaselinkerInventoryManufacturersName());
            LoadItemsAndSetDefault(CategorySelect, SQLiteDatabaseNames.GetBaselinkerCategoriesDatabaseName());
            CatalogSelect.SelectedIndex = 0;
            ManufacturerSelect.SelectedIndex = 0;
            CategorySelect.SelectedIndex = 0;

            string qtyString = item.SubiektQty;

            qtyString = qtyString.Replace(".", ",");

            if (decimal.TryParse(qtyString, out decimal qtyOut))
            {
                qty = Convert.ToInt32(qtyOut);
            }

            string price = item.SubiektPrice;


            if (decimal.TryParse(price, out decimal priceOut))
            {
                PriceText.Text = priceOut.ToString("F").Replace(",", ".");
            }

            PriceText.TextChanged += PriceText_TextChanged;
            WeightText.TextChanged += WeightText_TextChanged;

        }


        private async void AddToBaselinker_Click(object sender, RoutedEventArgs e)
        {
            BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey), ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId));
            try
            {
                Record category = SQLiteService.ReadRecord(SQLiteDatabaseNames.GetBaselinkerCategoriesDatabaseName(), "key", CategorySelect.Text);

                AddBaselinkerObject addBaselinkerObject = new AddBaselinkerObject();
                addBaselinkerObject.storage_id = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);
                addBaselinkerObject.ean = EANText.Text;
                addBaselinkerObject.sku = SKUText.Text;
                addBaselinkerObject.tax_rate = VatSelect.Text;
                addBaselinkerObject.name = ProductNameText.Text;
                addBaselinkerObject.description = DescriptionText.Text;
                addBaselinkerObject.man_name = ManufacturerSelect.Text;
                addBaselinkerObject.price_brutto = PriceText.Text;
                addBaselinkerObject.category_id = category.value;
                addBaselinkerObject.quantity = qty;


                AddProductResponse addProductResponse = await baselinkerAdapter.AddProductAsync(addBaselinkerObject);

                if (addProductResponse.status == "SUCCESS")
                {
                    SQLiteAssortmentObject assortmentObject = new SQLiteAssortmentObject();
                    
                        assortmentObject.ean_code = EANText.Text;
                        assortmentObject.baselinker_id = addProductResponse.product_id.ToString();
                        assortmentObject.baselinker_name = ProductNameText.Text;
                        assortmentObject.subiekt_id = subiekt_id;
                        assortmentObject.subiekt_symbol = itemObject.SubiektSymbol;
                        assortmentObject.subiekt_name = itemObject.SubiektName;

                    AssortmentRepository.UpdateOrCreateRecord("ean_code", EANText.Text, assortmentObject);

                    MessageBox.Show("Product został dodany do Baselinker.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    throw new Exception(JsonConvert.SerializeObject(addProductResponse.warnings));
                }


            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Wystąpił błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadItemsAndSetDefault(System.Windows.Controls.ComboBox comboBox, string databaseName, string registryKey = null)
        {
            List<Record> records = SQLiteService.ReadRecords(databaseName);

            if (records.Count > 0)
            {
                comboBox.Items.Clear();
                foreach (Record record in records)
                {
                    comboBox.Items.Add(record.key);
                }
            }

            if (registryKey != null)
            {
                var defaultValue = ConfigRepository.GetValue(registryKey);
                if (!string.IsNullOrEmpty(defaultValue) && defaultValue.Length > 1)
                {
                    comboBox.SelectedItem = defaultValue;
                }
            }
        }

        private void PriceText_TextChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(PriceText.Text, @"^\d*\.?\d{0,2}$"))
            {
                PriceText.Text = PriceText.Text.Substring(0, PriceText.Text.Length - 1);
                PriceText.SelectionStart = PriceText.Text.Length;
            }
        }

        private void WeightText_TextChanged(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(WeightText.Text, @"^\d*\.?\d{0,2}$"))
            {
                WeightText.Text = WeightText.Text.Substring(0, WeightText.Text.Length - 1);
                WeightText.SelectionStart = WeightText.Text.Length;
            }
        }
    }

    public class AddBaselinkerObject
    {

        public string storage_id { get; set; }
        public string ean { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public string price_brutto { get; set; }
        public string tax_rate { get; set; }
        public string weight { get; set; }
        public string category_id { get; set; }
        public string man_name { get; set; }
    }
}
