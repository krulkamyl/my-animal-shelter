using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Storages;
using BaselinkerSubiektConnector.Services.HttpService;
using BaselinkerSubiektConnector.Support;
using InsERT.Moria.Klienci;
using InsERT.Moria.Sfera;
using InsERT.Mox.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();
        private MSSQLAdapter mssqlAdapter;
        private List<BaselinkerStoragesResponseStorage> storages; 
        private HttpService httpService;
        private DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();

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
        }

        private void LoadConfiguration()
        {
            MSSQL_IP.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Host);
            MSSQL_User.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Login);
            MSSQL_Password.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Password);

            Subiekt_User.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Login);
            Subiekt_Password.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Password);

            Baselinker_ApiKey.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);

            if (SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterEnabled) == "1")
            {
                Subiekt_CashRegisterEnabled.IsChecked = true;
            }

            if (SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_PrinterEnabled) == "1")
            {
                Subiekt_PrinterEnabled.IsChecked = true;
            }

            UpdateComboBox(Baselinker_StorageName, RegistryConfigurationKeys.Baselinker_StorageName);
            UpdateComboBox(MSSQL_Name, RegistryConfigurationKeys.MSSQL_DB_NAME);
            UpdateComboBox(Subiekt_DefaultBranch, RegistryConfigurationKeys.Subiekt_Default_Branch);
            UpdateComboBox(Subiekt_DefaultWarehouse, RegistryConfigurationKeys.Subiekt_Default_Warehouse);


            var defaultPrinter = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName);
            if (defaultPrinter.Length > 3)
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

        private void UpdateComboBox(ComboBox comboBox, string registryKey)
        {
            string value = SharedRegistryManager.GetValue(registryKey);

            if (value.Length > 0)
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

        public MainWindowViewModel ViewModel { get; }

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
                    Serwer = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                    Baza = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                    LoginSql = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                    HasloSql = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Password),
                    LoginNexo = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Login),
                    HasloNexo = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Password),
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
            DaneDoUruchomieniaSfery daneDoUruchomieniaSfery;

            var danePolaczenia = OdbierzDanePolaczeniaZInsLauncher();
            daneDoUruchomieniaSfery = PodajDaneDoUruchomienia(danePolaczenia);

            ViewModel.PolaczZeSfera(daneDoUruchomieniaSfery);
        }

        private void UruchomButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.UchwytDoSfery != null)
            {
                var okno = ViewModel.UchwytDoSfery.PodajObiektTypu<IPodmiotOkno>();
                _ = okno.Wybierz();
            }
        }

        private void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.MSSQL_Host, MSSQL_IP.Text);
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.MSSQL_Login, MSSQL_User.Text);
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.MSSQL_Password, MSSQL_Password.Text);
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.MSSQL_DB_NAME, MSSQL_Name.Text);


            int ProductIndex = Baselinker_StorageName.SelectedIndex;
            var selected = storages[ProductIndex];
            if (selected != null)
            {
                SharedRegistryManager.SetValue(RegistryConfigurationKeys.Baselinker_StorageId, selected.storage_id);
                SharedRegistryManager.SetValue(RegistryConfigurationKeys.Baselinker_StorageName, selected.name);
            }

            if (Subiekt_DefaultWarehouse.Text.Length > 0)
            {
                SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse, Subiekt_DefaultWarehouse.Text);
            }

            if (Subiekt_DefaultBranch.Text.Length > 0)
            {
                SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Default_Branch, Subiekt_DefaultBranch.Text);
            }

            if (Subiekt_PrinterName.Text.Length > 0)
            {
                SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_PrinterName, Subiekt_PrinterName.Text);
            }

            SharedRegistryManager.SetValue(
                RegistryConfigurationKeys.Subiekt_PrinterEnabled,
                Subiekt_PrinterEnabled.IsChecked == true ? "1" : "0"
                );

            SharedRegistryManager.SetValue(
                RegistryConfigurationKeys.Subiekt_CashRegisterEnabled,
                Subiekt_CashRegisterEnabled.IsChecked == true ? "1" : "0"
                );

            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Login, Subiekt_User.Text);
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Password, Subiekt_Password.Text);

            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Baselinker_ApiKey, Baselinker_ApiKey.Text);

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

        private void MSSQL_Name_SelectionChanged(Object sender, EventArgs e)
        {
            if (MSSQL_Name.Text.StartsWith("Nexo_")) { 
                Subiekt_DefaultWarehouse.Items.Clear();
                mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                List<string> warehouses = mssqlAdapter.GetWarehouses(MSSQL_Name.Text);
                List<string> branches = mssqlAdapter.GetBranches(MSSQL_Name.Text);
                if (warehouses.Count > 0)
                {
                    Subiekt_DefaultWarehouse.IsEnabled = true;
                }

                foreach (string warehouse in warehouses)
                {
                    Subiekt_DefaultWarehouse.Items.Add(warehouse);
                }

                if (branches.Count > 0)
                {
                    Subiekt_DefaultBranch.IsEnabled = true;
                }

                foreach (string branch in branches)
                {
                    if (!Subiekt_DefaultBranch.Items.Contains(branch))
                    {
                        Subiekt_DefaultBranch.Items.Add(branch);
                    }
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

        private void StopStartHttpService_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CzySferaJestUruchomiona)
            {
                httpService.StartStop();
            }
            else
            {
                MessageBox.Show("Sfera nie jest uruchiomiona.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CheckHttpServiceEnabled(object sender, EventArgs e)
        {
            if (httpService.IsEnabled)
            {
                HttpServiceCheck.Text = "Serwer HTTP działa. Adres: http://"+ httpService.ServerIpAddress+":"+httpService.port;
                HttpServiceCheck.Foreground = Brushes.Green;
            }
            else
            {
                HttpServiceCheck.Text = "Serwer HTTP nie jest uruchomiony.";
                HttpServiceCheck.Foreground = Brushes.Red;
            }
        }
    }
}