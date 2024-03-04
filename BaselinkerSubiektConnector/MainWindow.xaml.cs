using InsERT.Moria.Klienci;
using InsERT.Moria.Sfera;
using InsERT.Mox.Product;
using System;
using System.Linq;
using System.Windows;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        internal static RegistryManager SharedRegistryManager { get; } = new RegistryManager();

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
            MSSQL_Name.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME);

            Subiekt_User.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Login);
            Subiekt_Password.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Subiekt_Password);

            Baselinker_ApiKey.Text = SharedRegistryManager.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);

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
    }
}