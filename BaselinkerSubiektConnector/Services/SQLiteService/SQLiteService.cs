using BaselinkerSubiektConnector.Support;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace BaselinkerSubiektConnector.Services.SQLiteService
{
    class SQLiteService
    {

        static string connectionString = $"Data Source={SQLiteService.getDatabasePath()}";

        public static void InitializeDatabase()
        {
            if (!File.Exists(SQLiteService.getDatabasePath()))
            {

                SQLiteConnection.CreateFile(SQLiteService.getDatabasePath());

            }

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS config (
                                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            key TEXT,
                                            value TEXT
                                        );";
                    command.ExecuteNonQuery();


                    command.CommandText = @"CREATE TABLE IF NOT EXISTS assortments (
                                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            baselinker_external_id TEXT,
                                            ean_code TEXT,
                                            insert_symbol TEXT
                                        );";
                    command.ExecuteNonQuery();

                    foreach (var warehouseType in new[] { "baselinker_warehouses", "subiekt_warehouses", "subiekt_branches", "subiekt_cashregisters" })
                    {
                        command.CommandText = $"CREATE TABLE IF NOT EXISTS {warehouseType} (id INTEGER PRIMARY KEY AUTOINCREMENT, key TEXT, value TEXT);";
                        command.ExecuteNonQuery();

                    }
                }

                connection.Close();
            }
        }

        public static void CreateRecord(string tableName, object values)
        {
            string columns = string.Join(",", values.GetType().GetProperties().Select(p => p.Name));
            string parameters = string.Join(",", values.GetType().GetProperties().Select(p => $"@{p.Name}"));

            string queryString = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    foreach (var property in values.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(values));
                    }

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public static void ReadRecords(string tableName)
        {
            string queryString = $"SELECT * FROM {tableName}";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine($"Records from {tableName}:");
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write($"{reader.GetValue(i)} ");
                            }
                            Console.WriteLine();
                        }
                        Console.WriteLine();
                    }
                }

                connection.Close();
            }
        }

        public static Record ReadRecord(string tableName, string column, string value)
        {
            string queryString = $"SELECT * FROM {tableName} WHERE @column = @value";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@column", column);
                    command.Parameters.AddWithValue("@value", value);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Record record = new Record();
                            record.id = Convert.ToInt32(reader["id"]);
                            foreach (var property in typeof(Record).GetProperties().Where(p => p.Name != "id"))
                            {
                                property.SetValue(record, reader[property.Name]);
                            }
                            return record;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }

        public static void UpdateRecord(string tableName, int id, object values)
        {
            string setValues = string.Join(",", values.GetType().GetProperties().Select(p => $"{p.Name} = @{p.Name}"));

            string queryString = $"UPDATE {tableName} SET {setValues} WHERE id = @id";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    foreach (var property in values.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(values));
                    }

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public static void DeleteRecord(string tableName, int id)
        {
            string queryString = $"DELETE FROM {tableName} WHERE id = @id";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        private static string getDatabasePath()
        {
            return System.IO.Path.Combine(Helpers.GetApplicationPath(), "database.sqlite");
        }

        private static string getPassword()
        {
            return "RandomPassword123";
        }
    }

    class Record
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string baselinker_external_id { get; set; }
        public string ean_code { get; set; }
        public string insert_symbol { get; set; }
    }
}
