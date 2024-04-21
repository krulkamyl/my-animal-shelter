using System.Collections.Generic;

namespace BaselinkerSubiektConnector.Objects.Baselinker.Storages
{
    public class BaselinkerStoragesResponse
    {
        public string status { get; set; }
        public string error_code { get; set; }
        public string error_message { get; set; }
        public List<BaselinkerStoragesResponseStorage> storages { get; set; }
    }

    public class BaselinkerStoragesResponseStorage
    {
        public string storage_id { get; set; }
        public string name { get; set; }
        public List<string> methods { get; set; }
        public bool read { get; set; }
        public bool write { get; set; }
    }


}
