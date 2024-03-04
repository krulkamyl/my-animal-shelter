using System.Windows.Threading;

namespace BaselinkerSubiektConnector
{
    internal static class Interaction
    {
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        public static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;

            return null;
        }
    }
}