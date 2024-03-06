using BaselinkerSubiektConnector.Adapters;
using InsERT.Moria.Klienci;
using InsERT.Moria.Sfera;
using InsERT.Mox.Launcher;
using InsERT.Mox.Product;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();
        private MSSQLAdapter mssqlAdapter;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel
            {
                ExceptionHandler = HandleException
            };

            LoadConfiguration();

            DataContext = ViewModel;
        }

        private void LoadConfiguration()
        {
            MSSQL_IP.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Host);
            MSSQL_User.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Login);
            MSSQL_Password.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_Password);

            Subiekt_User.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Login);
            Subiekt_Password.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Password);

            Baselinker_ApiKey.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);

            if (SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME).Length > 0)
            {
                MSSQL_Name.Items.Clear();

                MSSQL_Name.Items.Add(
                    SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME)
                );
                MSSQL_Name.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME);
            } else
            {
                MSSQL_Name.IsEnabled = false;
            }

        }

        public MainWindowViewModel ViewModel { get; }

        internal static DanePolaczenia OdbierzDanePolaczeniaZInsLauncher()
        {
            var commandLineArguments = Environment.GetCommandLineArgs();
            if (commandLineArguments != null && commandLineArguments.Contains(@"/UruchomionePrzezInsLauncher"))
            {
                //pobieramy parametry podane przez Launcher
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

            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Login, Subiekt_User.Text);
            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Subiekt_Password, Subiekt_Password.Text);

            SharedRegistryManager.SetValue(RegistryConfigurationKeys.Baselinker_ApiKey, Baselinker_ApiKey.Text);

            MessageBox.Show("Możesz spróbować połączyć się ze Sferą", "Konfiguracja zapisana pomyślnie!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MssqlTest_Click(object sender, RoutedEventArgs e)
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

        private async void BaselinkerTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(Baselinker_ApiKey.Text);

                var storagesList = await baselinkerAdapter.GetStoragesListAsync();

                if (storagesList.status == "SUCCESS")
                {
                    var alertText = "Udało się nawiązać połączenie z BaseLinker. Dostępne magazyny:\n";

                    foreach ( var storage in storagesList.storages )
                    {
                        alertText += " • " + storage.name + "\n";
                    }

                    MessageBox.Show(alertText, "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}