namespace Service.Services.ShipmentInboundProcess.Domain
{
    public class ShipmentInboundProcessRequest
    {
        /// <summary>
        /// 進口方式
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 入庫日期(起)，格式：yyyy-MM-dd
        /// </summary>
        public string InboundDateStart { get; set; }

        /// <summary>
        /// 入庫日期(迄)，格式：yyyy-MM-dd
        /// </summary>
        public string InboundDateEnd { get; set; }

        /// <summary>
        /// 客戶代碼(單一，相容舊版)
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶代碼(多選)
        /// </summary>
        public string[] CustCodes { get; set; }

        /// <summary>
        /// 貨件來源
        /// </summary>
        public int? SourceType { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 結案狀態：null=全部，true=是，false=否
        /// </summary>
        public bool? IsClosed { get; set; }

        /// <summary>
        /// 是否原始貨件
        /// </summary>
        public bool? IsOrderOriginal { get; set; }

        /// <summary>
        /// 頁碼
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數
        /// </summary>
        public int PageSize { get; set; }
    }
}
