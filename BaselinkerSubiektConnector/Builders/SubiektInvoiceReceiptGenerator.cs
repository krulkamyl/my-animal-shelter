using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Repositories;
using BaselinkerSubiektConnector.Support;
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
using System.Linq;
using System.Threading.Tasks;
using Spire.Pdf;
using System.Threading;
using BaselinkerSubiektConnector.Services.EmailService;
using InsERT.Moria.Urzadzenia;
using InsERT.Moria.Urzadzenia.Core;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Builders.Emails;
using BaselinkerSubiektConnector.Objects.SQLite;

namespace BaselinkerSubiektConnector.Builders
{
    public class SubiektInvoiceReceiptBuilder
    {
        private int baselinkerOrderId;
        private BaselinkerAdapter blAdapter;
        public MainWindowViewModel mainWindowViewModel;
        private BaselinkerOrderResponse blOrderResponse;
        private string blOrderResponseString;
        private Podmiot customer;
        private MSSQLAdapter mssqlAdapter;
        private string documentType;
        private string baselinkerId;
        public string subiektNumberSalesDoc;
        public string errorMessage;

        public SubiektInvoiceReceiptBuilder(int baselinkerOrderId, MainWindowViewModel mainWindowViewModel)
        {
            this.baselinkerOrderId = baselinkerOrderId;
            this.mainWindowViewModel = mainWindowViewModel;

            try
            {
                this.mssqlAdapter = new MSSQLAdapter(
                    ConfigRepository.GetValue(
                        RegistryConfigurationKeys.MSSQL_Host),
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password)
                );

                string blApiKey = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
                string storageId = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId);

                this.blAdapter = new BaselinkerAdapter(blApiKey, storageId);
                InitializeOrderResponseAsync().Wait();

                blOrderResponseString = JsonConvert.SerializeObject(this.blOrderResponse);
                baselinkerId = blOrderResponse.orders[0].order_id.ToString();
                if (this.blOrderResponse.orders[0].extra_field_1 == "#ZAIMPORTOWANE#")
                {
                    throw new Exception("Zamówienie zostało już zaimporowane do Subiekta: " + this.blOrderResponse.orders[0].extra_field_2);
                }

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

                        if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_AddCommentDocNumber) == "1")
                        {
                            this.UpdateOrder(receiptInvoiceObj);
                        }


                        if (this.documentType == "FS" || this.documentType == "FD")
                        {
                            this.SavePrintInvoiceAndSendEmail(receiptInvoiceObj);
                        }
                        else if((this.documentType == "PA" || this.documentType == "PF") && ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterEnabled) == "1")
                        {
                            this.PrintFiscalReceipt(receiptInvoiceObj);
                        }

                        InsertRecord(null, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
                return;
            }
        }

        private void SavePrintInvoiceAndSendEmail(DokumentDS receiptInvoiceObj)
        {
            string documentTypeHumanable = "fakturę sprzedaży VAT";
            IWydruki wydruki = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IWydruki>();
            IDokumentySprzedazy dokumentySprzedazy = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IDokumentySprzedazy>();
            DokumentDS dokumentSprzedazy = dokumentySprzedazy.Dane.Wszystkie().Where(ds => ds.Id == receiptInvoiceObj.Id).FirstOrDefault();

            using (IDokumentSprzedazy ds = dokumentySprzedazy.Znajdz(dokumentSprzedazy))
            {
                InsERT.Moria.Wydruki.Enums.TypWzorcaWydruku typWzorca = InsERT.Moria.Wydruki.Enums.TypWzorcaWydruku.FakturaSprzedazy;
                if (this.documentType == "FD")
                {
                    documentTypeHumanable = "fakturę detaliczną";
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

                        if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterEnabled) == "1"
                            && ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName).Length > 3)
                        {
                            int milliseconds = 2000;
                            Thread.Sleep(milliseconds);
                            PdfDocument pdfdocument = new PdfDocument();
                            pdfdocument.LoadFromFile(filepath);
                            pdfdocument.PrintSettings.PrinterName = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName);
                            pdfdocument.Print();
                            pdfdocument.Dispose();
                        }

                        BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                        if (ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailSendAuto) == "1"
                            && blResponseOrder.email.Length > 3 
                            && blResponseOrder.email.Contains("@")
                            )
                        {
                            EmailService emailService = new EmailService();
                            string subject = "Faktura elektroniczna nr." + receiptInvoiceObj.NumerWewnetrzny.PelnaSygnatura;
                            string body = "Szanowni Państwo!\n\n W załączniku przesyłamy "+ documentTypeHumanable + " o numerze " + receiptInvoiceObj.NumerWewnetrzny.PelnaSygnatura;


                            List<string> attachments = new List<string>();
                            attachments.Add(filepath);

                            emailService.SendEmail(blResponseOrder.email, subject, body, attachments);
                        }


                 }
                    catch (Exception ex)
                    {
                        Helpers.Log(ex.Message);
                        SendErrorMessage(ex.Message);
                        this.errorMessage = ex.Message;
                    }
                }

            }
        }

        private void PrintFiscalReceipt(DokumentDS receiptInvoiceObj)
        {
            IFiskalizacjaDokumentu fiskalizator = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IFiskalizacjaDokumentu>();
            IUrzadzeniaZewnetrzne cashRegisters = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IUrzadzeniaZewnetrzne>();
            string cashRegisterName = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterName).ToString();
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

        private void InsertRecord(
            string errors,
            int status = 1
            )
        {
            SQLiteSalesDocObject sQLiteSalesDocObject = new SQLiteSalesDocObject();
            sQLiteSalesDocObject.baselinker_id = baselinkerId;
            sQLiteSalesDocObject.type = documentType;
            sQLiteSalesDocObject.subiekt_doc_number = subiektNumberSalesDoc;
            sQLiteSalesDocObject.baselinker_data = blOrderResponseString;
            sQLiteSalesDocObject.errors = errors;
            sQLiteSalesDocObject.status = status;
            sQLiteSalesDocObject.created_at = DateTime.Now.ToString();

            SalesDocsRepository.CreateRecord(sQLiteSalesDocObject);
        }

        private Task UpdateOrder(DokumentDS receiptInvoice)
        {
            try
            {
                var data = new Dictionary<string, string>
            {
                {
                    "extra_field_1", "#ZAIMPORTOWANE#"
                },
                {
                    "extra_field_2", receiptInvoice.NumerWewnetrzny.PelnaSygnatura
                },
            };

                blAdapter.UpdateOrderAsync(baselinkerOrderId, data);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);

                this.errorMessage = ex.Message;
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
                    return this.customer;
                }
            }
            this.customer = podmioty.Dane.Wszystkie().Where(pdm => pdm.NazwaSkrocona == blResponseOrder.invoice_fullname)
                .Where(pdm => pdm.Telefon == blResponseOrder.phone).FirstOrDefault();

            if (this.customer != null)
            {
                return this.customer;
            }
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
                nowyPodmiot = podmioty.UtworzFirme();
            }
            else
            {
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

                    if (!nowyPodmiot.Zapisz())
                    {
                        throw new Exception(nowyPodmiot.DumpErrors());
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
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
                    }
                }
                if (!emailFound)
                {
                    using (IPodmiot customer = podmioty.Znajdz(this.customer))
                    {
                        Kontakt newEmail = new Kontakt();
                        customer.Dane.Kontakty.Add(newEmail);
                        newEmail.Rodzaj = rodzajeKontaktu.DaneDomyslne.Email;
                        newEmail.Wartosc = blResponseOrder.email;
                        newEmail.Podstawowy = true;
                        if (!customer.Zapisz())
                        {
                            throw new Exception(customer.DumpErrors());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
            }
        }

        private bool checkReceiptInvoiceExist()
        {
            return false;
        }

        private int createReceipt()
        {
            this.documentType = "PA";
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy receipt = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzParagon())
                {
                    receipt.Dane.Magazyn = getWarehouse();
                    receipt.Dane.MiejsceSprzedazy = getBranch();


                    receipt.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    receipt.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            int qtyOnWarehouse = mssqlAdapter.GetWarehouseAssortmentQuantity(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 asortymentId,
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse)
                            );
                            if (qtyOnWarehouse == 0)
                            {
                                throw new Exception("Na magazynie " + ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse) + " nie ma dla produktu: " + asortyment.Nazwa + " (EAN: "+ orderItem.ean + ") wymaganej ilości dostępnej dla zamowienia (" + orderItem.quantity.ToString() + " sztuk)");
                            }
                            PozycjaDokumentu invoiceitem = receipt.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);

                            receipt.Przelicz();
                        }
                        else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. \nEAN:" + orderItem.ean + "\nNazwa: " + orderItem.name);
                        }
                    }


                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Nie jestem w stanie utworzyc produktu zwiazanego z dostawa.");
                        }
                        PozycjaDokumentu deliveryItem = receipt.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        receipt.Przelicz();
                    }

                    receipt.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    if (receipt.Zapisz())
                    {
                        subiektNumberSalesDoc = receipt.Dane.NumerWewnetrzny.PelnaSygnatura;
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
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
                return 0;
            }
        }

        private InsERT.Moria.ModelDanych.Magazyn getWarehouse()
        {
            IMagazyny warehouses = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IMagazyny>();
            var warehouseKeyValue = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse).ToString();
            return warehouses.Dane.Wszystkie().Where(key => key.Symbol == warehouseKeyValue).Single();
        }

        private InsERT.Moria.ModelDanych.MiejsceSprzedazy getBranch()
        {
            IMiejscaSprzedazy branches = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IMiejscaSprzedazy>();
            var branchesKeyValue = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Branch).ToString();
            Helpers.Log(branchesKeyValue);
            return branches.Dane.Wszystkie().Where(key => key.Symbol.Contains(branchesKeyValue)).Single();
        }

        private int createInvoice()
        {
            this.documentType = "FS";
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy invoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureSprzedazy())
                {
                    invoice.Dane.Magazyn = getWarehouse();
                    invoice.Dane.MiejsceSprzedazy = getBranch();
                    invoice.PodmiotyDokumentu.UstawNabywceWedlugNIP(Helpers.ExtractDigits(blResponseOrder.invoice_nip));
                    invoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            int qtyOnWarehouse = mssqlAdapter.GetWarehouseAssortmentQuantity(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 asortymentId,
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse)
                            );
                            if (qtyOnWarehouse == 0)
                            {
                                throw new Exception("Na magazynie " + ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse) + " nie ma dla produktu: " + asortyment.Nazwa + " (EAN: " + orderItem.ean + ") wymaganej ilości dostępnej dla zamowienia (" + orderItem.quantity.ToString() + " sztuk)");
                            }

                            PozycjaDokumentu invoiceitem = invoice.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            invoice.Przelicz();
                        } else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. \nEAN:" + orderItem.ean + "\nNazwa: " + orderItem.name);
                        }
                    }

                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Nie jestem w stanie utworzyc produktu zwiazanego z dostawa.");
                        }
                        PozycjaDokumentu deliveryItem = invoice.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        invoice.Przelicz();
                    }

                    invoice.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    if (invoice.Zapisz())
                    {
                        subiektNumberSalesDoc = invoice.Dane.NumerWewnetrzny.PelnaSygnatura;
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
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
                return 0;
            }
        }

        private int createRetailInvoice()
        {
            this.documentType = "FD";
            try
            {
                BaselinkerOrderResponseOrder blResponseOrder = this.blOrderResponse.orders[0];
                using (IDokumentSprzedazy retailInvoice = this.mainWindowViewModel.UchwytDoSfery.DokumentySprzedazy().UtworzFaktureDetaliczna())
                {
                    retailInvoice.Dane.Magazyn = getWarehouse();
                    retailInvoice.Dane.MiejsceSprzedazy = getBranch();
                    retailInvoice.PodmiotyDokumentu.UstawNabywceWedlugId(this.customer.Id);
                    retailInvoice.Dane.OperacjePrzeliczaniaPozycji = OperacjePrzeliczaniaPozycji.Brutto_ID;

                    foreach (BaselinkerOrderResponseOrderProduct orderItem in blResponseOrder.products)
                    {
                        IAsortymenty podmioty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        int asortymentId = Convert.ToInt32(
                            this.mssqlAdapter.GetProductFromEan(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 orderItem.ean
                                ).First()
                        );

                        IAsortymenty asortymenty = this.mainWindowViewModel.UchwytDoSfery.PodajObiektTypu<IAsortymenty>();
                        InsERT.Moria.ModelDanych.Asortyment asortyment = asortymenty.Dane.Wszystkie().Where(k => k.Id == asortymentId).Single();
                        if (asortyment != null)
                        {
                            int qtyOnWarehouse = mssqlAdapter.GetWarehouseAssortmentQuantity(
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                                 asortymentId,
                                 ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse)
                            );
                            if (qtyOnWarehouse == 0)
                            {
                                throw new Exception("Na magazynie " + ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse) + " nie ma dla produktu: " + asortyment.Nazwa + " (EAN: " + orderItem.ean + ") wymaganej ilości dostępnej dla zamowienia (" + orderItem.quantity.ToString() + " sztuk)");
                            }
                            PozycjaDokumentu invoiceitem = retailInvoice.Pozycje.Dodaj(asortyment, Convert.ToDecimal(orderItem.quantity), asortyment.JednostkaSprzedazy);
                            invoiceitem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(orderItem.price_brutto);
                            retailInvoice.Przelicz();

                        }
                        else
                        {
                            throw new Exception("Produkt nie zostal znaleziony. \nEAN:" + orderItem.ean + "\nNazwa: " + orderItem.name);
                        }
                    }

                    if (this.blOrderResponse.orders[0].delivery_price != 0)
                    {
                        var deliveryAssortmentRepository = new DeliveryAssortmentRepository(
                            this.mainWindowViewModel
                        );
                        var deliveryPosition = deliveryAssortmentRepository.GetAssortment();
                        if (deliveryPosition == null)
                        {
                            throw new Exception("Nie jestem w stanie utworzyc produktu zwiazanego z dostawa.");
                        }
                        PozycjaDokumentu deliveryItem = retailInvoice.Pozycje.Dodaj(deliveryPosition, 1, deliveryPosition.JednostkaSprzedazy);
                        deliveryItem.Cena.BruttoPrzedRabatem = Convert.ToDecimal(blResponseOrder.delivery_price);
                        retailInvoice.Przelicz();
                    }

                    retailInvoice.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();
                    if (retailInvoice.Zapisz())
                    {
                        subiektNumberSalesDoc = retailInvoice.Dane.NumerWewnetrzny.PelnaSygnatura;
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
                Helpers.Log("Problem z zapisem faktury detalicznej");
                Helpers.Log(ex.Message);
                SendErrorMessage(ex.Message);
                this.errorMessage = ex.Message;
                return 0;
            }
        }
        private void SendErrorMessage(string error)
        {
            InsertRecord(error, 0);
            Helpers.SendWebhook(error);

            EmaiReportError.Build(
                error,
                blOrderResponse
                );
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
                encjaZBledami.ToString();
                foreach (var bladNaCalejEncji in encjaZBledami.Errors)
                {
                    errors += bladNaCalejEncji+"\n";
                    errors += " na encjach:" + encjaZBledami.GetType().Name +"\n";
                    errors += "\n";
                }
                foreach (var bladNaKonkretnychPolach in encjaZBledami.MemberErrors)
                {

                    errors += string.Join(", ", bladNaKonkretnychPolach.Select(b => encjaZBledami.GetType().FullName + "." + b)) + "\n";
                    errors += string.Join(", ", bladNaKonkretnychPolach.Select(b => encjaZBledami.GetType().Name + "." + b)) + "\n";
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
