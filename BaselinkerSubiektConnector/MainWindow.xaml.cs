using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Services.EmailService;
using BaselinkerSubiektConnector.Services.HttpService;
using InsERT.Moria.Sfera;
using InsERT.Mox.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfMessageBoxLibrary;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Threading.Timer;
using Helpers = BaselinkerSubiektConnector.Support.Helpers;
using System.IO;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Support;
using BaselinkerSubiektConnector.Validators;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Composites;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        private MSSQLAdapter mssqlAdapter;
        private List<BaselinkerStoragesResponseStorage> storages; 
        private HttpService httpService;
        private DispatcherTimer timer;
        private static Timer checkSferaIsEnabled;

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            CheckAppDataFolderExists();
            InitializeDatabase();


            ViewModel = new MainWindowViewModel
            {
                ExceptionHandler = HandleException
            };
            LoadConfiguration();

            DataContext = ViewModel;

            httpService = new HttpService();
            httpService.mainWindowViewModel = ViewModel;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += CheckHttpServiceEnabled;
            timer.Start();

            checkSferaIsEnabled = new Timer(CheckSferaIsEnabledMethod, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

 
            Closing += MainWindow_Closing;

        }

        private void CheckAppDataFolderExists()
        {
            if (!Directory.Exists(Helpers.GetApplicationPath()))
            {
                try
                {
                    Directory.CreateDirectory(Helpers.GetApplicationPath());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd podczas tworzenia folderu '{Helpers.GetApplicationPath()}': {ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                SQLiteService.InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SQLITE] " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show(ex.Message, "Błąd SQLite!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CheckSferaIsEnabledMethod(object state)
        {
            if (ViewModel.CzySferaJestUruchomiona)
            {
                httpService.StartStop();
                checkSferaIsEnabled.Dispose();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (httpService.IsEnabled)
            {
                httpService.Stop();
            }
        }


        private void LoadConfiguration()
        {

            LoadItemsAndSetDefault(Subiekt_DefaultWarehouse, SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(),
                RegistryConfigurationKeys.Subiekt_Default_Warehouse);
            LoadItemsAndSetDefault(Subiekt_DefaultBranch, SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Branch);
            LoadItemsAndSetDefault(Subiekt_CashRegisterName, SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(), RegistryConfigurationKeys.Subiekt_CashRegisterName);
            LoadItemsAndSetDefault(Baselinker_StorageName, SQLiteDatabaseNames.GetBaselinkerWarehousesDatabaseName(), RegistryConfigurationKeys.Baselinker_StorageName);

            UpdateComboBox(MSSQL_Name, RegistryConfigurationKeys.MSSQL_DB_NAME);

            MSSQL_IP.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host);
            MSSQL_User.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login);
            MSSQL_Password.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password);

            Subiekt_User.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Login);
            Subiekt_Password.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Password);

            Baselinker_ApiKey.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);

            Config_EmailServer.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailServer);
            Config_EmailPort.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPort);
            Config_EmailLogin.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailLogin);
            Config_EmailPassword.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPassword);


            Config_EmailReporting.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailReporting);

            Config_CompanyName.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyName);
            Config_CompanyNip.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyNip);
            Config_CompanyAddress.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyAddress);
            Config_CompanyZipCode.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyZipCode);
            Config_CompanyCity.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyCity);
            Config_CompanyEmailAddress.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress);
            Config_CompanyPhone.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyPhone);

            string emailTemplate = ConfigRepository.GetValue(RegistryConfigurationKeys.Email_Template);

            if (emailTemplate != null && emailTemplate.Length > 50)
            {
                EmailTemplate.Text = emailTemplate;
            }
            else
            {
                EmailTemplate.Text = EmailService.GetEmailTemplate();
            }


            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterEnabled) == "1")
            {
                Subiekt_CashRegisterEnabled.IsChecked = true;
            }

            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterEnabled) == "1")
            {
                Subiekt_PrinterEnabled.IsChecked = true;
            }

            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailSendAuto) == "1")
            {
                Config_EmailSendAuto.IsChecked = true;
            }


            var defaultPrinter = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName);
            if (defaultPrinter != null && defaultPrinter.Length > 3)
            {
                Subiekt_PrinterName.Items.Add(defaultPrinter);
                Subiekt_PrinterName.SelectedItem = defaultPrinter;
            }
            foreach (string printer in Helpers.GetPrinters())
            {
                if (defaultPrinter != printer)
                {
                    Subiekt_PrinterName.Items.Add(printer);
                }
            }

        }

        private void LoadItemsAndSetDefault(System.Windows.Controls.ComboBox comboBox, string databaseName, string registryKey)
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

            var defaultValue = ConfigRepository.GetValue(registryKey);
            if (!string.IsNullOrEmpty(defaultValue) && defaultValue.Length > 1)
            {
                comboBox.SelectedItem = defaultValue;
            }
        }

        private void UpdateComboBox(System.Windows.Controls.ComboBox comboBox, string registryKey)
        {
            string value = ConfigRepository.GetValue(registryKey);

            if (value != null && value.Length > 0)
            {
                comboBox.Items.Clear();
                comboBox.Items.Add(value);
                comboBox.Text = value;
            }
            else
            {
                comboBox.IsEnabled = false;
            }
        }



        internal static DanePolaczenia OdbierzDanePolaczeniaZInsLauncher()
        {
            var commandLineArguments = Environment.GetCommandLineArgs();
            if (commandLineArguments != null && commandLineArguments.Contains(@"/UruchomionePrzezInsLauncher"))
            {
                return DanePolaczenia.Odbierz();
            }

            return null;
        }

        internal static DaneDoUruchomieniaSfery PodajDaneDoUruchomienia(DanePolaczenia danePolaczenia)
        {
            var dane = new DaneDoUruchomieniaSfery()
                {
                    Serwer = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                    Baza = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                    LoginSql = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                    HasloSql = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password),
                    LoginNexo = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Login),
                    HasloNexo = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Password),
                    Produkt = ProductId.Subiekt,
            };
                return dane;
            }

        private void HandleException(Exception e)
        {
            _ = MessageBox.Show(e.ToString());
        }

        private void PolaczButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sendDataToValidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Wystąpił błąd walidacyjny", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (httpService.IsEnabled)
            {
                httpService.Stop();
            }
            else
            {
                if (!ViewModel.CzySferaJestUruchomiona)
                {
                    DaneDoUruchomieniaSfery daneDoUruchomieniaSfery;
                    Helpers.EnsureExportFolderExists();
                    Helpers.StartLog();

                    var danePolaczenia = OdbierzDanePolaczeniaZInsLauncher();
                    daneDoUruchomieniaSfery = PodajDaneDoUruchomienia(danePolaczenia);

                    ViewModel.PolaczZeSfera(daneDoUruchomieniaSfery);
                }
            }
        }

        private void sendDataToValidate()
        {
            ConfigValidatorModel model = new ConfigValidatorModel
            {
                MssqlDatabaseName = MSSQL_Name.Text,
                BaselinkerWarehouse = Baselinker_StorageName.Text,
                SubiektWarehouse = Subiekt_DefaultWarehouse.Text,
                SubiektBranch = Subiekt_DefaultBranch.Text,
                PrinterEnabled = (bool)Subiekt_PrinterEnabled.IsChecked,
                PrinterName = Subiekt_PrinterName.Text,
                CashRegisterEnabled = (bool)Subiekt_CashRegisterEnabled.IsChecked,
                CashRegisterName = Subiekt_CashRegisterName.Text,
                SendEmailEnabled = (bool)Config_EmailSendAuto.IsChecked,
                EmailLogin = Config_EmailLogin.Text,
                EmailPassword = Config_EmailPassword.Text,
                EmailReporting = Config_EmailReporting.Text,
                CompanyName = Config_CompanyName.Text,
                CompanyNip = Config_CompanyNip.Text,
                CompanyEmail = Config_CompanyEmailAddress.Text
            };
            ConfigValidator.Validate(model);
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                sendDataToValidate();
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Wystąpił błąd walidacyjny", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Host, MSSQL_IP.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Login, MSSQL_User.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Password, MSSQL_Password.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_DB_NAME, MSSQL_Name.Text);


            Record BaselinkerWarehouseSelected = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetBaselinkerWarehousesDatabaseName(),
                "key",
                Baselinker_StorageName.Text
            );

            if (BaselinkerWarehouseSelected != null)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageId, BaselinkerWarehouseSelected.value);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageName, BaselinkerWarehouseSelected.key);
            }

            if (Subiekt_DefaultWarehouse.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse, Subiekt_DefaultWarehouse.Text);
            }

            if (Subiekt_CashRegisterName.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_CashRegisterName, Subiekt_CashRegisterName.Text);
            }

            if (Subiekt_DefaultBranch.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Default_Branch, Subiekt_DefaultBranch.Text);
            }

            if (Subiekt_PrinterName.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_PrinterName, Subiekt_PrinterName.Text);
            }

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Subiekt_PrinterEnabled,
                Subiekt_PrinterEnabled.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Subiekt_CashRegisterEnabled,
                Subiekt_CashRegisterEnabled.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Config_EmailSendAuto,
                Config_EmailSendAuto.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Login, Subiekt_User.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Password, Subiekt_Password.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_ApiKey, Baselinker_ApiKey.Text);


            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailServer, Config_EmailServer.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPort, Config_EmailPort.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailLogin, Config_EmailLogin.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPassword, Config_EmailPassword.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailReporting, Config_EmailReporting.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyName, Config_CompanyName.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyNip, Config_CompanyNip.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyAddress, Config_CompanyAddress.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyZipCode, Config_CompanyZipCode.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyCity, Config_CompanyCity.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress, Config_CompanyEmailAddress.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyPhone, Config_CompanyPhone.Text);

            MessageBox.Show("Możesz spróbować połączyć się ze Sferą", "Konfiguracja zapisana pomyślnie!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MssqlGetDbName(object sender, RoutedEventArgs e)
        {
            try
            {
                MSSQL_Name.Items.Clear();


                mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                List<string> databaseNames = mssqlAdapter.GetNexoDatabaseNames();

                if (databaseNames.Count > 0 )
                {
                    MSSQL_Name.IsEnabled = true;
                }

                foreach (string dbName in databaseNames)
                {
                    MSSQL_Name.Items.Add(dbName);
                }

                MessageBox.Show("Pobrano listę dostępnych baz!", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void EmailTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Email_Template, EmailTemplate.Text);

                MessageBox.Show("Zapisano szablon e-mail", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            if (Config_EmailLogin.Text.Length > 3 && Config_EmailPassword.Text.Length > 3 && Config_EmailPort.Text.Length >= 2 && Config_EmailServer.Text.Length > 3)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailServer, Config_EmailServer.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPort, Config_EmailPort.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailLogin, Config_EmailLogin.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPassword, Config_EmailPassword.Text);

                var msgProperties = new WpfMessageBoxProperties()
                {
                    Button = MessageBoxButton.OKCancel,
                    ButtonOkText = "Wyślij",
                    CheckBoxText = "Anuluj",
                    Image = MessageBoxImage.Information,
                    Header = "Testowanie wysyłania e-mail",
                    IsCheckBoxChecked = true,
                    IsCheckBoxVisible = false,
                    IsTextBoxVisible = true,
                    Text = "Wprowadź swój adres e-mail, na którego ma zostać wysłany testowa wiadomość e-mail.",
                    Title = "Test e-mail",
                };

                MessageBoxResult result = WpfMessageBox.Show(this, ref msgProperties);
                if (msgProperties.TextBoxText.Length > 3 && msgProperties.TextBoxText.Contains("@"))
                {
                    EmailService emailService = new EmailService();
                    emailService.SendEmail(msgProperties.TextBoxText, "Testowa wiadomosć e-mail", "Jeżeli otrzymałeś/aś tę wiadomość e-mail, to znaczy, że wysyłanie e-maili działa!");
                    MessageBox.Show("Wiadomość została wysłana. Sprawdź swój adres e-mail.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
               
            }
            else
            {
                MessageBox.Show("Wystąpił błąd: \n" + "Nie uzupełniono wszystkich pól wymaganych dla działania wysyłania wiadomości e-mail.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void MSSQL_Name_DropDownClosed(Object sender, EventArgs e)
        {
            if (MSSQL_Name.Text.StartsWith("Nexo_"))
            {
                Subiekt_DefaultWarehouse.Items.Clear();
                Subiekt_DefaultBranch.Items.Clear();
                Subiekt_CashRegisterName.Items.Clear();
                mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                
                SubiektWarehouses.UpdateExistingData(mssqlAdapter.GetWarehouses(MSSQL_Name.Text));
                SubiektBranches.UpdateExistingData(mssqlAdapter.GetBranches(MSSQL_Name.Text));
                SubiektCashRegisters.UpdateExistingData(mssqlAdapter.GetCashRegisters(MSSQL_Name.Text));
                LoadItemsAndSetDefault(Subiekt_DefaultWarehouse, SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Warehouse);
                LoadItemsAndSetDefault(Subiekt_DefaultBranch, SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Branch);
                LoadItemsAndSetDefault(Subiekt_CashRegisterName, SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(), RegistryConfigurationKeys.Subiekt_CashRegisterName);

                if (Subiekt_DefaultWarehouse.Items.Count > 0)
                {
                    Subiekt_DefaultWarehouse.IsEnabled = true;
                }

                if (Subiekt_DefaultBranch.Items.Count > 0)
                {
                    Subiekt_DefaultBranch.IsEnabled = true;
                }

                if (Subiekt_CashRegisterName.Items.Count > 0)
                {
                    Subiekt_CashRegisterName.IsEnabled = true;
                }



            }
        }

        private async void BaselinkerGetStorage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(Baselinker_ApiKey.Text);

                var storagesList = await baselinkerAdapter.GetStoragesListAsync();

                if (storagesList.status == "SUCCESS")
                {
                    Baselinker_StorageName.IsEnabled = true;
                    Baselinker_StorageName.Items.Clear();
                    storages = storagesList.storages;

                    BaselinkerWarehouses.UpdateExistingData(storagesList.storages);

                    foreach ( var storage in storagesList.storages )
                    {
                        Baselinker_StorageName.Items.Add(storage.name);
                    }

                    MessageBox.Show("Udało się nawiązać połączenie z BaseLinker. Wybierz magazyn.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    throw new Exception(storagesList.error_message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CheckHttpServiceEnabled(object sender, EventArgs e)
        {
            if (httpService.IsEnabled)
            {
                HttpServiceEnabled.Text = "Serwer HTTP działa. Adres: http://" + httpService.ServerIpAddress + ":" + httpService.port;
                HttpServiceEnabledIcon.Fill = Brushes.Green;
                ButtonEnableDisableApp.Content = "Wyłacz";
            }
            else
            {
                HttpServiceEnabled.Text = "Serwer HTTP wyłączony";
                HttpServiceEnabledIcon.Fill = Brushes.Red;
                ButtonEnableDisableApp.Content = "Włącz";
            }
        }


        private async void AssortmentsBaselinkerSyncButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId) == null)
            {
                MessageBox.Show("Wystąpił błąd: \n nie wybrano domyślnego magazynu w Baselinker", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey)
                );

                assortments_BaselinkerSyncProgressText.Text = "Pobieranie inwentarza";

                assortmentsBaselinkerSyncButton.IsEnabled = false;
                assortments_BaselinkerSyncProgressBar.Visibility = Visibility.Visible;
                assortments_BaselinkerSyncProgressText.Visibility = Visibility.Visible;

                InventoryResponse inventories = await baselinkerAdapter.GetInventoriesAsync();


                if (inventories.status == "SUCCESS")
                {
                    var inventoriesList = inventories.inventories;

                    foreach (Inventory inventory in inventoriesList)
                    {
                        if (inventory.is_default)
                        {
                            assortments_BaselinkerSyncProgressText.Text = "Pobrano inwentarz \""+inventory.name+"\". Wysyłanie zlecenia o pobranie produktów.";

                            BaselinkerSQLiteProductSyncComposite baselinkerSQLiteProductSyncComposite = new BaselinkerSQLiteProductSyncComposite();
                            baselinkerSQLiteProductSyncComposite.Sync(assortments_BaselinkerSyncProgressText, inventory.inventory_id, assortments_BaselinkerSyncProgressBar);

                        }
                    }
                }
                else
                {
                    assortmentsBaselinkerSyncButton.IsEnabled = false;
                    assortments_BaselinkerSyncProgressBar.Visibility = Visibility.Hidden;
                    assortments_BaselinkerSyncProgressText.Visibility = Visibility.Hidden;
                    throw new Exception(inventories.error_message);
                }
            } catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
    }
}