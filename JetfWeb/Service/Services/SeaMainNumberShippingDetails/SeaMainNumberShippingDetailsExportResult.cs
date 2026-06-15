using Service.Models;
using System.Collections.Generic;

namespace Service.Services.SeaMainNumberShippingDetails
{
    /// <summary>
    /// 海運主號託運明細表(無稅金)匯出結果。
    /// </summary>
    public sealed class SeaMainNumberShippingDetailsExportResult
    {
        /// <summary>
        /// 初始化海運主號託運明細表(無稅金)匯出結果。
        /// </summary>
        public SeaMainNumberShippingDetailsExportResult()
        {
            Rows = new List<SeaMainNumberShippingDetailsRow>();
            status = Status.success;
            msg = string.Empty;
        }

        /// <summary>
        /// 匯出狀態。
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 匯出訊息。
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 下載檔名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Excel 檔案內容。
        /// </summary>
        public byte[] FileBytes { get; set; }

        /// <summary>
        /// 匯出明細資料。
        /// </summary>
        public List<SeaMainNumberShippingDetailsRow> Rows { get; set; }
    }
}