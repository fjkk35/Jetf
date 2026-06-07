namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞託運資料查詢條件。
    /// </summary>
    public class SeaShenzhenOriginalQueryRequest
    {
        /// <summary>
        /// 資料日期起。
        /// </summary>
        public string DataDateStart { get; set; }

        /// <summary>
        /// 資料日期迄。
        /// </summary>
        public string DataDateEnd { get; set; }

        public string TrackingNo { get; set; }

        public string BlNo { get; set; }

        public string OrderNo { get; set; }

        public string JetfSerial { get; set; }

        public string Importer { get; set; }

        public string ImporterPhone { get; set; }

        /// <summary>
        /// 稅金支付方式代碼。
        /// </summary>
        public string TaxPayment { get; set; }

        /// <summary>
        /// 頁碼。
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}