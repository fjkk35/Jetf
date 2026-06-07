namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 代收金額人工調整查詢條件。
    /// </summary>
    public class SeaShenzhenFeeManualToDlvCodQueryRequest
    {
        /// <summary>
        /// 託運單號。
        /// </summary>
        public string TrackingNo { get; set; }

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