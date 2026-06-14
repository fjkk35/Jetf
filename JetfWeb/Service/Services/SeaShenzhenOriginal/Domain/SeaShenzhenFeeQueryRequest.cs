namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞稅金資料查詢條件。
    /// </summary>
    public class SeaShenzhenFeeQueryRequest
    {
        /// <summary>
        /// 資料日期起。
        /// </summary>
        public string DataDateStart { get; set; }

        /// <summary>
        /// 資料日期迄。
        /// </summary>
        public string DataDateEnd { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 稅金支付方式代碼。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 報關行資料類型代碼。
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 頁碼。
        /// </summary>
        public int PageIndex { get; set; }
    }
}