using InsERT.Moria.Sfera;
using System;
using System.Windows;

namespace BaselinkerSubiektConnector
{
    public sealed class MainWindowViewModel : PropertyChangedNotifier
    {
        private string _baza;
        private bool _czySferaJestUruchomiona;
        private bool _czyTrwaUruchamianieSfery;

        public MainWindowViewModel()
        {
            PostepLadowania = new PostepViewModel()
            {
                BiezacyProcent = 0,
                Opis = string.Empty
            };
        }

        public string Baza
        {
            get => _baza;
            set
            {
                _baza = value;
                OnPropertyChanged(nameof(Baza));
                OnPropertyChanged(nameof(CzyJestBaza));
            }
        }

        public bool CzyJestBaza => !string.IsNullOrEmpty(Baza);

        public bool CzySferaJestUruchomiona
        {
            get => _czySferaJestUruchomiona;
            set
            {
                _czySferaJestUruchomiona = value;
                OnPropertyChanged(nameof(CzySferaJestUruchomiona));
            }
        }

        public bool CzyTrwaUruchamianieSfery
        {
            get => _czyTrwaUruchamianieSfery;
            set
            {
                _czyTrwaUruchamianieSfery = value;
                OnPropertyChanged(nameof(CzyTrwaUruchamianieSfery));
            }
        }

        public PostepViewModel PostepLadowania { get; }

        public string WersjaSfery => DanePolaczenia.WersjaSfery.ToString(4);

        internal Action<Exception> ExceptionHandler { get; set; }
        internal Uchwyt UchwytDoSfery { get; private set; }

        internal void PolaczZeSfera(DaneDoUruchomieniaSfery daneDoUruchomieniaSfery)
        {
            // Uwaga: tworzenie uchwytu musi się odbywać w wątku UI.
            try
            {
                if (UchwytDoSfery == null)
                {
                    Baza = daneDoUruchomieniaSfery.Baza;
                    CzyTrwaUruchamianieSfery = true;

                    var postep = new PostepLadowaniaSfery(PostepLadowania);
                    UchwytDoSfery = Uchwyty.UtworzNowy(daneDoUruchomieniaSfery, postep);

                    CzySferaJestUruchomiona = true;
                }
            }
            catch (Exception e)
            {
                ExceptionHandler?.Invoke(e);
            }
            finally
            {
                CzyTrwaUruchamianieSfery = false;
            }
        }
    }
}