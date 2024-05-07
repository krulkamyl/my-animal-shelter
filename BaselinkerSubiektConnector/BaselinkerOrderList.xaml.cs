using BaselinkerSubiektConnector;
using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Objects.Baselinker.Orders;
using BaselinkerSubiektConnector.Objects.SQLite;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
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
        private List<BaselinkerOrderItem> allRecords;
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

            RefreshData();
        }

        private async void GetOrders(object sender, EventArgs e)
        {
            string baselinkerApiKey = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);
            if (baselinkerApiKey != null && baselinkerApiKey.Length > 10)
            {
                this.baselinkerAdapter = new BaselinkerAdapter(baselinkerApiKey);
                BaselinkerOrderResponse baselinkerOrderResponse = await baselinkerAdapter.GetOrdersAsync();
                baselinkerOrderResponseOrders = baselinkerOrderResponse.orders;


                foreach (BaselinkerOrderResponseOrder baselinkerOrderItem in baselinkerOrderResponseOrders)
                {

                    DateTime date = UnixTimeStampToDateTime(baselinkerOrderItem.date_confirmed);

                    SQLiteBaselinkerOrderObject obj = new SQLiteBaselinkerOrderObject();
                    obj.baselinker_id = baselinkerOrderItem.order_id.ToString();

                    if (baselinkerOrderItem.invoice_fullname.Length > 3)
                    {
                        obj.customer_name = baselinkerOrderItem.invoice_fullname.ToString();
                    }
                    else
                    {
                        obj.customer_name = baselinkerOrderItem.delivery_fullname.ToString();
                    }

                    obj.status_string = baselinkerOrderItem.order_status_id.ToString();

                    double priceProducts = 0.00;

                    foreach (BaselinkerOrderResponseOrderProduct baselinkerOrderResponseOrderProduct in baselinkerOrderItem.products)
                    {
                        priceProducts += (double)baselinkerOrderResponseOrderProduct.price_brutto;
                    }

                    priceProducts += (double)baselinkerOrderItem.delivery_price;

                    obj.price = priceProducts.ToString() +" "+ baselinkerOrderItem.currency;


                    obj.created_at = date.ToString("dd-MM-yyyy HH:mm");

                    string json = JsonConvert.SerializeObject(baselinkerOrderItem);
                    obj.baselinker_data = json;

                    BaselinkerOrderRepository.CreateRecordWhenNotExist("baselinker_id", baselinkerOrderItem.order_id.ToString(), obj);
                }

            }
        }

        private DateTime UnixTimeStampToDateTime(int? unixTimeStamp)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return unixEpoch.AddSeconds((double)unixTimeStamp).ToLocalTime();
        }


        private void RefreshBaselinkerList_Click(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            currentPage = 1;
            allRecords = null;
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
            if (allRecords == null)
            {
                allRecords = SQLiteService.GetBaselinkerOrders()
                    .Select(record => new BaselinkerOrderItem
                    {
                        Status = record.status_string.ToString(),
                        SubiektDocNumber = record.subiekt_doc_number ?? "---",
                        BaselinkerId = record.baselinker_id.Length > 3 ? "#" + record.baselinker_id : "",
                        CreatedAt = record.created_at,
                        OrderPerson = record.customer_name,
                        OrderPrice = record.price,
                        ShowButton = (record.subiekt_doc_number ?? "---").Length == 3 ? Visibility.Visible : Visibility.Hidden
                    }).ToList();

            }

            if (allRecords.Count == 0)
            {
                DocsTable.Visibility = Visibility.Hidden;
                DocsTable.Visibility = Visibility.Visible;
                return;
            }

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, allRecords.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                DocsTable.Items.Add(allRecords[i]);
            }
            ScrollViewer scrollViewer = GetScrollViewer(DocsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(prevVerticalOffset);
            }
            currentPage++;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var baselinkerId = (sender as Button)?.CommandParameter as string;
            baselinkerId = baselinkerId.Replace("#", "");

            Console.WriteLine($"Kliknięto przycisk dla BaselinkerId: {baselinkerId}");
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
        public string CreatedAt { get; set; }
        public string Action { get; set; }
        public Visibility ShowButton { get; set; }
    }
}
