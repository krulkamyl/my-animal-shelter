
namespace BaselinkerSubiektConnector.Objects.SQLite
{
    public class SQLiteSalesDocObject
    {
        public int? id { get; set; }
        public string baselinker_id { get; set; }
        public string type { get; set; }
        public string subiekt_doc_number { get; set; }
        public string baselinker_data { get; set; }
        public string errors { get; set; }
        public int? status { get; set; }
        public string created_at { get; set; }
    }

}
