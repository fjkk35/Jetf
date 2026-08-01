namespace Service.Models
{
    /// <summary>
    /// 稅金總表或客戶總表的彙總資料列。
    /// </summary>
    public sealed class DownloadIncludeTaxReportSummaryModel
    {
        /// <summary>
        /// 資料日期。
        /// </summary>
        public string DataDate { get; set; }

        /// <summary>
        /// 資料來源。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 稅金一合計。
        /// </summary>
        public long Tax1 { get; set; }

        /// <summary>
        /// 稅金二合計。
        /// </summary>
        public long Tax2 { get; set; }

        /// <summary>
        /// 到付款合計。
        /// </summary>
        public long Cod { get; set; }
    }
}
