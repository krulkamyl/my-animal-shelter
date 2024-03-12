using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.Baselinker.Products;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Support;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Dokumenty.Logistyka;
using InsERT.Moria.Klienci;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.ModelOrganizacyjny;
using InsERT.Moria.Narzedzia.EPP.Typy;
using InsERT.Moria.Sfera;
using InsERT.Mox.BusinessObjects;
using InsERT.Mox.ObiektyBiznesowe;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BaselinkerSubiektConnector.Builders
{
    public class SubiektInvoiceReceiptBuilder
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();
        private int baselinkerOrderId;
        private BaselinkerAdapter blAdapter;
        public MainWindowViewModel mainWindowViewModel;
        private BaselinkerOrderResponse blOrderResponse;
        private Podmiot customer;
        private MSSQLAdapter mssqlAdapter;

        public SubiektInvoiceReceiptBuilder(int baselinkerOrderId, MainWindowViewModel mainWindowViewModel)
        {
            this.baselinkerOrderId = baselinkerOrderId;
            this.mainWindowViewModel = mainWindowViewModel;



            this.mssqlAdapter = new MSSQLAdapter(
                SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Password)
            );

            string blApiKey = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
            string storageId = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);

            this.blAdapter = new BaselinkerAdapter(blApiKey, storageId);
            InitializeOrderResponseAsync().Wait();
            Console.WriteLine(JsonConvert.SerializeObject(this.blOrderResponse));
            Console.WriteLine("Check customer exist");
            this.customer = null;
            while (this.customer == null)
            {
                this.checkCustomerExist();
                if (this.customer == null)
                {
                    this.createCustomer();
                } else
                {
                    this.checkCustomerHaveSameEmail();
                }
            };

            Console.WriteLine("Found!: " + this.customer.NazwaSkrocona);

            if (this.customer.NIP.Length > 8)
            {
                this.createInvoice();
            }

            // TODO: add to baselinker sell document number

        }
        private async Task InitializeOrderResponseAsync()
        {
            this.blOrderResponse = await blAdapter.GetOrderAsync(baselinkerOrderId);
        }

        private Podmiot checkCustomerExist()
        {
            IPodmioty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IPodmioty>();
            BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
            if (blResponseOrder.invoice_nip.Length > 9)
            {
                this.customer = podmioty.Dane.Wszystkie().Where(pdm => pdm.NIP == blResponseOrder.invoice_nip).FirstOrDefault();
                if (this.customer != null)
                {
                    Console.WriteLine("Client exist - company");
                    return this.customer;
                }
            }
            this.customer = podmioty.Dane.Wszystkie().Where(pdm => pdm.NazwaSkrocona == blResponseOrder.invoice_fullname)
                .Where(pdm => pdm.Telefon == blResponseOrder.phone).FirstOrDefault();

            if (this.customer != null)
            {
                Console.WriteLine("Client exist - person");
                return this.customer;
            }
            Console.WriteLine("Client not exist");
            return null;
        }


        private void createCustomer()
        {
            IPodmioty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IPodmioty>();
            ITypyAdresu typyAdresu = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<ITypyAdresu>();
            IRodzajeKontaktu rodzajeKontaktu = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IRodzajeKontaktu>();
            PolaWlasnePodmiot polaWlasnePodmiot = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<PolaWlasnePodmiot>();
            BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
            string nip = blResponseOrder.invoice_nip;
            IPodmiot nowyPodmiot = null;
            if (nip.Length > 9)
            {
                Console.WriteLine("Client not exist - create company");
                nowyPodmiot = podmioty.UtworzFirme();
            }
            else
            {
                Console.WriteLine("Client not exist - create person");
                nowyPodmiot = podmioty.UtworzOsobe();
            }
            try
            {
                using (nowyPodmiot)
                {
                    AdresPodmiotu adresGlowny = null;
                    AdresPodmiotu adresDostaw = null;
                    nowyPodmiot.AutoSymbol();
                    string name = blResponseOrder.invoice_fullname;
                    if (name.Length < 2)
                    {
                        name = blResponseOrder.invoice_company;
                    }
                    nowyPodmiot.Dane.NazwaSkrocona = name;
                    if (nip.Length > 8)
                    {
                        nowyPodmiot.Dane.Firma.Nazwa = blResponseOrder.invoice_company;
                        nowyPodmiot.Dane.NIPSformatowany = nip;
                    } else
                    {
                        string[] split = blResponseOrder.invoice_fullname.Split(' ');
                        nowyPodmiot.Dane.Osoba.Nazwisko = split[split.Length - 1];
                        nowyPodmiot.Dane.Osoba.Imie = string.Join(" ", split, 0, split.Length - 1);
                    }
                    nowyPodmiot.Dane.Telefon = blResponseOrder.phone;

                    if (nowyPodmiot.Dane.AdresPodstawowy == null)
                        adresGlowny = nowyPodmiot.DodajAdres(typyAdresu.DaneDomyslne.Glowny);
                    else
                        adresGlowny = nowyPodmiot.Dane.AdresPodstawowy;

                    adresGlowny.Szczegoly.Ulica = blResponseOrder.invoice_address;
                    adresGlowny.Szczegoly.KodPocztowy = blResponseOrder.invoice_postcode;
                    adresGlowny.Szczegoly.Miejscowosc = blResponseOrder.invoice_city;
                    adresDostaw = nowyPodmiot.DodajAdres(typyAdresu.DaneDomyslne.DoWysylki);
                    adresDostaw.Nazwa = blResponseOrder.delivery_fullname.Length > 5 ? blResponseOrder.delivery_fullname : blResponseOrder.delivery_company;
                    adresDostaw.Szczegoly.Ulica = blResponseOrder.delivery_address;
                    adresDostaw.Szczegoly.KodPocztowy = blResponseOrder.delivery_postcode;
                    adresDostaw.Szczegoly.Miejscowosc = blResponseOrder.delivery_city;

                    Kontakt email = new Kontakt();
                    nowyPodmiot.Dane.Kontakty.Add(email);
                    email.Rodzaj = rodzajeKontaktu.DaneDomyslne.Email;
                    email.Wartosc = blResponseOrder.email;
                    email.Podstawowy = true;


                    Kontakt phone = new Kontakt();
                    nowyPodmiot.Dane.Kontakty.Add(phone);
                    phone.Rodzaj = rodzajeKontaktu.DaneDomyslne.Telefon;
                    phone.Wartosc = blResponseOrder.phone;
                    phone.Podstawowy = true;

                    if (nowyPodmiot.Zapisz())
                    {
                        Console.WriteLine("Customer created: " + name);
                    }

                    else
                    {
                        nowyPodmiot.WypiszBledy();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save user");
                Console.WriteLine(ex.Message);
            }
        }

        private void checkCustomerHaveSameEmail()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                IPodmioty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IPodmioty>();
                IRodzajeKontaktu rodzajeKontaktu = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IRodzajeKontaktu>();
                bool emailFound = false;
                foreach (Podmiot podmiot in podmioty.Dane.Wszystkie())
                {
                    if (podmiot.Kontakty.Where(k => k.Wartosc == blResponseOrder.email).Where(k => k.Podmiot_Id == this.customer.Id).FirstOrDefault() != null)
                    {
                        emailFound = true;
                        Console.WriteLine("Email found!: " + blResponseOrder.email);
                    }
                }
                if (!emailFound)
                {
                    Console.WriteLine("E-mail is diffrent: " + blResponseOrder.email);
                    using (IPodmiot customer = podmioty.Znajdz(this.customer))
                    {
                        Kontakt newEmail = new Kontakt();
                        customer.Dane.Kontakty.Add(newEmail);
                        newEmail.Rodzaj = rodzajeKontaktu.DaneDomyslne.Email;
                        newEmail.Wartosc = blResponseOrder.email;
                        newEmail.Podstawowy = true;
                        if (customer.Zapisz())
                            Console.WriteLine("Email updated");
                        else
                            customer.WypiszBledy();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save user");
                Console.WriteLine(ex.Message);
            }
        }

        private bool checkReceiptInvoiceExist()
        {
            return false;
        }

        private void createReceipt()
        {

        }

        private void createInvoice()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy invoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureSprzedazy())
                {
                    invoice.PodmiotyDokumentu.UstawNabywceWedlugNIP(Helpers.ExtractDigits(blResponseOrder.invoice_nip));
                    Console.WriteLine("Added customer by NIP");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Console.WriteLine("Searching product: " + orderItem.name);
                        Console.WriteLine("Test hot reload #2");

                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        Console.WriteLine("Assortiment found in DB: " + asortymentId);
                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            Console.WriteLine("Assortiment Found in Subiekt: " + asortymentId);
                            Cena cena = new Cena();
                            cena.BruttoPoRabacie = Convert.ToDecimal(orderItem.price_brutto);
                            cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            cena.NettoPrzedRabatem = orderItem.price_netto();
                            cena.NettoPoRabacie = orderItem.price_netto();
                            Console.WriteLine("Created pricing for: " + asortymentId);

                            PozycjaDokumentu pozycjaDokumentu = new PozycjaDokumentu();
                            pozycjaDokumentu.AsortymentAktualny = asortyment;
                            var pozycjaProdukt = invoice.Pozycje.Dodaj(asortyment.Symbol, Convert.ToDecimal(orderItem.quantity));
                            pozycjaProdukt.Cena = cena;
                            invoice.Pozycje.AktualizujAsortyment(pozycjaProdukt);

                            invoice.Przelicz();

                            Console.WriteLine("make invoice count for position: " + asortymentId);
                        } else
                        {
                            Console.WriteLine("Assortiment not found in Subiekt: " + asortymentId);
                            // TODO: exception not found product
                        }
                    }

                    invoice.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    Console.WriteLine("save invoice");
                    if (invoice.Zapisz())
                    {
                        Console.WriteLine($"Invoice number: {invoice.Dane.NumerWewnetrzny.PelnaSygnatura}.");
                    }
                    else
                    {
                        invoice.WypiszBledy();
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save invoice");
                Console.WriteLine(ex.Message);
            }
        }

    }

    public static class DumpingErrors
    {
        internal static void WypiszBledy(this IObiektBiznesowy obiektBiznesowy)
        {
            WypiszBledy((IBusinessObject)obiektBiznesowy);
            var uow = ((IGetUnitOfWork)obiektBiznesowy).UnitOfWork;
            foreach (var innyObiektBiznesowy in uow.Participants.OfType<IBusinessObject>().Where(bo => bo != obiektBiznesowy))
            {
                WypiszBledy(innyObiektBiznesowy);
            }
        }

        internal static void WypiszBledy(this IBusinessObject obiektBiznesowy)
        {
            foreach (var encjaZBledami in obiektBiznesowy.InvalidData)
            {
                foreach (var bladNaCalejEncji in encjaZBledami.Errors)
                {
                    Console.Error.WriteLine(bladNaCalejEncji);
                    Console.Error.WriteLine(" na encjach:" + encjaZBledami.GetType().Name);
                    Console.Error.WriteLine();
                }
                foreach (var bladNaKonkretnychPolach in encjaZBledami.MemberErrors)
                {
                    Console.Error.WriteLine(bladNaKonkretnychPolach.Key);
                    Console.Error.WriteLine(" na polach:");
                    Console.Error.WriteLine(string.Join(", ", bladNaKonkretnychPolach.Select(b => encjaZBledami.GetType().Name + "." + b)));
                    Console.Error.WriteLine();
                }
            }
        }
    }
}
