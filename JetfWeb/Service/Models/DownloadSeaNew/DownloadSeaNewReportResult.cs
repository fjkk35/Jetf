using Service.EnumTax;
using System.Collections.Generic;

namespace Service.Models.DownloadSeaNew
{
    /// <summary>
    /// 海運下載報表結果。
    /// </summary>
    public sealed class DownloadSeaNewReportResult
    {
        /// <summary>
        /// 初始化海運下載報表結果。
        /// </summary>
        public DownloadSeaNewReportResult()
        {
            status = Status.success;
            Rows = new List<DownloadSeaNewReportItem>();
        }

        /// <summary>
        /// 執行狀態。
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 執行訊息。
        /// </summary>
        public string msg { get; set; } = string.Empty;

        /// <summary>
        /// 報表資料列。
        /// </summary>
        public List<DownloadSeaNewReportItem> Rows { get; set; }
    }
}
