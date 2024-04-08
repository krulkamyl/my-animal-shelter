using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System;
using System.Collections.Generic;
using System.Windows;

namespace BaselinkerSubiektConnector
{
    public partial class AddToBaselinker : Window
    {
        public AddToBaselinker()
        {
            InitializeComponent();

            LoadItemsAndSetDefault(CatalogSelect, SQLiteDatabaseNames.GetBaselinkerInventoriesDatabaseName());
            LoadItemsAndSetDefault(ManufacturerSelect, SQLiteDatabaseNames.GetBaselinkerInventoryManufacturersName());
            LoadItemsAndSetDefault(ManufacturerSelect, SQLiteDatabaseNames.GetBaselinkerInventoryManufacturersName());
            LoadItemsAndSetDefault(PriceGroupSelect, SQLiteDatabaseNames.GetBaselinkerInventoryPriceGroupsName());
            LoadItemsAndSetDefault(CategorySelect, SQLiteDatabaseNames.GetBaselinkerCategoriesDatabaseName());
        }


        private void AddToBaselinker_Click(object sender, RoutedEventArgs e)
        {

            string catalog = CatalogSelect.Text;
            string ean = EANText.Text;
            string sku = SKUText.Text;
            string vat = VATText.Text;
            string weight = WeightText.Text;
            string width = WidthText.Text;
            string height = HeightText.Text;
            string length = LengthText.Text;
            string price = PriceText.Text;
            string manufacturer = ManufacturerSelect.Text;
            string category = CategorySelect.Text;
            string priceGroup = PriceGroupSelect.Text;
            string productName = ProductNameText.Text;
            string description = DescriptionText.Text;

            MessageBox.Show("Produkt dodany do Baselinker: " + productName);


            this.Close();
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
    }
}
