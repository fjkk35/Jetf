using Service.Models;
using System.Collections.Generic;

namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示 DownloadEtlNew 報表查詢結果。
    /// </summary>
    public sealed class DownloadEtlNewReportResult
    {
        /// <summary>
        /// 初始化報表查詢結果。
        /// </summary>
        public DownloadEtlNewReportResult()
        {
            status = Status.success;
            Rows = new List<DownloadEtlNewReportItem>();
        }

        /// <summary>
        /// 取得或設定執行狀態。
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 取得或設定執行訊息。
        /// </summary>
        public string msg { get; set; } = string.Empty;

        /// <summary>
        /// 取得或設定報表列資料。
        /// </summary>
        public List<DownloadEtlNewReportItem> Rows { get; set; }
    }
}