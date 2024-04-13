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
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Input;
using BaselinkerSubiektConnector.Builders.Baselinker;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        private MSSQLAdapter mssqlAdapter;
        private List<BaselinkerStoragesResponseStorage> storages; 
        private HttpService httpService;
        private DispatcherTimer timer;
        private static Timer checkSferaIsEnabled;
        private List<AssortmentTableItem> allRecords;
        private int itemsPerPage = 100;
        private int currentPage = 1; 
        private double prevVerticalOffset;
        private bool isServiceRunning = false;
        private AddToBaselinker addToBaselinkerWindow;

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

            LoadAssortmentsPage();
            SearchMissingProductInBaselinkerSync();
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
            LoadItemsAndSetDefault(Baselinker_StorageName, SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName(), RegistryConfigurationKeys.Baselinker_StorageName);
            LoadItemsAndSetDefault(Baselinker_InventoryWarehouseName, SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName(), RegistryConfigurationKeys.Baselinker_InventoryWarehouseName);
            LoadItemsAndSetDefault(Subiekt_Login, SQLiteDatabaseNames.GetSubiektLoginsDatabaseName(), RegistryConfigurationKeys.Subiekt_Login);

            UpdateComboBox(MSSQL_Name, RegistryConfigurationKeys.MSSQL_DB_NAME);

            MSSQL_IP.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host);
            MSSQL_User.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login);
            MSSQL_Password.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password);


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

            string intervalTime = ConfigRepository.GetValue(RegistryConfigurationKeys.SyncServiceIntervalTime);

            string[] intervalValues = { "15", "30", "60", "120", "180" };

            IntervalSyncComboBox.Items.Clear();

            foreach (string value in intervalValues)
            {
                IntervalSyncComboBox.Items.Add(value);
            }

            if (intervalTime != null && IntervalSyncComboBox.Items.Contains(intervalTime))
            {
                IntervalSyncComboBox.SelectedItem = intervalTime;
            }
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
                BaselinkerStorage = Baselinker_StorageName.Text,
                BaselinkerInventoryWarehouse = Baselinker_InventoryWarehouseName.Text,
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

        private async void SaveConfiguration_Click(object sender, RoutedEventArgs e)
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


            Record bastelinkerStorageSelected = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName(),
                "key",
                Baselinker_StorageName.Text
            );

            if (bastelinkerStorageSelected != null)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageId, bastelinkerStorageSelected.value);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageName, bastelinkerStorageSelected.key);
            }


            Record baselinkerInventoryWarehouseSelected = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName(),
                "key",
                Baselinker_InventoryWarehouseName.Text
            );


            if (baselinkerInventoryWarehouseSelected != null)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_InventoryWarehouseId, baselinkerInventoryWarehouseSelected.value);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_InventoryWarehouseName, baselinkerInventoryWarehouseSelected.key);
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

            if (Subiekt_Login.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Login, Subiekt_Login.Text);
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

            _ = await FetchBaselinkerData.GetDataAsync(
                Baselinker_ApiKey.Text,
                null,
                true
            );

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
                if (mssqlAdapter == null)
                {
                    mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                }
                
                SubiektWarehouses.UpdateExistingData(mssqlAdapter.GetWarehouses(MSSQL_Name.Text));
                SubiektBranches.UpdateExistingData(mssqlAdapter.GetBranches(MSSQL_Name.Text));
                SubiektCashRegisters.UpdateExistingData(mssqlAdapter.GetCashRegisters(MSSQL_Name.Text));

                SubiektLogins.UpdateExistingData(mssqlAdapter.GetLogins(MSSQL_Name.Text));

                LoadItemsAndSetDefault(Subiekt_DefaultWarehouse, SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Warehouse);
                LoadItemsAndSetDefault(Subiekt_DefaultBranch, SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Branch);
                LoadItemsAndSetDefault(Subiekt_CashRegisterName, SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(), RegistryConfigurationKeys.Subiekt_CashRegisterName);
                LoadItemsAndSetDefault(Subiekt_Login, SQLiteDatabaseNames.GetSubiektLoginsDatabaseName(), RegistryConfigurationKeys.Subiekt_Login);

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
                _ = await FetchBaselinkerData.GetDataAsync(
                    Baselinker_ApiKey.Text
                );

                List<Record> records = SQLiteService.ReadRecords(
                    SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName()
                    );

                Baselinker_StorageName.Items.Clear();

                foreach (Record item in records )
                {
                    Baselinker_StorageName.Items.Add(item.key);
                }

                List<Record> recordsWarehouses = SQLiteService.ReadRecords(
                    SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName()
                    );


                Baselinker_InventoryWarehouseName.Items.Clear();

                foreach (Record item in recordsWarehouses)
                {
                    Baselinker_InventoryWarehouseName.Items.Add(item.key);
                }

                MessageBox.Show("Pobrano dane z Baselinker. Możesz wybrać magazyn.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            allRecords = null;
            AssortmentsTable.Items.Clear(); 
            LoadAssortmentsPage();

            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void LoadAssortmentsPage()
        {
            if (allRecords == null)
            {
                allRecords = SQLiteService.ReadRecords(SQLiteDatabaseNames.GetAssortmentsDatabaseName())
                                          .Select(record => new AssortmentTableItem
                                          {
                                              BaselinkerId = record.baselinker_id,
                                              BaselinkerName = record.baselinker_name,
                                              Barcode = record.ean_code,
                                              SubiektName = record.subiekt_name ?? "---",
                                              SubiektSymbol = record.subiekt_symbol ?? "---"
                                          }).ToList();
            }

            if (allRecords.Count == 0)
            {
                AssortmentsTable.Visibility = Visibility.Hidden;
                AssortmentsTableProductsNotFound.Visibility = Visibility.Visible;
                return;
            }

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, allRecords.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                AssortmentsTable.Items.Add(allRecords[i]);
            }
            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(prevVerticalOffset);
            }
            currentPage++;
        }

        private void AssortmentsTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                if (e.Delta < 0)
                {
                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        prevVerticalOffset = scrollViewer.VerticalOffset;
                        LoadAssortmentsPage();
                    }
                }
            }
        }


        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer)
            {
                return depObj as ScrollViewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                ScrollViewer scrollViewer = GetScrollViewer(child);
                if (scrollViewer != null)
                {
                    return scrollViewer;
                }
            }
            return null;
        }

        private void SearchMissingProductInBaselinker_Click(object sender, RoutedEventArgs e)
        {
            SearchMissingProductInBaselinkerSync();
        }

        private void SearchMissingProductInBaselinkerSync()
        {
            if (mssqlAdapter == null)
            {
                mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
            }

            GetMissingInBaselinkerSubiektProducts getMissingInBaselinkerSubiektProducts = new GetMissingInBaselinkerSubiektProducts();
            List<Record> missingProducts = getMissingInBaselinkerSubiektProducts.Sync(allRecords, mssqlAdapter);
            if (missingProducts.Count > 0)
            {
                MissingBaselinkerProducts.Items.Clear();

                MissingBaselinkerProducts.Visibility = Visibility.Visible;
                MissingBaselinkerProductsNotFound.Visibility = Visibility.Hidden;
                var missingAssortmentTableItems = missingProducts
                                          .Select(record => new AssortmentTableItem
                                          {
                                              Barcode = record.ean_code,
                                              SubiektName = record.subiekt_name ?? "---",
                                              SubiektSymbol = record.subiekt_symbol ?? "---",
                                              SubiektId = record.subiekt_id,
                                              SubiektDescription = record.subiekt_description,
                                              SubiektPrice = record.subiekt_price,
                                              SubiektQty = record.subiekt_qty,
                                          }).ToList();

                foreach (var missingAssortmentTableItem in missingAssortmentTableItems)
                {
                    MissingBaselinkerProducts.Items.Add(missingAssortmentTableItem);
                }
            }
            else
            {
                MissingBaselinkerProductsNotFound.Visibility = Visibility.Visible;
                MissingBaselinkerProducts.Visibility = Visibility.Hidden;
            }
        }

        private void AddMissingProductToBaselinker_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var item = button.DataContext as AssortmentTableItem;
                if (item != null)
                {
                    addToBaselinkerWindow = new AddToBaselinker(item);
                    addToBaselinkerWindow.Closed += AddToBaselinkerWindow_Closed;
                    addToBaselinkerWindow.ShowDialog();
                }
            }
        }

        private void AddToBaselinkerWindow_Closed(object sender, EventArgs e)
        {
            currentPage = 1;
            allRecords = null;
            AssortmentsTable.Items.Clear();
            LoadAssortmentsPage();

            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
            SearchMissingProductInBaselinkerSync();
        }

        private void AssortmentsBaselinkerRefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId) == null)
            {
                MessageBox.Show("Wystąpił błąd: \n nie wybrano domyślnego magazynu w Baselinker", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _ = FetchBaselinkerData.GetDataAsync(ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey));

            MessageBox.Show("Synchronizacja zakończona.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
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
                var progressDialog = new ProcessDialog("Importowanie asortymentu. Proszę czekać...");
                progressDialog.Show();

                BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey)
                );

                assortments_BaselinkerSyncProgressText.Text = "Pobieranie inwentarza";

                assortmentsBaselinkerSyncButton.IsEnabled = false;
                assortments_BaselinkerSyncProgressText.Visibility = Visibility.Visible;

                InventoryResponse inventories = await baselinkerAdapter.GetInventoriesAsync();

                if (inventories.status == "SUCCESS")
                {
                    var inventoriesList = inventories.inventories;

                    foreach (Inventory inventory in inventoriesList)
                    {
                        if (inventory.is_default)
                        {
                            assortments_BaselinkerSyncProgressText.Text = "Pobrano inwentarz \"" + inventory.name + "\". Wysyłanie zlecenia o pobranie produktów.";

                            BaselinkerSQLiteProductSyncComposite baselinkerSQLiteProductSyncComposite = new BaselinkerSQLiteProductSyncComposite();

                            assortments_BaselinkerSyncProgressText.Text = "Proszę czekać. Trwa wykonywanie importu.";

                            await baselinkerSQLiteProductSyncComposite.Sync(inventory.inventory_id);

                            assortmentsBaselinkerSyncButton.IsEnabled = true;
                            assortments_BaselinkerSyncProgressText.Visibility = Visibility.Hidden;
                            MessageBox.Show("Synchronizacja zakończona.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    throw new Exception(inventories.error_message);
                }

                progressDialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }




        private async void SyncInventoriesProductsStock_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProcessDialog("Trwa synchronizacja. Proszę czekać...");
            progressDialog.Show();

               await Task.Run(() =>
                    {
                        BaselinkerSyncInventoryQtyService.Sync();
                    });

            progressDialog.Close();

            MessageBox.Show("Synchronizacja zakończona", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private async void StartStopServiceSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (isServiceRunning)
            {
                isServiceRunning = false;
                StartStopServiceSyncButton.Content = "Uruchom serwis";
            }
            else
            {
                isServiceRunning = true;
                StartStopServiceSyncButton.Content = "Zatrzymaj serwis";
                await RunBackgroundServiceAsync();
            }
        }

        private async Task RunBackgroundServiceAsync()
        {
            while (isServiceRunning)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)IntervalSyncComboBox.SelectedItem;
                string intervalString = selectedItem.Content.ToString();

                if (double.TryParse(intervalString, out double interval))
                {

                    Console.WriteLine("Serwis działa...");
                    await Task.Run(() =>
                    {
                        BaselinkerSyncInventoryQtyService.Sync();
                    });
                    await Task.Delay(TimeSpan.FromMinutes(interval));
                }
                else
                {
                    MessageBox.Show("Błąd konwersji interwału.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);

                }
            }
        }


        private void IntervalSyncComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (IntervalSyncComboBox.SelectedItem != null)
            {
                string intervalString = IntervalSyncComboBox.SelectedItem.ToString();
                ConfigRepository.SetValue(RegistryConfigurationKeys.SyncServiceIntervalTime, intervalString);
                StartStopServiceSyncButton.IsEnabled = true;
            }
        }
    }



    public class AssortmentTableItem
    {
        public string BaselinkerId { get; set; }
        public string BaselinkerName { get; set; }
        public string Barcode { get; set; }
        public string SubiektSymbol { get; set; }
        public string SubiektId { get; set; }
        public string SubiektName { get; set; }
        public string SubiektPrice { get; set; }
        public string SubiektQty { get; set; }
        public string SubiektDescription { get; set; }
    }
}