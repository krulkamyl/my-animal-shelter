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
    }
}
