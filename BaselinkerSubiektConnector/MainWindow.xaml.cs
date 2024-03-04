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
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel
            {
                ExceptionHandler = HandleException
            };

            DataContext = ViewModel;
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
                DanePolaczenia = danePolaczenia,
                Produkt = ProductId.Subiekt,
                LoginNexo = "Szef",
                HasloNexo = "robocze"
            };
            return dane;
        }

        internal static DaneDoUruchomieniaSfery PodajDomyslneDaneDoUruchomienia()
        {
            var dane = new DaneDoUruchomieniaSfery()
            {
                Serwer = "(local)\\INSERTNEXO",
                Baza = "Nexo_Demo_1",
                Produkt = ProductId.Subiekt,
                LoginNexo = "Szef",
                HasloNexo = "robocze"
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
            if (danePolaczenia != null)
            {
                daneDoUruchomieniaSfery = PodajDaneDoUruchomienia(danePolaczenia);
            }
            else
            {
                daneDoUruchomieniaSfery = PodajDomyslneDaneDoUruchomienia();
            }

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
    }
}