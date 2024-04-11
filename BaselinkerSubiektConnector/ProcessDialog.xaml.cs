using System.Windows;

namespace BaselinkerSubiektConnector
{
    public partial class ProcessDialog : Window
    {
        public ProcessDialog(string statusMessage)
        {
            InitializeComponent();
            statusTextBlock.Text = statusMessage;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
