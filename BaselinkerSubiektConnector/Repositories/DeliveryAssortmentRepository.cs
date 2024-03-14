using System;
using System.Linq;
using BaselinkerSubiektConnector.Builders;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Sfera;

namespace BaselinkerSubiektConnector.Repositories
{
    public class DeliveryAssortmentRepository
    {
        public MainWindowViewModel sfera { get; set; }
        public IAsortymenty asortymenty { get; set; }

        public const string ASSORTMENT_SYMBOL = "DOSTAWA_ECOMMERCE";

        public DeliveryAssortmentRepository(MainWindowViewModel sfera)
        {
            this.sfera = sfera;

        }

        public InsERT.Moria.ModelDanych.Asortyment GetAssortment()
        {
            this.asortymenty = this.sfera.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
            Console.WriteLine("DeliveryAssortmentRepository Get()");
            try
            {
                InsERT.Moria.ModelDanych.Asortyment assortment = this.asortymenty.Dane.Wszystkie().Where(k => k.Symbol == ASSORTMENT_SYMBOL).FirstOrDefault();
                if (assortment != null)
                {
                    Console.WriteLine("ASSORTMENT FOUND! : "+ assortment.Symbol);
                    return assortment;
                }
                var assortmentCreated = this.CreateAssortment();

                return assortmentCreated;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ASSORTMENT FAIL");
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public InsERT.Moria.ModelDanych.Asortyment CreateAssortment()
        {
            Console.WriteLine("DeliveryAssortmentRepository Create()");
            try
            {
                var jednostkiMiar = this.sfera.UchwytDoSfery.JednostkiMiar();
                var walutyDD = this.sfera.UchwytDoSfery.Waluty().DaneDomyslne;

                var jednostkaMiarySzt = jednostkiMiar.Dane.WyszukajPoSymbolu("szt");
                var jednostkaMiary = jednostkiMiar.Dane.WyszukajPoSymbolu("usl");
                if (jednostkaMiary == null)
                {
                    using (var jednostkaMiaryBO = jednostkiMiar.Utworz())
                    {
                        jednostkaMiaryBO.Dane.Symbol = "usl";
                        jednostkaMiaryBO.Dane.Nazwa = "Usługa";
                        jednostkaMiaryBO.Dane.Precyzja = 0;
                        jednostkaMiaryBO.Dane.Typ = jednostkiMiar.DaneDomyslne.Usluga.Typ;
                        jednostkaMiaryBO.UstawPrzelicznikDoJednostkiPodstawowej(12.0m, 1.0m, jednostkiMiar.DaneDomyslne.Sztuka);
                        if (!jednostkaMiaryBO.Zapisz())
                        {
                            throw new Exception(jednostkaMiaryBO.DumpErrors());
                        }
                        jednostkaMiary = jednostkaMiaryBO.Dane;
                    }
                }

                using (IAsortyment assortment = this.asortymenty.Utworz())
                {
                    var szablony = this.sfera.UchwytDoSfery.SzablonyAsortymentu();
                    assortment.WypelnijNaPodstawieSzablonu(szablony.DaneDomyslne.Usluga);


                    assortment.Dane.Symbol = ASSORTMENT_SYMBOL;
                    assortment.Dane.Nazwa = "Dostawa";

                    assortment.JednostkiMiary.UstawPodstawowaJednostkeMiary(jednostkaMiary);


                    assortment.Dane.CenaEwidencyjna = 12.30m;
                    assortment.Dane.WalutaCenyEwidencyjnej = walutyDD.PLN;

                    foreach (var poz in assortment.Dane.PozycjeCennika)
                    {
                        poz.CenaKalkulacyjna = assortment.Dane.CenaEwidencyjna;
                        switch (poz.Cennik.PoziomCen.Nazwa)
                        {
                            case "Detaliczny":
                                poz.CenaBrutto = 12.30m;
                                break;
                            case "Hurtowy":
                                poz.CenaNetto = 12.30m;
                                break;
                        }
                    }
                    assortment.Dane.StronaWWW = "https://cichy.cloud";
                    assortment.Dane.Opis = "Nie usuwać! Wykorzystywane przez program połączeniowy z Baselinker";

                    if (!assortment.Zapisz())
                    {
                        throw new Exception(assortment.DumpErrors());
                    }
                    return this.GetAssortment();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
    }
}
