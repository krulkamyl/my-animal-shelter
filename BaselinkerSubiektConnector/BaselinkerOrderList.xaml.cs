using BaselinkerSubiektConnector;
using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Repositories.SQLite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NexoLink
{

    public partial class BaselinkerOrderList : UserControl
    {
        private double prevVerticalOffset;
        private int itemsPerPage = 100;
        private int currentPage = 1;
        private List<SalesDocumentItem> allRecordsSalesDocs;
        private BaselinkerAdapter baselinkerAdapter;
        private List<BaselinkerOrderResponseOrder> baselinkerOrderResponseOrders;
        private DispatcherTimer timer;

        public BaselinkerOrderList()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += GetOrders;
            timer.Start();
        }

        private async void GetOrders(object sender, EventArgs e)
        {
            string baselinkerApiKey = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
            if (baselinkerApiKey != null && baselinkerApiKey.Length > 10)
            {
                this.baselinkerAdapter = new BaselinkerAdapter(baselinkerApiKey);
                BaselinkerOrderResponse baselinkerOrderResponse = await baselinkerAdapter.GetOrdersAsync();
                baselinkerOrderResponseOrders = baselinkerOrderResponse.orders;


                string json = JsonConvert.SerializeObject(baselinkerOrderResponseOrders);
                Console.WriteLine("Nowe zamówienia: ");
                Console.WriteLine(json);
            }
        }


        private void RefreshBaselinkerList_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            allRecordsSalesDocs = null;
            DocsTable.Items.Clear();

            LoadPage();

            ScrollViewer scrollViewer = GetScrollViewer(DocsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void LoadPage()
        {

        }

        private void DocsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                DataGrid dataGrid = DocsTable;
                if (dataGrid != null)
                {
                    DataGridCell cell = GetSelectedCell(dataGrid);

                    if (cell != null && cell.Content is TextBlock)
                    {
                        TextBlock textBlock = cell.Content as TextBlock;
                        string cellContent = textBlock.Text;
                        Clipboard.SetText(cellContent);
                    }
                }
            }
        }

        private void Table_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = GetScrollViewer(DocsTable);
            if (scrollViewer != null)
            {
                if (e.Delta < 0)
                {
                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        prevVerticalOffset = scrollViewer.VerticalOffset;

                        LoadPage();
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

        private DataGridCell GetSelectedCell(DataGrid dataGrid)
        {
            if (dataGrid == null || dataGrid.SelectedCells.Count == 0)
                return null;

            DataGridCellInfo cellInfo = dataGrid.SelectedCells[0];
            if (cellInfo == null)
                return null;

            DataGridCell cell = null;
            try
            {
                cell = (DataGridCell)cellInfo.Column.GetCellContent(cellInfo.Item).Parent;
            }
            catch
            {
                cell = null;
            }

            return cell;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null)
                return null;

            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        private childItem GetVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = GetVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }

    public class BaselinkerOrderItem
    {
        public string BaselinkerId { get; set; }
        public string Status { get; set; }
        public string OrderPerson { get; set; }
        public string OrderPrice { get; set; }
        public string SubiektDocNumber { get; set; }
        public string Action { get; set; }
    }
}
