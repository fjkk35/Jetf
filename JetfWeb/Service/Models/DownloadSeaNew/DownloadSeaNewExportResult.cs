using Service.EnumTax;
using System.Collections.Generic;

namespace Service.Models.DownloadSeaNew
{
    /// <summary>
    /// 海運下載匯出結果。
    /// </summary>
    public sealed class DownloadSeaNewExportResult
    {
        /// <summary>
        /// 初始化海運下載匯出結果。
        /// </summary>
        public DownloadSeaNewExportResult()
        {
            status = Status.success;
            FileName = string.Empty;
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
        /// 匯出檔名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 匯出檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// 匯出資料列。
        /// </summary>
        public List<DownloadSeaNewReportItem> Rows { get; set; }
    }
}
