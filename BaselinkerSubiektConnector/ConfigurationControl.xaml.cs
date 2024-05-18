using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Services.EmailService;
using BaselinkerSubiektConnector.Support;
using BaselinkerSubiektConnector;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BaselinkerSubiektConnector.Services.SQLiteService;
using BaselinkerSubiektConnector.Adapters;
using BaselinkerSubiektConnector.Builders.Baselinker;
using BaselinkerSubiektConnector.Validators;
using WpfMessageBoxLibrary;

namespace NexoLink
{
    public partial class ConfigurationControl : UserControl
    {

        private MSSQLAdapter mssqlAdapter;

        public ConfigurationControl()
        {
            InitializeComponent();

            LoadConfiguration();
        }


        private void LoadConfiguration()
        {

            LoadItemsAndSetDefault(Subiekt_DefaultWarehouse, SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(),
                RegistryConfigurationKeys.Subiekt_Default_Warehouse);
            LoadItemsAndSetDefault(Subiekt_DefaultBranch, SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Branch);
            LoadItemsAndSetDefault(Subiekt_CashRegisterName, SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(), RegistryConfigurationKeys.Subiekt_CashRegisterName);
            LoadItemsAndSetDefault(Baselinker_StorageName, SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName(), RegistryConfigurationKeys.Baselinker_StorageName);
            LoadItemsAndSetDefault(Baselinker_InventoryWarehouseName, SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName(), RegistryConfigurationKeys.Baselinker_InventoryWarehouseName);
            LoadItemsAndSetDefault(Subiekt_Login, SQLiteDatabaseNames.GetSubiektLoginsDatabaseName(), RegistryConfigurationKeys.Subiekt_Login);

            UpdateComboBox(MSSQL_Name, RegistryConfigurationKeys.MSSQL_DB_NAME);

            MSSQL_IP.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Host);
            MSSQL_User.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Login);
            MSSQL_Password.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSSQL_Password);


            Subiekt_Password.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_Password);

            Baselinker_ApiKey.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_ApiKey);

            Config_EmailServer.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailServer);
            Config_EmailPort.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPort);
            Config_EmailLogin.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailLogin);
            Config_EmailPassword.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailPassword);
            MSTEAMS_WEBHOOK_URL.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.MSTeams_Webhook_Url);


            Config_EmailReporting.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailReporting);

            Config_CompanyName.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyName);
            Config_CompanyNip.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyNip);
            Config_CompanyAddress.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyAddress);
            Config_CompanyZipCode.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyZipCode);
            Config_CompanyCity.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyCity);
            Config_CompanyEmailAddress.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress);
            Config_CompanyPhone.Text = ConfigRepository.GetValue(RegistryConfigurationKeys.Config_CompanyPhone);


            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_CashRegisterEnabled) == "1")
            {
                Subiekt_CashRegisterEnabled.IsChecked = true;
            }

            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterEnabled) == "1")
            {
                Subiekt_PrinterEnabled.IsChecked = true;
            }

            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Config_EmailSendAuto) == "1")
            {
                Config_EmailSendAuto.IsChecked = true;
            }


            if (ConfigRepository.GetValue(RegistryConfigurationKeys.Baselinker_AddCommentDocNumber) == "1")
            {
                Baselinker_AddCommentDocNumber.IsChecked = true;
            }

            var defaultPrinter = ConfigRepository.GetValue(RegistryConfigurationKeys.Subiekt_PrinterName);
            if (defaultPrinter != null && defaultPrinter.Length > 3)
            {
                Subiekt_PrinterName.Items.Add(defaultPrinter);
                Subiekt_PrinterName.SelectedItem = defaultPrinter;
            }
            foreach (string printer in Helpers.GetPrinters())
            {
                if (defaultPrinter != printer)
                {
                    Subiekt_PrinterName.Items.Add(printer);
                }
            }

        }

        private void LoadItemsAndSetDefault(System.Windows.Controls.ComboBox comboBox, string databaseName, string registryKey)
        {
            List<Record> records = SQLiteService.ReadRecords(databaseName);

            if (records.Count > 0)
            {
                comboBox.Items.Clear();
                foreach (Record record in records)
                {
                    comboBox.Items.Add(record.key);
                }
            }

            var defaultValue = ConfigRepository.GetValue(registryKey);
            if (!string.IsNullOrEmpty(defaultValue) && defaultValue.Length > 1)
            {
                comboBox.SelectedItem = defaultValue;
            }
        }

        public void sendDataToValidate()
        {
            ConfigValidatorModel model = new ConfigValidatorModel
            {
                MssqlDatabaseName = MSSQL_Name.Text,
                MsTeamsWebhookUrl = MSTEAMS_WEBHOOK_URL.Text,
                BaselinkerStorage = Baselinker_StorageName.Text,
                BaselinkerInventoryWarehouse = Baselinker_InventoryWarehouseName.Text,
                SubiektWarehouse = Subiekt_DefaultWarehouse.Text,
                SubiektBranch = Subiekt_DefaultBranch.Text,
                PrinterEnabled = (bool)Subiekt_PrinterEnabled.IsChecked,
                PrinterName = Subiekt_PrinterName.Text,
                CashRegisterEnabled = (bool)Subiekt_CashRegisterEnabled.IsChecked,
                CashRegisterName = Subiekt_CashRegisterName.Text,
                SendEmailEnabled = (bool)Config_EmailSendAuto.IsChecked,
                EmailLogin = Config_EmailLogin.Text,
                EmailPassword = Config_EmailPassword.Text,
                EmailReporting = Config_EmailReporting.Text,
                CompanyName = Config_CompanyName.Text,
                CompanyNip = Config_CompanyNip.Text,
                CompanyEmail = Config_CompanyEmailAddress.Text
            };
            ConfigValidator.Validate(model);
        }

        private async void SaveConfiguration_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                sendDataToValidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Wystąpił błąd walidacyjny", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Host, MSSQL_IP.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Login, MSSQL_User.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_Password, MSSQL_Password.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSSQL_DB_NAME, MSSQL_Name.Text);


            Record bastelinkerStorageSelected = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName(),
                "key",
                Baselinker_StorageName.Text
            );

            if (bastelinkerStorageSelected != null)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageId, bastelinkerStorageSelected.value);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_StorageName, bastelinkerStorageSelected.key);
            }


            Record baselinkerInventoryWarehouseSelected = SQLiteService.ReadRecord(
                SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName(),
                "key",
                Baselinker_InventoryWarehouseName.Text
            );


            if (baselinkerInventoryWarehouseSelected != null)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_InventoryWarehouseId, baselinkerInventoryWarehouseSelected.value);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_InventoryWarehouseName, baselinkerInventoryWarehouseSelected.key);
            }


            if (Subiekt_DefaultWarehouse.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Default_Warehouse, Subiekt_DefaultWarehouse.Text);
            }

            if (Subiekt_CashRegisterName.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_CashRegisterName, Subiekt_CashRegisterName.Text);
            }

            if (Subiekt_DefaultBranch.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Default_Branch, Subiekt_DefaultBranch.Text);
            }

            if (Subiekt_PrinterName.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_PrinterName, Subiekt_PrinterName.Text);
            }

            if (Subiekt_Login.Text.Length > 0)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Login, Subiekt_Login.Text);
            }

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Subiekt_PrinterEnabled,
                Subiekt_PrinterEnabled.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Subiekt_CashRegisterEnabled,
                Subiekt_CashRegisterEnabled.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Config_EmailSendAuto,
                Config_EmailSendAuto.IsChecked == true ? "1" : "0"
                );


            ConfigRepository.SetValue(
                RegistryConfigurationKeys.Baselinker_AddCommentDocNumber,
                Baselinker_AddCommentDocNumber.IsChecked == true ? "1" : "0"
                );

            ConfigRepository.SetValue(RegistryConfigurationKeys.Subiekt_Password, Subiekt_Password.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Baselinker_ApiKey, Baselinker_ApiKey.Text);


            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailServer, Config_EmailServer.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPort, Config_EmailPort.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailLogin, Config_EmailLogin.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPassword, Config_EmailPassword.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.MSTeams_Webhook_Url, MSTEAMS_WEBHOOK_URL.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailReporting, Config_EmailReporting.Text);

            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyName, Config_CompanyName.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyNip, Config_CompanyNip.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyAddress, Config_CompanyAddress.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyZipCode, Config_CompanyZipCode.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyCity, Config_CompanyCity.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyEmailAddress, Config_CompanyEmailAddress.Text);
            ConfigRepository.SetValue(RegistryConfigurationKeys.Config_CompanyPhone, Config_CompanyPhone.Text);

            _ = await FetchBaselinkerData.GetDataAsync(
                Baselinker_ApiKey.Text,
                null,
                true
            );

            MessageBox.Show("Możesz spróbować połączyć się ze Sferą", "Konfiguracja zapisana pomyślnie!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MssqlGetDbName(object sender, RoutedEventArgs e)
        {
            try
            {
                MSSQL_Name.Items.Clear();


                mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                List<string> databaseNames = mssqlAdapter.GetNexoDatabaseNames();

                if (databaseNames.Count > 0)
                {
                    MSSQL_Name.IsEnabled = true;
                }

                foreach (string dbName in databaseNames)
                {
                    MSSQL_Name.Items.Add(dbName);
                }

                MessageBox.Show("Pobrano listę dostępnych baz!", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            if (Config_EmailLogin.Text.Length > 3 && Config_EmailPassword.Text.Length > 3 && Config_EmailPort.Text.Length >= 2 && Config_EmailServer.Text.Length > 3)
            {
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailServer, Config_EmailServer.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPort, Config_EmailPort.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailLogin, Config_EmailLogin.Text);
                ConfigRepository.SetValue(RegistryConfigurationKeys.Config_EmailPassword, Config_EmailPassword.Text);

                var msgProperties = new WpfMessageBoxProperties()
                {
                    Button = MessageBoxButton.OKCancel,
                    ButtonOkText = "Wyślij",
                    CheckBoxText = "Anuluj",
                    Image = MessageBoxImage.Information,
                    Header = "Testowanie wysyłania e-mail",
                    IsCheckBoxChecked = true,
                    IsCheckBoxVisible = false,
                    IsTextBoxVisible = true,
                    Text = "Wprowadź swój adres e-mail, na którego ma zostać wysłany testowa wiadomość e-mail.",
                    Title = "Test e-mail",
                };

                MessageBoxResult result = WpfMessageBox.Show(Application.Current.MainWindow, ref msgProperties);
                if (msgProperties.TextBoxText.Length > 3 && msgProperties.TextBoxText.Contains("@"))
                {
                    EmailService emailService = new EmailService();
                    emailService.SendEmail(msgProperties.TextBoxText, "Testowa wiadomosć e-mail", "Jeżeli otrzymałeś/aś tę wiadomość e-mail, to znaczy, że wysyłanie e-maili działa!");
                    MessageBox.Show("Wiadomość została wysłana. Sprawdź swój adres e-mail.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
                }

            }
            else
            {
                MessageBox.Show("Wystąpił błąd: \n" + "Nie uzupełniono wszystkich pól wymaganych dla działania wysyłania wiadomości e-mail.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void MSSQL_Name_DropDownClosed(Object sender, EventArgs e)
        {
            if (MSSQL_Name.Text.StartsWith("Nexo_"))
            {
                Subiekt_DefaultWarehouse.Items.Clear();
                Subiekt_DefaultBranch.Items.Clear();
                Subiekt_CashRegisterName.Items.Clear();
                if (mssqlAdapter == null)
                {
                    mssqlAdapter = new MSSQLAdapter(MSSQL_IP.Text, MSSQL_User.Text, MSSQL_Password.Text);
                }

                SubiektWarehouses.UpdateExistingData(mssqlAdapter.GetWarehouses(MSSQL_Name.Text));
                SubiektBranches.UpdateExistingData(mssqlAdapter.GetBranches(MSSQL_Name.Text));
                SubiektCashRegisters.UpdateExistingData(mssqlAdapter.GetCashRegisters(MSSQL_Name.Text));

                SubiektLogins.UpdateExistingData(mssqlAdapter.GetLogins(MSSQL_Name.Text));

                LoadItemsAndSetDefault(Subiekt_DefaultWarehouse, SQLiteDatabaseNames.GetSubiektWarehousesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Warehouse);
                LoadItemsAndSetDefault(Subiekt_DefaultBranch, SQLiteDatabaseNames.GetSubiektBranchesDatabaseName(), RegistryConfigurationKeys.Subiekt_Default_Branch);
                LoadItemsAndSetDefault(Subiekt_CashRegisterName, SQLiteDatabaseNames.GetSubiektCashRegistersDatabaseName(), RegistryConfigurationKeys.Subiekt_CashRegisterName);
                LoadItemsAndSetDefault(Subiekt_Login, SQLiteDatabaseNames.GetSubiektLoginsDatabaseName(), RegistryConfigurationKeys.Subiekt_Login);

                if (Subiekt_DefaultWarehouse.Items.Count > 0)
                {
                    Subiekt_DefaultWarehouse.IsEnabled = true;
                }

                if (Subiekt_DefaultBranch.Items.Count > 0)
                {
                    Subiekt_DefaultBranch.IsEnabled = true;
                }

                if (Subiekt_CashRegisterName.Items.Count > 0)
                {
                    Subiekt_CashRegisterName.IsEnabled = true;
                }



            }
        }

        private async void BaselinkerGetStorage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = await FetchBaselinkerData.GetDataAsync(
                    Baselinker_ApiKey.Text
                );

                List<Record> records = SQLiteService.ReadRecords(
                    SQLiteDatabaseNames.GetBaselinkerStoragesDatabaseName()
                    );

                Baselinker_StorageName.Items.Clear();

                foreach (Record item in records)
                {
                    Baselinker_StorageName.Items.Add(item.key);
                }

                List<Record> recordsWarehouses = SQLiteService.ReadRecords(
                    SQLiteDatabaseNames.GetBaselinkerInventoryWarehousesDatabaseName()
                    );


                Baselinker_InventoryWarehouseName.Items.Clear();

                foreach (Record item in recordsWarehouses)
                {
                    Baselinker_InventoryWarehouseName.Items.Add(item.key);
                }

                MessageBox.Show("Pobrano dane z Baselinker. Możesz wybrać magazyn.", "Sukces!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd: \n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateComboBox(System.Windows.Controls.ComboBox comboBox, string registryKey)
        {
            string value = ConfigRepository.GetValue(registryKey);

            if (value != null && value.Length > 0)
            {
                comboBox.Items.Clear();
                comboBox.Items.Add(value);
                comboBox.Text = value;
            }
            else
            {
                comboBox.IsEnabled = false;
            }
        }
    }


   
}
