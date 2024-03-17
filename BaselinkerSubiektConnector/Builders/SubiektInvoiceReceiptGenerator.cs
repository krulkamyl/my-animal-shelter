using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Repositories;
using BaselinkerSubiektConnector.Support;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Dokumenty.Logistyka;
using InsERT.Moria.Klienci;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.Sfera;
using InsERT.Mox.BusinessObjects;
using InsERT.Mox.ObiektyBiznesowe;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            try
            {
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

                if (this.blOrderResponse.orders[0].extra_field_1 == "#ZAIMPORTOWANE#")
                {
                    throw new Exception("Zamówienie zostało już zaimporowane do Subiekta.");
                }

                Console.WriteLine("Check customer exist");
                int receiptInvoice = 0;

                this.customer = null;
                while (this.customer == null)
                {
                    this.checkCustomerExist();
                    if (this.customer == null)
                    {
                        this.createCustomer();
                    }
                    else
                    {
                        this.checkCustomerHaveSameEmail();
                    }
                };

                Console.WriteLine("Found!: " + this.customer.NazwaSkrocona);

                if (this.customer.NIP.Length > 8)
                {
                    receiptInvoice = this.createInvoice();
                }
                else
                {
                    if (this.blOrderResponse.orders[0].want_invoice == "1")
                    {
                        receiptInvoice = this.createRetailInvoice();
                    }
                    else
                    {
                        receiptInvoice = this.createReceipt();
                    }
                }

                if (receiptInvoice > 0)
                {
                    // TODO Get invoice
                    IDokumentySprzedazy sdi = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IDokumentySprzedazy>();
                    var receiptInvoiceObj = sdi.Dane.Wszystkie().Where(pdm => pdm.Id == receiptInvoice).FirstOrDefault();
                    if (receiptInvoiceObj != null)
                    {
                        this.UpdateOrder(receiptInvoiceObj);
                    }

                    // TODO: add radiobox - send to e-mail
                    // TODO: add combobox - select department and warehouse
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

           

        }
        private async Task InitializeOrderResponseAsync()
        {
            this.blOrderResponse = await blAdapter.GetOrderAsync(baselinkerOrderId);
        }

        private Task UpdateOrder(DokumentDS receiptInvoice)
        {
            try
            {
                Console.WriteLine("update in baselinker");
                var data = new Dictionary<string, string>
            {
                {
                    "extra_field_1", "#ZAIMPORTOWANE#"
                },
                {
                    "extra_field_2", receiptInvoice.NumerWewnetrzny.PelnaSygnatura
                },
            };

                blAdapter.UpdateOrder(baselinkerOrderId, data);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine("problem save to baselinker");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return Task.CompletedTask;
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
                        throw new Exception(nowyPodmiot.DumpErrors());
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
                        {
                            Console.WriteLine("Email updated");
                        }
                        else
                        {
                            throw new Exception(customer.DumpErrors());
                        }
                       
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

        private int createReceipt()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy receipt = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzParagon())
                {
                    receipt.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    receipt.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;
                    Console.WriteLine("Added customer by ID");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Console.WriteLine("Searching product: " + orderItem.name);

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
                            PozycjaDokumentu invoiceitem = receipt.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);

                            receipt.Przelicz();

                            Console.WriteLine("make receipt count for position: " + asortymentId);
                        }
                        else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. EAN:" + orderItem.ean + " nazwa " + orderItem.name);
                        }
                    }


                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        Console.WriteLine("Add delivery position to invoice");
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Delivery item position not found");
                        }
                        PozycjaDokumentu deliveryItem = receipt.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        receipt.Przelicz();
                    }

                    receipt.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    Console.WriteLine("save receipt");
                    if (receipt.Zapisz())
                    {

                        Console.WriteLine($"Receipt number: {receipt.Dane.NumerWewnetrzny.PelnaSygnatura}.");
                        return receipt.Dane.Id;
                    }
                    else
                    {
                        throw new Exception(receipt.DumpErrors());
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save receipt");
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        private int createInvoice()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy invoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureSprzedazy())
                {
                    invoice.PodmiotyDokumentu.UstawNabywceWedlugNIP(Helpers.ExtractDigits(blResponseOrder.invoice_nip));
                    invoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    Console.WriteLine("Added company by NIP");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Console.WriteLine("Searching product: " + orderItem.name);

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
                            PozycjaDokumentu invoiceitem = invoice.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            invoice.Przelicz();

                            Console.WriteLine("make invoice count for position: " + asortymentId);
                        } else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. EAN:" + orderItem.ean + " nazwa " + orderItem.name);
                        }
                    }

                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        Console.WriteLine("Add delivery position to invoice");
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Delivery item position not found");
                        }
                        PozycjaDokumentu deliveryItem = invoice.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        invoice.Przelicz();
                    }

                    invoice.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    Console.WriteLine("save invoice");
                    if (invoice.Zapisz())
                    {
                        Console.WriteLine($"Invoice number: {invoice.Dane.NumerWewnetrzny.PelnaSygnatura}.");
                        return invoice.Dane.Id;
                    }
                    else
                    {
                        throw new Exception(invoice.DumpErrors());
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save invoice");
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        private int createRetailInvoice()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy retailInvoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureDetaliczna())
                {
                    retailInvoice.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    retailInvoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    Console.WriteLine("Added retail customer by id");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Console.WriteLine("Searching product: " + orderItem.name);

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
                            PozycjaDokumentu invoiceitem = retailInvoice.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            retailInvoice.Przelicz();

                        }
                        else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. EAN:" + orderItem.ean + " nazwa " + orderItem.name);
                        }
                    }

                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        Console.WriteLine("Add delivery position to invoice retail");
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Delivery item position not found");
                        }
                        PozycjaDokumentu deliveryItem = retailInvoice.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        retailInvoice.Przelicz();
                    }

                    retailInvoice.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    Console.WriteLine("save invoice");
                    if (retailInvoice.Zapisz())
                    {
                        Console.WriteLine($"Invoice number: {retailInvoice.Dane.NumerWewnetrzny.PelnaSygnatura}.");
                        return retailInvoice.Dane.Id;
                    }
                    else
                    {
                        throw new Exception(retailInvoice.DumpErrors());
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Problem with save invoice");
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

    }
    
    public static class DumpingErrors
    {
        internal static string DumpErrors(this IObiektBiznesowy obiektBiznesowy)
        {
            string errors = "";
            errors  += DumpErrors((IBusinessObject)obiektBiznesowy)+"\n";
            var uow = ((IGetUnitOfWork)obiektBiznesowy).UnitOfWork;
            foreach (var innyObiektBiznesowy in uow.Participants.OfType<IBusinessObject>().Where(bo => bo != obiektBiznesowy))
            {
                errors += DumpErrors(innyObiektBiznesowy) + "\n";
            }
            return errors;
        }

        internal static string DumpErrors(this IBusinessObject obiektBiznesowy)
        {
            var errors = "";
            foreach (var encjaZBledami in obiektBiznesowy.InvalidData)
            {
                foreach (var bladNaCalejEncji in encjaZBledami.Errors)
                {
                    errors += bladNaCalejEncji+"\n";
                    errors += " na encjach:" + encjaZBledami.GetType().Name +"\n";
                    errors += "\n";
                }
                foreach (var bladNaKonkretnychPolach in encjaZBledami.MemberErrors)
                {
                    errors += bladNaKonkretnychPolach.Key + "\n";
                    errors += "na polach:" + "\n";
                    errors += string.Join(", ", bladNaKonkretnychPolach.Select(b => encjaZBledami.GetType().Name + "." + b)) + "\n";
                    errors += "\n";
                }
            }
            return errors;
        }
    }
}
