using System.Collections.Generic;

namespace Service.Models
{
    /// <summary>
    /// 處置說明報表查詢結果。
    /// </summary>
    public sealed class ProcessReportResultModel
    {
        /// <summary>查詢狀態。</summary>
        public string status { get; set; } = Status.success;
        /// <summary>查詢訊息。</summary>
        public string msg { get; set; } = "";
        /// <summary>報表資料列。</summary>
        public List<ProcessReportRowModel> Rows { get; set; } = new List<ProcessReportRowModel>();
    }
}
