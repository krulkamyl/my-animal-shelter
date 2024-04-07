using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BaselinkerSubiektConnector
{
    public partial class AddToBaselinker : Window
    {
        public AddToBaselinker()
        {
            InitializeComponent();
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
            string productName = ProductNameText.Text;
            string description = DescriptionText.Text;

            MessageBox.Show("Produkt dodany do Baselinker: " + productName);


            this.Close();
        }

    }
}
