using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using BaselinkerSubiektConnector.Support;
using BaselinkerSubiektConnector.Services.SQLiteService;

namespace BaselinkerSubiektConnector.Adapters
{
    public class MSSQLAdapter
    {
        private string connectionString;

        public MSSQLAdapter(string host, string username, string password = "")
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = host;
            builder.UserID = username;
            builder.Password = password;

            connectionString = builder.ConnectionString;
        }

        public List<string> GetNexoDatabaseNames()
        {
            List<string> databaseNames = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    DataTable databases = connection.GetSchema("Databases");

                    foreach (DataRow database in databases.Rows)
                    {
                        string databaseName = database.Field<string>("database_name"); 
                        if (databaseName.StartsWith("Nexo_", StringComparison.OrdinalIgnoreCase))
                        {
                            databaseNames.Add(databaseName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return databaseNames;
        }

        public List<string> GetProductFromEan(string dbName, string ean)
        {
            List<string> products = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $@"SELECT jma.Asortyment_Id
                                FROM {dbName}.ModelDanychContainer.KodyKreskowe kk
                                INNER JOIN {dbName}.ModelDanychContainer.JednostkiMiarAsortymentow jma
                                ON jma.Id = kk.JednostkaMiaryAsortymentu_Id
                                WHERE Kod = @EAN";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@EAN", ean);

                    SqlDataReader reader = command.ExecuteReader();
                    Helpers.Log(reader.ToString());
                    while (reader.Read())
                    {
                        string productId = reader["Asortyment_Id"].ToString();
                        products.Add(productId);
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return products;
        }


        public Services.SQLiteService.Record GetRecordFromEan(string dbName, string ean)
        {
            Record product = new Record();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $@"SELECT a.Id, a.Symbol, a.Nazwa
                                FROM {dbName}.ModelDanychContainer.KodyKreskowe kk
                                INNER JOIN {dbName}.ModelDanychContainer.JednostkiMiarAsortymentow jma
                                ON jma.Id = kk.JednostkaMiaryAsortymentu_Id
                                INNER JOIN {dbName}.ModelDanychContainer.Asortymenty a
                                ON a.Id = jma.Asortyment_Id
                                WHERE kk.Kod = @EAN";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@EAN", ean);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        product.subiekt_id = reader["Id"].ToString();
                        product.subiekt_name = reader["Nazwa"].ToString();
                        product.subiekt_symbol = reader["Symbol"].ToString();

                        return product;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return product;
        }

        public List<Services.SQLiteService.Record> GetAllAssortmentsFromWarehouse(string dbName, string warehouse)
        {
            List<Services.SQLiteService.Record> products = new List<Services.SQLiteService.Record>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $@"SELECT kk.Kod, a.Id, a.Symbol, a.Nazwa
                                     FROM Nexo_Demo_1.ModelDanychContainer.KodyKreskowe kk
                                     LEFT JOIN Nexo_Demo_1.ModelDanychContainer.JednostkiMiarAsortymentow jma
	                                    ON jma.Id = kk.JednostkaMiaryAsortymentu_Id
                                     LEFT JOIN Nexo_Demo_1.ModelDanychContainer.StanyMagazynowe sm
	                                    ON sm.Asortyment_Id = jma.Asortyment_Id
                                     LEFT JOIN Nexo_Demo_1.ModelDanychContainer.Asortymenty a
	                                    ON a.Id = sm.Asortyment_Id
                                     LEFT JOIN Nexo_Demo_1.ModelDanychContainer.Magazyny m
	                                    ON m.Id = sm.Magazyn_Id
                                     WHERE 
	                                    sm.IloscDostepna > 0
	                                    AND m.Symbol = @WAREHOUSE;";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@WAREHOUSE", warehouse);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Services.SQLiteService.Record product = new Services.SQLiteService.Record();
                        product.subiekt_id = reader["Id"].ToString();
                        product.subiekt_name = reader["Nazwa"].ToString();
                        product.subiekt_symbol = reader["Symbol"].ToString();
                        product.ean_code = reader["Kod"].ToString();

                        products.Add(product);
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return products;
        }

        public List<string> GetWarehouses(string dbName)
        {
            List<string> warehouses = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    string query = $@"SELECT Symbol
                                FROM {dbName}.ModelDanychContainer.Magazyny";

                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataReader reader = command.ExecuteReader();
                    Helpers.Log(reader.ToString());
                    while (reader.Read())
                    {
                        warehouses.Add(reader["Symbol"].ToString());
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return warehouses;
        }

        public List<string> GetBranches(string dbName)
        {
            List<string> warehouses = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    string query = $@"SELECT Symbol
                                FROM {dbName}.ModelDanychContainer.JednostkiOrganizacyjne";

                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        warehouses.Add(reader["Symbol"].ToString());
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return warehouses;
        }

        public List<string> GetCashRegisters(string dbName)
        {
            List<string> cashRegisters = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    string query = $@"SELECT Nazwa
                                FROM {dbName}.ModelDanychContainer.UrzadzeniaZewnetrzne";

                    SqlCommand command = new SqlCommand(query, connection);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        cashRegisters.Add(reader["Nazwa"].ToString());
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return cashRegisters;
        }

        public int GetWarehouseAssortmentQuantity(string dbName, int assortmentId, string warehouseSymbol)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $@"SELECT sm.IloscDostepna
                                FROM {dbName}.ModelDanychContainer.StanyMagazynowe sm
                                INNER JOIN {dbName}.ModelDanychContainer.Magazyny m
                                ON m.Id = sm.Magazyn_Id
                                WHERE sm.Asortyment_Id = @AssortmentId
                                AND m.Symbol = @Symbol;";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@AssortmentId", assortmentId.ToString());
                    command.Parameters.AddWithValue("@Symbol", warehouseSymbol);

                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        int qty = Convert.ToInt32(reader["IloscDostepna"]);
                        return qty;
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Helpers.Log("Error: " + ex.Message);
            }

            return 0;
        }

    }
}
