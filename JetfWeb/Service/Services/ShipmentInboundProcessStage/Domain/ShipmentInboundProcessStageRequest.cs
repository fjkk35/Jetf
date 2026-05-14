namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記處理查詢條件。
    /// </summary>
    public class ShipmentInboundProcessStageRequest
    {
        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 輸入日期起，格式 yyyy-MM-dd。
        /// </summary>
        public string CreatedTimeStart { get; set; }

        /// <summary>
        /// 輸入日期迄，格式 yyyy-MM-dd。
        /// </summary>
        public string CreatedTimeEnd { get; set; }

        /// <summary>
        /// 輸入人員。
        /// </summary>
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 匹配日期起，格式 yyyy-MM-dd。
        /// </summary>
        public string MatchTimieStart { get; set; }

        /// <summary>
        /// 匹配日期迄，格式 yyyy-MM-dd。
        /// </summary>
        public string MatchTimieEnd { get; set; }

        /// <summary>
        /// 匹配狀態，null=全部、true=是、false=否。
        /// </summary>
        public bool? IsMatched { get; set; }

        /// <summary>
        /// 目前頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}
