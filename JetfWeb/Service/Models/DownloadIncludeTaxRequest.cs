namespace Service.Models
{
    /// <summary>
    /// 稅金總表及明細表匯出條件。
    /// </summary>
    public sealed class DownloadIncludeTaxRequest
    {
        /// <summary>
        /// 查詢開始日期，格式為 yyyy-MM-dd。
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// 查詢結束日期，格式為 yyyy-MM-dd。
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// 資料來源，1 為海運、3 為空運。
        /// </summary>
        public string Source { get; set; }
    }
}
