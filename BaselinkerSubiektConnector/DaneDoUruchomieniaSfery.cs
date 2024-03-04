using InsERT.Moria.Sfera;
using InsERT.Mox.Product;

namespace BaselinkerSubiektConnector
{
    public class DaneDoUruchomieniaSfery
    {
        public DanePolaczenia DanePolaczenia { get; set; }
        public string Serwer { get; set; }
        public string Baza { get; set; }
        public string LoginSql { get; set; }
        public string HasloSql { get; set; }
        public string LoginNexo { get; set; }
        public string HasloNexo { get; set; }
        public ProductId Produkt { get; set; }
    }
}