using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Services.EmailService;
using BaselinkerSubiektConnector.Services.HttpService;
using InsERT.Moria.Sfera;
using InsERT.Mox.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Threading.Timer;
using Helpers = BaselinkerSubiektConnector.Support.Helpers;
using System.IO;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Support;
using BaselinkerSubiektConnector.Objects.Baselinker.Inventory;
using BaselinkerSubiektConnector.Composites;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using BaselinkerSubiektConnector.Builders.Baselinker;
using System.Text;
using System.Windows.Documents;
using System.Diagnostics;
using System.Data;
using NexoLink;
using System.Security.Principal;
using System.Threading;

namespace BaselinkerSubiektConnector
{
    public partial class MainWindow : Window
    {
        private MSSQLAdapter mssqlAdapter;
        private HttpService httpService;
        private DispatcherTimer timer;
        private static Timer checkSferaIsEnabled;
        private List<AssortmentTableItem> allRecords;
        private int itemsPerPage = 100;
        private int currentPage = 1;
        private double prevVerticalOffset;
        private bool isServiceRunning = false;
        private AddToBaselinker addToBaselinkerWindow; 
        private const int MaxLogLines = 400;
        private ConfigurationControl configurationControl = null;
        private BaselinkerOrderList baselinkerOrderList = null;
        private SalesViewControl salesViewControl = null;
        private string logFilePath = Path.Combine(Helpers.GetApplicationPath(), "Logs.txt");
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private static Timer autorunSfera;

        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            CheckAppDataFolderExists();
            InitializeDatabase();
            LoadConfig();


            ViewModel = new MainWindowViewModel
            {
                ExceptionHandler = HandleException
            };

            string assetsFolderPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Assets");

            Uri iconUri = new Uri("pack://application:,,,/nexo-linker-logo.ico");
            Stream iconStream = Application.GetResourceStream(iconUri)?.Stream;

            if (iconStream != null)
            {
                notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                notifyIcon.Text = "NexoLink";
            }

            notifyIcon.MouseClick += NotifyIcon_MouseClick;

            this.ResizeMode = ResizeMode.CanMinimize;

            this.WindowStyle = WindowStyle.SingleBorderWindow;

            this.StateChanged += MainWindow_StateChanged;


            configurationControl = new ConfigurationControl();
            salesViewControl = new SalesViewControl();


            DataContext = ViewModel;

            httpService = new HttpService();
            httpService.mainWindowViewModel = ViewModel;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += CheckHttpServiceEnabled;
            timer.Start();

            checkSferaIsEnabled = new Timer(CheckSferaIsEnabledMethod, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));


            SferaAutoRun.Checked += SferaAutoRun_Checked;
            SferaAutoRun.Unchecked += SferaAutoRun_Checked;


            Closing += MainWindow_Closing;

            LoadAssortmentsPage();
            SearchMissingProductInBaselinkerSync();


            ReadLogFromFile();
            WatchLogFileChanges();

            autorunSfera = new Timer(CheckAutoRunSfera, null, 3000, Timeout.Infinite);

        }

        private void CheckAutoRunSfera(object state)
        {

            Dispatcher.Invoke(() =>
            {
                if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Sfera_Autorun) == "1")
                {
                    SferaAutoRun.IsChecked = true;

                    PolaczButton_Click(this, new RoutedEventArgs());
                }
            });
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                notifyIcon.Visible = true;
            }
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                notifyIcon.Visible = false;
            }
        }

        private void LoadConfig()
        {
            if (ConfigRepository.GetValue(RegistryConfigurationKeys.AutoRun_IntervalSyncQtyWarehouse) == "1")
            {
                AutoSyncCheckbox.IsChecked = true;
                StartStopServiceSyncButton_Click(this, new RoutedEventArgs());
            }

            string intervalTime = ConfigRepository.GetValue(RegistryConfigurationKeys.SyncServiceIntervalTime);

            string[] intervalValues = { "15", "30", "60", "120", "180" };

            IntervalSyncComboBox.Items.Clear();

            foreach (string value in intervalValues)
            {
                IntervalSyncComboBox.Items.Add(value);
            }

            if (intervalTime != null && IntervalSyncComboBox.Items.Contains(intervalTime))
            {
                IntervalSyncComboBox.SelectedItem = intervalTime;
            }
            string emailTemplate = ConfigRepository.GetValue(RegistryConfigurationKeys.Email_Template);

            if (emailTemplate != null && emailTemplate.Length > 50)
            {
                EmailTemplate.Text = emailTemplate;
            }
            else
            {
                EmailTemplate.Text = EmailService.GetEmailTemplate();
            }
        }


        private void SferaAutoRun_Checked(object sender, RoutedEventArgs e)
        {
            if (SferaAutoRun.IsChecked == true)
            {
                string subiektLogin = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Login);
                string mssqlName = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME);

                if (subiektLogin.Length > 2 && mssqlName.Length > 3 && mssqlName.Contains("Nexo_"))
                {
                    ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Sfera_Autorun, "1");
                }
                else
                {
                    MessageBox.Show("Brak danych do logowania do sfery", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
                    SferaAutoRun.IsChecked = false;
                }
            }
            else
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Sfera_Autorun, "0");
            }
        }


        private void CheckAppDataFolderExists()
        {
            if (!Directory.Exists(Helpers.GetApplicationPath()))
            {
                try
                {
                    Directory.CreateDirectory(Helpers.GetApplicationPath());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Wystąpił błąd podczas tworzenia folderu '{Helpers.GetApplicationPath()}': {ex.Message}", "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                SQLiteService.InitializeDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SQLITE] " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                MessageBox.Show(ex.Message, "Błąd SQLite!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }



        private void CheckSferaIsEnabledMethod(object state)
        {
            if (ViewModel.CzySferaJestUruchomiona)
            {
                httpService.StartStop();
                checkSferaIsEnabled.Dispose();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (httpService.IsEnabled)
            {
                httpService.Stop();
            }
        }



        internal static DanePolaczenia OdbierzDanePolaczeniaZInsLauncher()
        {
            var commandLineArguments = Environment.GetCommandLineArgs();
            if (commandLineArguments != null && commandLineArguments.Contains(@"/UruchomionePrzezInsLauncher"))
            {
                return DanePolaczenia.Odbierz();
            }

            return null;
        }

        internal static DaneDoUruchomieniaSfery PodajDaneDoUruchomienia(DanePolaczenia danePolaczenia)
        {
            var dane = new DaneDoUruchomieniaSfery()
            {
                Serwer = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host),
                Baza = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_DB_NAME),
                LoginSql = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login),
                HasloSql = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password),
                LoginNexo = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Login),
                HasloNexo = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Password),
                Produkt = ProductId.Subiekt,
            };
            return dane;
        }

        private void HandleException(Exception e)
        {
            _ = MessageBox.Show(e.ToString());
        }

        private void PolaczButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                configurationControl.sendDataToValidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Wystąpił błąd walidacyjny", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (httpService.IsEnabled)
            {
                httpService.Stop();
            }
            else
            {
                if (!ViewModel.CzySferaJestUruchomiona)
                {
                    if (IsAdministrator())
                    {
                        DaneDoUruchomieniaSfery daneDoUruchomieniaSfery;
                        Helpers.EnsureExportFolderExists();
                        Helpers.StartLog();

                        var danePolaczenia = OdbierzDanePolaczeniaZInsLauncher();
                        daneDoUruchomieniaSfery = PodajDaneDoUruchomienia(danePolaczenia);

                        ViewModel.PolaczZeSfera(daneDoUruchomieniaSfery);
                    } else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                        startInfo.Verb = "runas";
                        try
                        {
                            Process.Start(startInfo);
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            MessageBox.Show("Nie można uruchomić aplikacji jako administrator. Wymagany on jest do działania nasłuchu danych z Baselinker.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        Environment.Exit(0);
                    }
                }
            }
        }

        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        private void CheckHttpServiceEnabled(object sender, EventArgs e)
        {
            if (httpService.IsEnabled)
            {
                HttpServiceEnabled.Text = "Serwer HTTP działa. Adres: http://" + httpService.ServerIpAddress + ":" + httpService.port;
                HttpServiceEnabledIcon.Fill = Brushes.Green;
                ButtonEnableDisableApp.Content = "Wyłacz";
            }
            else
            {
                HttpServiceEnabled.Text = "Serwer HTTP wyłączony";
                HttpServiceEnabledIcon.Fill = Brushes.Red;
                ButtonEnableDisableApp.Content = "Włącz";
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            currentPage = 1;
            allRecords = null;
            AssortmentsTable.Items.Clear();
            LoadAssortmentsPage();

            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void LoadAssortmentsPage()
        {
            if (allRecords == null)
            {
                allRecords = SQLiteService.ReadRecords(SQLiteDatabaseNames.GetAssortmentsDatabaseName())
                                          .Select(record => new AssortmentTableItem
                                          {
                                              BaselinkerId = record.baselinker_id,
                                              BaselinkerName = record.baselinker_name,
                                              Barcode = record.ean_code,
                                              SubiektName = record.subiekt_name ?? "---",
                                              SubiektSymbol = record.subiekt_symbol ?? "---"
                                          }).ToList();
            }

            if (allRecords.Count == 0)
            {
                AssortmentsTable.Visibility = Visibility.Hidden;
                AssortmentsTableProductsNotFound.Visibility = Visibility.Visible;
                return;
            }

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Math.Min(startIndex + itemsPerPage, allRecords.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                AssortmentsTable.Items.Add(allRecords[i]);
            }
            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(prevVerticalOffset);
            }
            currentPage++;
        }

        private void AssortmentsTable_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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


        private void AssortmentsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                DataGrid dataGrid = AssortmentsTable;
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

        private void MissingBaselinkerProductsCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                DataGrid dataGrid = MissingBaselinkerProducts;
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

        private void SearchMissingProductInBaselinker_Click(object sender, RoutedEventArgs e)
        {
            SearchMissingProductInBaselinkerSync();
        }

        private void SearchMissingProductInBaselinkerSync()
        {
            if (mssqlAdapter == null)
            {
                mssqlAdapter = new MSSQLAdapter(
                    
                    ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host), ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login), ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password));
            }

            GetMissingInBaselinkerSubiektProducts getMissingInBaselinkerSubiektProducts = new GetMissingInBaselinkerSubiektProducts();
            List<Record> missingProducts = getMissingInBaselinkerSubiektProducts.Sync(allRecords, mssqlAdapter);
            if (missingProducts.Count > 0)
            {
                MissingBaselinkerProducts.Items.Clear();

                MissingBaselinkerProducts.Visibility = Visibility.Visible;
                MissingBaselinkerProductsNotFound.Visibility = Visibility.Hidden;
                var missingAssortmentTableItems = missingProducts
                                          .Select(record => new AssortmentTableItem
                                          {
                                              Barcode = record.ean_code,
                                              SubiektName = record.subiekt_name ?? "---",
                                              SubiektSymbol = record.subiekt_symbol ?? "---",
                                              SubiektId = record.subiekt_id,
                                              SubiektDescription = record.subiekt_description,
                                              SubiektPrice = record.subiekt_price,
                                              SubiektQty = record.subiekt_qty,
                                          }).ToList();

                foreach (var missingAssortmentTableItem in missingAssortmentTableItems)
                {
                    MissingBaselinkerProducts.Items.Add(missingAssortmentTableItem);
                }
            }
            else
            {
                MissingBaselinkerProductsNotFound.Visibility = Visibility.Visible;
                MissingBaselinkerProducts.Visibility = Visibility.Hidden;
            }
        }

        private void EmailTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Email_Template, EmailTemplate.Text);

                MessageBox.Show("Zapisano szablon e-mail", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddMissingProductToBaselinker_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var item = button.DataContext as AssortmentTableItem;
                if (item != null)
                {
                    addToBaselinkerWindow = new AddToBaselinker(item);
                    addToBaselinkerWindow.Closed += AddToBaselinkerWindow_Closed;
                    addToBaselinkerWindow.ShowDialog();
                }
            }
        }

        private void MissingBaselinkerProducts_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
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

        private void AddToBaselinkerWindow_Closed(object sender, EventArgs e)
        {
            currentPage = 1;
            allRecords = null;
            AssortmentsTable.Items.Clear();
            LoadAssortmentsPage();

            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(0);
            }
            SearchMissingProductInBaselinkerSync();
        }

        private void AssortmentsTable_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = GetScrollViewer(AssortmentsTable);
            if (scrollViewer != null)
            {
                if (e.Delta < 0)
                {
                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        prevVerticalOffset = scrollViewer.VerticalOffset;
                        // TODO: below to sales docs
                        LoadAssortmentsPage();
                    }
                }
            }
        }

        private void AssortmentsBaselinkerRefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId) == null)
            {
                MessageBox.Show("Wystąpił błąd: \n nie wybrano domyślnego magazynu w Baselinker", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _ = FetchBaselinkerData.GetDataAsync(ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey));

            MessageBox.Show("Synchronizacja zakończona.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private async void AssortmentsBaselinkerSyncButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_StorageId) == null)
            {
                MessageBox.Show("Wystąpił błąd: \n nie wybrano domyślnego magazynu w Baselinker", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var progressDialog = new ProcessDialog("Importowanie asortymentu. Proszę czekać...");
                progressDialog.Show();

                BaselinkerAdapter baselinkerAdapter = new BaselinkerAdapter(
                    ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey)
                );

                assortments_BaselinkerSyncProgressText.Text = "Pobieranie inwentarza";

                assortmentsBaselinkerSyncButton.IsEnabled = false;
                assortments_BaselinkerSyncProgressText.Visibility = Visibility.Visible;

                InventoryResponse inventories = await baselinkerAdapter.GetInventoriesAsync();

                if (inventories.status == "SUCCESS")
                {
                    var inventoriesList = inventories.inventories;

                    foreach (Inventory inventory in inventoriesList)
                    {
                        if (inventory.is_default)
                        {
                            assortments_BaselinkerSyncProgressText.Text = "Pobrano inwentarz \"" + inventory.name + "\". Wysyłanie zlecenia o pobranie produktów.";

                            BaselinkerSQLiteProductSyncComposite baselinkerSQLiteProductSyncComposite = new BaselinkerSQLiteProductSyncComposite();

                            assortments_BaselinkerSyncProgressText.Text = "Proszę czekać. Trwa wykonywanie importu.";

                            await baselinkerSQLiteProductSyncComposite.Sync(inventory.inventory_id);

                            assortmentsBaselinkerSyncButton.IsEnabled = true;
                            assortments_BaselinkerSyncProgressText.Visibility = Visibility.Hidden;
                            MessageBox.Show("Synchronizacja zakończona.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                else
                {
                    throw new Exception(inventories.error_message);
                }

                progressDialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private void AutoSyncChecked(object sender, RoutedEventArgs e)
        {
            ConfigRepository.SetValue(RegistryConfigurationKeys.AutoRun_IntervalSyncQtyWarehouse, "1");

            if (IntervalSyncComboBox.SelectedItem != null)
            {
                string intervalString = IntervalSyncComboBox.SelectedItem.ToString();

                ConfigRepository.SetValue(RegistryConfigurationKeys.AutoRun_IntervalSyncQtyWarehouse, "1");
                ConfigRepository.SetValue(RegistryConfigurationKeys.SyncServiceIntervalTime, intervalString);
            }
            else
            {
                AutoSyncCheckbox.IsChecked = false;
                MessageBox.Show("Nie wybrano interwału pracy", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);

            }
        }

        private void AutoSyncUnchecked(object sender, RoutedEventArgs e)
        {
            ConfigRepository.SetValue(RegistryConfigurationKeys.AutoRun_IntervalSyncQtyWarehouse, "0");
        }

        private async void SyncInventoriesProductsStock_Click(object sender, RoutedEventArgs e)
        {
            var progressDialog = new ProcessDialog("Trwa synchronizacja. Proszę czekać...");
            progressDialog.Show();

            await Task.Run(() =>
                 {
                     BaselinkerSyncInventoryQtyService.Sync();
                 });

            progressDialog.Close();

            MessageBox.Show("Synchronizacja zakończona", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private async void StartStopServiceSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (isServiceRunning)
            {
                isServiceRunning = false;
                StartStopServiceSyncButton.Content = "Uruchom serwis";
            }
            else
            {
                isServiceRunning = true;
                StartStopServiceSyncButton.Content = "Zatrzymaj serwis";
                await RunBackgroundServiceAsync();
            }
        }

        private async Task RunBackgroundServiceAsync()
        {
            while (isServiceRunning)
            {
                string intervalString = ConfigRepository.GetValue(RegistryConfigurationKeys.SyncServiceIntervalTime);

                if (double.TryParse(intervalString, out double interval))
                {

                    Console.WriteLine("Serwis działa...");
                    await Task.Run(() =>
                    {
                        BaselinkerSyncInventoryQtyService.Sync();
                    });
                    await Task.Delay(TimeSpan.FromMinutes(interval));
                }
                else
                {
                    MessageBox.Show("Błąd konwersji interwału.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);

                }
            }
        }


        private void IntervalSyncComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (IntervalSyncComboBox.SelectedItem != null)
            {
                string intervalString = IntervalSyncComboBox.SelectedItem.ToString();
                ConfigRepository.SetValue(RegistryConfigurationKeys.SyncServiceIntervalTime, intervalString);
                StartStopServiceSyncButton.IsEnabled = true;
            }
        }
        private void WatchLogFileChanges()
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(logFilePath);
            watcher.Filter = Path.GetFileName(logFilePath);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnLogFileChanged;
            watcher.EnableRaisingEvents = true;
        }

        private void OnLogFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ReadLogFromFile();
            });
        }

        private void ReadLogFromFile()
        {
            try
            {
                if (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length == 0)
                {
                    noLogsTextBlock.Visibility = Visibility.Visible;
                    logTextBox.Text = "";
                    return;
                }

                noLogsTextBlock.Visibility = Visibility.Collapsed;

                string[] allLines = File.ReadAllLines(logFilePath, Encoding.UTF8);
                int startIndex = Math.Max(0, allLines.Length - MaxLogLines);

                StringBuilder stringBuilder = new StringBuilder();

                for (int i = allLines.Length - 1; i >= startIndex; i--)
                {
                    stringBuilder.AppendLine(allLines[i]);
                }

                logTextBox.Text = stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Wystąpił błąd podczas odczytu pliku logów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            if (hyperlink != null)
            {
                string navigateUri = hyperlink.NavigateUri.AbsoluteUri;
                Process.Start(new ProcessStartInfo(navigateUri));
                e.Handled = true;
            }
        }

    }

    public class AssortmentTableItem
    {
        public string BaselinkerId { get; set; }
        public string BaselinkerName { get; set; }
        public string Barcode { get; set; }
        public string SubiektSymbol { get; set; }
        public string SubiektId { get; set; }
        public string SubiektName { get; set; }
        public string SubiektPrice { get; set; }
        public string SubiektQty { get; set; }
        public string SubiektDescription { get; set; }
    }
}