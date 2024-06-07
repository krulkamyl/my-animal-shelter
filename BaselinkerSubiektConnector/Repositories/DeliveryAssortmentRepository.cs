using System;
using System.Linq;
using BaselinkerSubiektConnector.Builders;
using BaselinkerSubiektConnector.Support;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Sfera;

namespace BaselinkerSubiektConnector.Repositories
{
    public class DeliveryAssortmentRepository
    {
        private const string AssortmentSymbol = "DOSTAWA_ECOMMERCE";
        private readonly MainWindowViewModel _sfera;
        private IAsortymenty _asortymenty;

        public DeliveryAssortmentRepository(MainWindowViewModel sfera)
        {
            _sfera = sfera;
        }

        public InsERT.Moria.ModelDanych.Asortyment GetAssortment()
        {
            _asortymenty = _sfera.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
            Helpers.Log("DeliveryAssortmentRepository Get()");

            try
            {
                var assortment = _asortymenty.Dane.Wszystkie().FirstOrDefault(k => k.Symbol == AssortmentSymbol);

                if (assortment != null)
                {
                    Helpers.Log($"ASSORTMENT FOUND! : {assortment.Symbol}");
                    return assortment;
                }

                return CreateAssortment();
            }
            catch (Exception ex)
            {
                Helpers.Log("ASSORTMENT FAIL");
                Helpers.Log(ex.Message);
                return null;
            }
        }

        public InsERT.Moria.ModelDanych.Asortyment CreateAssortment()
        {
            Helpers.Log("DeliveryAssortmentRepository Create()");

            try
            {
                var jednostkiMiar = _sfera.UchwytDoSfery.JednostkiMiar();
                var walutyDD = _sfera.UchwytDoSfery.Waluty().DaneDomyslne;
                var jednostkaMiary = jednostkiMiar.Dane.WyszukajPoSymbolu("usl") ?? CreateJednostkaMiary(jednostkiMiar);

                using (var assortment = _asortymenty.Utworz())
                {
                    var szablony = _sfera.UchwytDoSfery.SzablonyAsortymentu();
                    assortment.WypelnijNaPodstawieSzablonu(szablony.DaneDomyslne.Usluga);

                    assortment.Dane.Symbol = AssortmentSymbol;
                    assortment.Dane.Nazwa = "Dostawa";
                    assortment.JednostkiMiary.UstawPodstawowaJednostkeMiary(jednostkaMiary);

                    assortment.Dane.CenaEwidencyjna = 12.30m;
                    assortment.Dane.WalutaCenyEwidencyjnej = walutyDD.PLN;
                    UstawCeny(assortment);

                    assortment.Dane.StronaWWW = "https://nexolink.pl";
                    assortment.Dane.Opis = "Nie usuwać! Wykorzystywane przez program połączeniowy z Baselinker";

                    if (!assortment.Zapisz())
                    {
                        throw new Exception(assortment.DumpErrors());
                    }
                    return GetAssortment();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
                return null;
            }
        }

        private InsERT.Moria.ModelDanych.JednostkaMiary CreateJednostkaMiary(IJednostkiMiar jednostkiMiar)
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
                return jednostkaMiaryBO.Dane;
            }
        }

        private void UstawCeny(IAsortyment assortment)
        {
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
        }
    }
}
