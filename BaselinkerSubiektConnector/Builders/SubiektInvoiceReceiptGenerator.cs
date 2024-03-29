using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Repositories;
using BaselinkerSubiektConnector.Support;
using InsERT.Moria.Archiwa;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Dokumenty.Logistyka;
using InsERT.Moria.Klienci;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.ModelOrganizacyjny;
using InsERT.Moria.Sfera;
using InsERT.Moria.Wydruki;
using InsERT.Mox.BusinessObjects;
using InsERT.Mox.ObiektyBiznesowe;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using Microsoft.Win32;
using Spire.Pdf;
using System.Threading;
using BaselinkerSubiektConnector.Services.EmailService;
using InsERT.Moria.Urzadzenia;
using InsERT.Moria.Urzadzenia.Core;

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
        private string documentType;

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
                Helpers.Log(JsonConvert.SerializeObject(this.blOrderResponse));

                if (this.blOrderResponse.orders[0].extra_field_1 == "#ZAIMPORTOWANE#")
                {
                    throw new Exception("Zamówienie zostało już zaimporowane do Subiekta.");
                }

                Helpers.Log("Check customer exist");
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

                Helpers.Log("Found!: " + this.customer.NazwaSkrocona);

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
                    IDokumentySprzedazy sdi = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IDokumentySprzedazy>();
                    var receiptInvoiceObj = sdi.Dane.Wszystkie().Where(pdm => pdm.Id == receiptInvoice).FirstOrDefault();
                    if (receiptInvoiceObj != null)
                    {
                        //this.UpdateOrder(receiptInvoiceObj);


                        if (this.documentType == "FS" || this.documentType == "FD")
                        {
                            this.SavePrintInvoiceAndSendEmail(receiptInvoiceObj);
                        }
                        else if((this.documentType == "PA" || this.documentType == "PF") && SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterEnabled) == "1")
                        {
                            this.PrintFiscalReceipt(receiptInvoiceObj);
                        }
                    }

                    // TODO: add radiobox - send to e-mail
                    // TODO: add combobox - select department and warehouse

                }
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
            }
        }

        private void SavePrintInvoiceAndSendEmail(DokumentDS receiptInvoiceObj)
        {
            Helpers.Log("saveInvoiceWithPrint");

            IWydruki wydruki = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IWydruki>();
            IDokumentySprzedazy dokumentySprzedazy = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IDokumentySprzedazy>();
            DokumentDS dokumentSprzedazy = dokumentySprzedazy.Dane.Wszystkie().Where(ds => ds.Id == receiptInvoiceObj.Id).FirstOrDefault();

            using (IDokumentSprzedazy ds = dokumentySprzedazy.Znajdz(dokumentSprzedazy))
            {
                InsERT.Moria.Wydruki.Enums.TypWzorcaWydruku typWzorca = InsERT.Moria.Wydruki.Enums.TypWzorcaWydruku.FakturaSprzedazy;
                if (this.documentType == "FD")
                {
                    typWzorca = InsERT.Moria.Wydruki.Enums.TypWzorcaWydruku.FakturaDetaliczna;
                }
                using (IWydruk printDoc = wydruki.Utworz(typWzorca))
                {

                    var fileName = receiptInvoiceObj.NumerWewnetrzny.PelnaSygnatura.Replace("/", "_");
                    printDoc.ParametryDrukowania.WybranyWzorzec = printDoc.ParametryDrukowania.DostepneWzorce.FirstOrDefault(w => w.Domyslny);

                    printDoc.ObiektDoWydruku = ds.Dane;
                    printDoc.ParametryDrukowania.NazwaDokumentuUzytkownika = fileName;
                    printDoc.ParametryDrukowania.SciezkaEksportu = Helpers.GetExportApplicationPath() + "\\";

                    try
                    {
                        printDoc.Eksport();
                        if (!printDoc.OstatniaOperacjaZakonczonaSukcesem)
                        {
                            throw new Exception(printDoc.PobierzListeBledow().First());
                        }

                        var filepath = Helpers.GetExportApplicationPath() + "\\" + fileName + ".pdf";

                        if (SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_PrinterEnabled) == "1"
                            && SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName).Length > 3)
                        {
                            int milliseconds = 2000;
                            Thread.Sleep(milliseconds);
                            PdfDocument pdfdocument = new PdfDocument();
                            Helpers.Log(filepath);
                            pdfdocument.LoadFromFile(filepath);
                            pdfdocument.PrintSettings.PrinterName = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName);
                            pdfdocument.Print();
                            pdfdocument.Dispose();
                        }

                        BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                        if (SharedRegistryManager.GetValue(RegistryConfigurationKeys.Config_EmailSendAuto) == "1"
                            && blResponseOrder.email.Length > 3 
                            && blResponseOrder.email.Contains("@")
                            )
                        {
                            Helpers.Log("Send email to: " + blResponseOrder.email);
                            EmailService emailService = new EmailService();
                            string subject = "Faktura elektroniczna nr." + receiptInvoiceObj.NumerWewnetrzny.PelnaSygnatura;
                            string body = "Szanowni Państwo!\n\n W załączniku przesyłamy Fakturę VAT o numerze " + receiptInvoiceObj.NumerWewnetrzny.PelnaSygnatura;


                            List<string> attachments = new List<string>();
                            attachments.Add(filepath);

                            emailService.SendEmail(blResponseOrder.email, subject, body, attachments);
                        }


                 }
                    catch (Exception ex)
                    {
                        Helpers.Log("Problem z zapisem do pliku");
                        Helpers.Log(ex.Message);
                    }
                }

            }
        }

        private void PrintFiscalReceipt(DokumentDS receiptInvoiceObj)
        {
            //TODO: check it below works
            Helpers.Log("PrintFiscalReceipt");

            IFiskalizacjaDokumentu fiskalizator = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IFiskalizacjaDokumentu>();
            IUrzadzeniaZewnetrzne cashRegisters = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IUrzadzeniaZewnetrzne>();
            string cashRegisterName = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterName).ToString();
            UrzadzenieZewnetrzne cashRegisterDevice = cashRegisters.Dane.Wszystkie().Where(ds => ds.Nazwa == cashRegisterName).FirstOrDefault();


            using (IUrzadzenieZewnetrzne device = cashRegisters.Znajdz(cashRegisterDevice))
            {
                fiskalizator.Fiskalizuj(receiptInvoiceObj.Id, device.Dane.Id);
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
                Helpers.Log("update in baselinker");
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
                Helpers.Log("problem save to baselinker");
                Helpers.Log(ex.Message);
                Helpers.Log(ex.StackTrace);
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
                    Helpers.Log("Client exist - company");
                    return this.customer;
                }
            }
            this.customer = podmioty.Dane.Wszystkie().Where(pdm => pdm.NazwaSkrocona == blResponseOrder.invoice_fullname)
                .Where(pdm => pdm.Telefon == blResponseOrder.phone).FirstOrDefault();

            if (this.customer != null)
            {
                Helpers.Log("Client exist - person");
                return this.customer;
            }
            Helpers.Log("Client not exist");
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
                Helpers.Log("Client not exist - create company");
                nowyPodmiot = podmioty.UtworzFirme();
            }
            else
            {
                Helpers.Log("Client not exist - create person");
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
                        Helpers.Log("Customer created: " + name);
                    }

                    else
                    {
                        throw new Exception(nowyPodmiot.DumpErrors());
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Problem with save user");
                Helpers.Log(ex.Message);
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
                        Helpers.Log("Email found!: " + blResponseOrder.email);
                    }
                }
                if (!emailFound)
                {
                    Helpers.Log("E-mail is diffrent: " + blResponseOrder.email);
                    using (IPodmiot customer = podmioty.Znajdz(this.customer))
                    {
                        Kontakt newEmail = new Kontakt();
                        customer.Dane.Kontakty.Add(newEmail);
                        newEmail.Rodzaj = rodzajeKontaktu.DaneDomyslne.Email;
                        newEmail.Wartosc = blResponseOrder.email;
                        newEmail.Podstawowy = true;
                        if (customer.Zapisz())
                        {
                            Helpers.Log("Email updated");
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
                Helpers.Log("Problem with save user");
                Helpers.Log(ex.Message);
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
                    receipt.Dane.Magazyn = getWarehouse();
                    receipt.Dane.MiejsceSprzedazy = getBranch();


                    receipt.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    receipt.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;
                    Helpers.Log("Added customer by ID");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Helpers.Log("Searching product: " + orderItem.name);

                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        Helpers.Log("Assortiment found in DB: " + asortymentId);
                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            Helpers.Log("Assortiment Found in Subiekt: " + asortymentId);
                            PozycjaDokumentu invoiceitem = receipt.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);

                            receipt.Przelicz();

                            Helpers.Log("make receipt count for position: " + asortymentId);
                        }
                        else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. EAN:" + orderItem.ean + " nazwa " + orderItem.name);
                        }
                    }


                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        Helpers.Log("Add delivery position to invoice");
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
                    Helpers.Log("save receipt");
                    if (receipt.Zapisz())
                    {
                        this.documentType = "PA";
                        Helpers.Log($"Receipt number: {receipt.Dane.NumerWewnetrzny.PelnaSygnatura}.");
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
                Helpers.Log("Problem with save receipt");
                Helpers.Log(ex.Message);
                return 0;
            }
        }

        private InsERT.Moria.ModelDanych.Magazyn getWarehouse()
        {
            IMagazyny warehouses = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IMagazyny>();
            var warehouseKeyValue = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse).ToString();
            return warehouses.Dane.Wszystkie().Where(key => key.Symbol == warehouseKeyValue).Single();
        }

        private InsERT.Moria.ModelDanych.MiejsceSprzedazy getBranch()
        {
            IMiejscaSprzedazy branches = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IMiejscaSprzedazy>();
            var branchesKeyValue = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Default_Branch).ToString();
            Helpers.Log(branchesKeyValue);
            return branches.Dane.Wszystkie().Where(key => key.Symbol.Contains(branchesKeyValue)).Single();
        }

        private int createInvoice()
        {
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy invoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureSprzedazy())
                {
                    invoice.Dane.Magazyn = getWarehouse();
                    invoice.Dane.MiejsceSprzedazy = getBranch();
                    invoice.PodmiotyDokumentu.UstawNabywceWedlugNIP(Helpers.ExtractDigits(blResponseOrder.invoice_nip));
                    invoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    Helpers.Log("Added company by NIP");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Helpers.Log("Searching product: " + orderItem.name);

                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        Helpers.Log("Assortiment found in DB: " + asortymentId);
                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            Helpers.Log("Assortiment Found in Subiekt: " + asortymentId);
                            PozycjaDokumentu invoiceitem = invoice.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            invoice.Przelicz();

                            Helpers.Log("make invoice count for position: " + asortymentId);
                        } else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. EAN:" + orderItem.ean + " nazwa " + orderItem.name);
                        }
                    }

                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        Helpers.Log("Add delivery position to invoice");
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
                    Helpers.Log("save invoice");
                    if (invoice.Zapisz())
                    {
                        this.documentType = "FS";
                        Helpers.Log($"Invoice number: {invoice.Dane.NumerWewnetrzny.PelnaSygnatura}.");
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
                Helpers.Log("Problem with save invoice");
                Helpers.Log(ex.Message);
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
                    retailInvoice.Dane.Magazyn = getWarehouse();
                    retailInvoice.Dane.MiejsceSprzedazy = getBranch();
                    retailInvoice.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    retailInvoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    Helpers.Log("Added retail customer by id");

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        Helpers.Log("Searching product: " + orderItem.name);

                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        Helpers.Log("Assortiment found in DB: " + asortymentId);
                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            Helpers.Log("Assortiment Found in Subiekt: " + asortymentId);
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
                        Helpers.Log("Add delivery position to invoice retail");
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
                    Helpers.Log("save invoice");
                    if (retailInvoice.Zapisz())
                    {
                        this.documentType = "FD";
                        Helpers.Log($"Invoice number: {retailInvoice.Dane.NumerWewnetrzny.PelnaSygnatura}.");
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
                Helpers.Log("Problem with save invoice");
                Helpers.Log(ex.Message);
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
