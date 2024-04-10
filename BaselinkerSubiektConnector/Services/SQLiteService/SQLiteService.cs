using BaselinkerSubiektConnector.Repositories.SQLite;
using BaselinkerSubiektConnector.Support;
using System;
using System.Collections.Generic;
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
                                            baselinker_id TEXT,
                                            baselinker_name TEXT,
                                            ean_code TEXT,
                                            subiekt_id TEXT,
                                            subiekt_symbol TEXT,
                                            subiekt_name TEXT
                                        );";
                    command.ExecuteNonQuery();

                    foreach (var databaseTable in new[] { "baselinker_warehouses", "baselinker_inventories", "baselinker_categories", "baselinker_inventory_price_groups", "baselinker_inventory_manufactuters", "subiekt_warehouses", "subiekt_branches", "subiekt_cashregisters" })
                    {
                        command.CommandText = $"CREATE TABLE IF NOT EXISTS {databaseTable} (id INTEGER PRIMARY KEY AUTOINCREMENT, key TEXT, value TEXT);";
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

        public static List<Record> ReadRecords(string tableName)
        {
            string queryString = $"SELECT * FROM {tableName};";

            List<Record> records = new List<Record>();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Record record = new Record();
                            record.id = Convert.ToInt32(reader["id"]);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                if (columnName != "id")
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        typeof(Record).GetProperty(columnName)?.SetValue(record, null);
                                    }
                                    else
                                    {
                                        if (typeof(Record).GetProperty(columnName) != null)
                                        {
                                            typeof(Record).GetProperty(columnName).SetValue(record, reader[columnName]);
                                        }
                                        else
                                        {
                                            record.AdditionalProperties[columnName] = reader[columnName];
                                        }
                                    }
                                }
                            }
                            records.Add(record);
                        }
                        Console.WriteLine();
                    }
                }

                connection.Close();
            }
            return records;
        }

        public static List<Record> GetAssortmentConnectedWithSubiekt()
        {
            string tableName = SQLiteDatabaseNames.GetAssortmentsDatabaseName();
            string queryString = $"SELECT * FROM {tableName} WHERE subiekt_id IS NOT NULL;";

            List<Record> records = new List<Record>();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Record record = new Record();
                            record.id = Convert.ToInt32(reader["id"]);
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                if (columnName != "id")
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        typeof(Record).GetProperty(columnName)?.SetValue(record, null);
                                    }
                                    else
                                    {
                                        if (typeof(Record).GetProperty(columnName) != null)
                                        {
                                            typeof(Record).GetProperty(columnName).SetValue(record, reader[columnName]);
                                        }
                                        else
                                        {
                                            record.AdditionalProperties[columnName] = reader[columnName];
                                        }
                                    }
                                }
                            }
                            records.Add(record);
                        }
                        Console.WriteLine();
                    }
                }

                connection.Close();
            }
            return records;
        }


        public static Record ReadRecord(string tableName, string column, string value)
        {
            string queryString = $"SELECT * FROM {tableName} WHERE {column} = @value";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    command.Parameters.AddWithValue("@value", value);

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Console.WriteLine(reader.ToString());
                            Record record = new Record();
                            record.id = Convert.ToInt32(reader["id"]);
                            foreach (var property in typeof(Record).GetProperties().Where(p => p.Name != "id"))
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                                {
                                    property.SetValue(record, reader[property.Name]);
                                }
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

        public static void DeleteRecords(string tableName)
        {
            string queryString = $"DELETE FROM {tableName}";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(queryString, connection))
                {
                    command.ExecuteNonQuery();
                }


                string vacuumQuery = "VACUUM";
                using (SQLiteCommand vacuumCommand = new SQLiteCommand(vacuumQuery, connection))
                {
                    vacuumCommand.ExecuteNonQuery();
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

    public class Record
    {
        public int id { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public string baselinker_id { get; set; }
        public string baselinker_name { get; set; }
        public string ean_code { get; set; }
        public string insert_symbol { get; set; }
        public string subiekt_id { get; set; }
        public string subiekt_symbol { get; set; }
        public string subiekt_name { get; set; }
        public string subiekt_price { get; set; }
        public string subiekt_qty { get; set; }
        public string subiekt_description { get; set; }



        public Dictionary<string, object> AdditionalProperties { get; } = new Dictionary<string, object>();
    }

    public interface IDynamicProperties
    {
        Dictionary<string, object> AdditionalProperties { get; }
    }

}
