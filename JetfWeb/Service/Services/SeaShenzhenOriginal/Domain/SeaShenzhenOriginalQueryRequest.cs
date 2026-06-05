namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢條件。
    /// </summary>
    public class SeaShenzhenOriginalQueryRequest
    {
        public string DataDateStart { get; set; }

        public string DataDateEnd { get; set; }

        public string TrackingNo { get; set; }

        public string BlNo { get; set; }

        public string OrderNo { get; set; }

        public string JetfSerial { get; set; }

        public string Importer { get; set; }

        public string ImporterPhone { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }
    }
}