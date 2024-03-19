using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;

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
                Console.WriteLine("Error: " + ex.Message);
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
                    Console.WriteLine(reader.ToString());
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
                Console.WriteLine("Error: " + ex.Message);
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
                    Console.WriteLine(reader.ToString());
                    while (reader.Read())
                    {
                        warehouses.Add(reader["Symbol"].ToString());
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return warehouses;
        }

    }
}
