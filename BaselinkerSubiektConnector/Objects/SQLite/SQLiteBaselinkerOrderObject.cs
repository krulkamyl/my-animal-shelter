

namespace BaselinkerSubiektConnector.Objects.SQLite
{
    public class SQLiteBaselinkerOrderObject
    {
        public int? id { get; set; }
        public string baselinker_id { get; set; }
        public string customer_name { get; set; }
        public string price { get; set; }
        public string baselinker_data { get; set; }
        public string status_string { get; set; }
        public string created_at { get; set; }
    }

}
