namespace BaselinkerSubiektConnector
{
    public sealed class PostepViewModel : PropertyChangedNotifier
    {
        private int _biezacyProcent;
        private string _opis;

        public int BiezacyProcent
        {
            get => _biezacyProcent;
            set
            {
                _biezacyProcent = value;
                OnPropertyChanged(nameof(BiezacyProcent));
            }
        }

        public string Opis
        {
            get => _opis;
            set
            {
                _opis = value;
                OnPropertyChanged(nameof(Opis));
            }
        }
    }
}