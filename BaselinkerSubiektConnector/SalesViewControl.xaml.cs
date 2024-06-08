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
            HandleMouseWheel(DocsTable, e);
        }

        private void DocsTableProductsNotFound_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            HandleMouseWheel(DocsTable, e);
        }

        private void HandleMouseWheel(DataGrid dataGrid, MouseWheelEventArgs e)
        {
            var scrollViewer = GetScrollViewer(dataGrid);
            if (scrollViewer != null && e.Delta < 0 && scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
            {
                prevVerticalOffset = scrollViewer.VerticalOffset;
                LoadPage();
            }
        }

        private void RefreshSellerDocsButton_Click(object sender, EventArgs e)
        {
            ResetData();
            LoadPage();
            ScrollToTop(DocsTable);
        }

        private void ResetData()
        {
            currentPage = 1;
            allRecordsSalesDocs = null;
            DocsTable.Items.Clear();
        }

        private void ScrollToTop(DataGrid dataGrid)
        {
            var scrollViewer = GetScrollViewer(dataGrid);
            scrollViewer?.ScrollToVerticalOffset(0);
        }

        private void DocsTableProductsNotFound_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowContextMenuOnRightClick(sender as DataGrid, e);
        }

        private void ShowContextMenuOnRightClick(DataGrid dataGrid, MouseButtonEventArgs e)
        {
            if (dataGrid == null) return;

            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridCell))
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

        private void DocsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySelectedCellContentToClipboard(DocsTable);
        }

        private void CopySelectedCellContentToClipboard(DataGrid dataGrid)
        {
            var cell = GetSelectedCell(dataGrid);
            if (cell?.Content is TextBlock textBlock)
            {
                Clipboard.SetText(textBlock.Text);
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                scrollViewer = GetScrollViewer(child);
                if (scrollViewer != null)
                    return scrollViewer;
            }
            return null;
        }

        private DataGridCell GetSelectedCell(DataGrid dataGrid)
        {
            if (dataGrid?.SelectedCells.Count > 0)
            {
                var cellInfo = dataGrid.SelectedCells[0];
                return cellInfo.Column?.GetCellContent(cellInfo.Item)?.Parent as DataGridCell;
            }
            return null;
        }

        private void LoadPage()
        {
            if (allRecordsSalesDocs == null)
            {
                allRecordsSalesDocs = LoadSalesDocuments();
            }

            if (allRecordsSalesDocs.Count == 0)
            {
                ToggleDocsTableVisibility();
                return;
            }

            var pagedRecords = GetPagedRecords();
            AddRecordsToTable(pagedRecords);
            ScrollToPreviousOffset(DocsTable);
            currentPage++;
        }

        private List<SalesDocumentItem> LoadSalesDocuments()
        {
            return SQLiteService.ReadRecords(SQLiteDatabaseNames.GetSalesDocsDatabaseTable())
                .Select(record => new SalesDocumentItem
                {
                    Status = record.status.ToString(),
                    SubiektDocNumber = record.subiekt_doc_number ?? "---",
                    BaselinkerId = record.baselinker_id.Length > 3 ? "#" + record.baselinker_id : "",
                    CreatedAt = record.created_at,
                    Errors = record.errors ?? "---",
                    DocType = record.type
                }).ToList();
        }

        private void ToggleDocsTableVisibility()
        {
            DocsTable.Visibility = Visibility.Hidden;
            DocsTable.Visibility = Visibility.Visible;
        }

        private IEnumerable<SalesDocumentItem> GetPagedRecords()
        {
            var startIndex = (currentPage - 1) * itemsPerPage;
            var endIndex = Math.Min(startIndex + itemsPerPage, allRecordsSalesDocs.Count);
            return allRecordsSalesDocs.Skip(startIndex).Take(itemsPerPage);
        }

        private void AddRecordsToTable(IEnumerable<SalesDocumentItem> records)
        {
            foreach (var record in records)
            {
                DocsTable.Items.Add(record);
            }
        }

        private void ScrollToPreviousOffset(DataGrid dataGrid)
        {
            var scrollViewer = GetScrollViewer(dataGrid);
            scrollViewer?.ScrollToVerticalOffset(prevVerticalOffset);
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
                var cellInfo = e.AddedCells[0];
                if (cellInfo.Column?.DisplayIndex == 3)
                {
                    var errorText = cellInfo.Item.GetType().GetProperty("Errors")?.GetValue(cellInfo.Item, null)?.ToString();
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
            if (sender is TextBlock textBlock && textBlock.Text != "---")
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
            if (value is string stringValue && stringValue.Length > 3)
            {
                return "https://panel-e.baselinker.com/orders.php#order:" + stringValue;
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
            if (value is string stringValue && stringValue != "---")
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
