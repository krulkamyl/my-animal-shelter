﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace NexoLink
{
    public partial class SalesViewControl : UserControl
    {
        private double prevVerticalOffset;
        private int itemsPerPage = 100;
        private int currentPage = 1;
        private List<SalesDocumentItem> allRecordsSalesDocs;
        private int currentPageSalesDocs = 1;

        public SalesViewControl()
        {
            InitializeComponent();
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
                        // TODO: below to sales docs
                        //LoadAssortmentsPage();
                    }
                }
            }
        }

        private void RefreshSellerDocsButton_Click(object sender, EventArgs e)
        {
            currentPageSalesDocs = 1;
            allRecordsSalesDocs = null;
            DocsTable.Items.Clear();

            // TODO: below to sales docs
            //LoadAssortmentsPage();

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

                        // TODO: below to sales docs
                        //LoadAssortmentsPage();
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
    }

    public class SalesDocumentItem
    {
        public int Status { get; set; }
        public string SubiektDocNumber { get; set; }
        public string CreatedAt { get; set; }
        public string BaselinkerId { get; set; }
        public string BaselinkerData { get; set; }
        public string Errors { get; set; }
        public string DocType { get; set; }
    }
}
