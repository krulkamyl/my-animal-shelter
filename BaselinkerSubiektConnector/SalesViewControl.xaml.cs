using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;


namespace NexoLink
{
    public partial class SalesViewControl : UserControl
    {
        private double prevVerticalOffset;
        private int itemsPerPage = 100;
        private int currentPage = 1;
        private List<SalesDocumentItem> allRecordsSalesDocs;

        public SalesViewControl()
        {
            InitializeComponent();
            LoadPage();
        }


        private void AssortmentsTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void RefreshSellerDocsButton_Click(object sender, EventArgs e)
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

        private void DocsTableProductsNotFound_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void DocsTableProductsNotFound_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                DependencyObject dep = (DependencyObject)e.OriginalSource;

                while ((dep != null) && !(dep is DataGridCell))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (dep is DataGridCell)
                {
                    dataGrid.ContextMenu.Visibility = Visibility.Visible;
                    dataGrid.ContextMenu.IsOpen = true;
                }
                else
                {
                    dataGrid.ContextMenu.Visibility = Visibility.Collapsed;
                }
            }
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

        private void LoadPage()
        {
            if (allRecordsSalesDocs == null)
            {
                allRecordsSalesDocs = SQLiteService.ReadRecords(SQLiteDatabaseNames.GetSalesDocsDatabaseTable())
                    .Select(record => new SalesDocumentItem
                    {
                        Status = record.status.ToString(),
                        SubiektDocNumber = record.subiekt_doc_number ?? "---",
                        BaselinkerId = record.baselinker_id.Length > 3 ? "#"+record.baselinker_id : "",
                        CreatedAt = record.created_at,
                        Errors = record.errors ?? "---",
                        DocType = record.type
                    }).ToList();
            }

            if (allRecordsSalesDocs.Count == 0)
            {
                DocsTable.Visibility = Visibility.Hidden;
                DocsTable.Visibility = Visibility.Visible;
                return;
            }

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, allRecordsSalesDocs.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                DocsTable.Items.Add(allRecordsSalesDocs[i]);
            }
            ScrollViewer scrollViewer = GetScrollViewer(DocsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(prevVerticalOffset);
            }
            currentPage++;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void DocsTable_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (e.AddedCells.Count > 0)
            {
                DataGridCellInfo cellInfo = e.AddedCells[0];
                if (cellInfo.Column != null && cellInfo.Column.DisplayIndex == 3)
                {
                    var errorText = cellInfo.Item.GetType().GetProperty("Errors").GetValue(cellInfo.Item, null)?.ToString();

                    if (!string.IsNullOrEmpty(errorText) && errorText != "---")
                    {
                        Clipboard.SetText(errorText);
                        MessageBox.Show("Tekst błędu skopiowany do schowka:\n\n" + errorText, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SubiektDocNumber_Clicked(object sender, RoutedEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null || textBlock.Text != "---")
            {
                var fileName = textBlock.Text.Replace("/", "_");
                var filepath = Helpers.GetExportApplicationPath() + "\\" + fileName + ".pdf";
                if (File.Exists(filepath))
                {
                    Process.Start(filepath);
                }
                else
                {
                    MessageBox.Show("Plik nie został odnaleziony.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }


    }

    public class SalesDocumentItem
    {
        public string Status { get; set; }
        public string SubiektDocNumber { get; set; }
        public string CreatedAt { get; set; }
        public string BaselinkerId { get; set; }
        public string BaselinkerData { get; set; }
        public string Errors { get; set; }
        public string DocType { get; set; }
    }

    public class UrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.ToString().Length > 3)
            {
                return "https://panel-e.baselinker.com/orders.php#order:" + value.ToString();
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocNumberPropertiesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.ToString() != "---")
            {
                return new DocNumberProperties
                {
                    IsHyperlink = true,
                    ForegroundColor = Brushes.Blue,
                    TextDecorations = TextDecorations.Underline
                };
            }

            return new DocNumberProperties
            {
                IsHyperlink = false,
                ForegroundColor = Brushes.Black,
                TextDecorations = null
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocNumberProperties
    {
        public bool IsHyperlink { get; set; }
        public Brush ForegroundColor { get; set; }
        public TextDecorationCollection TextDecorations { get; set; }
    }
}
