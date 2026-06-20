using Service.Models;
using Service.Models.SeaCustomerShippingDetails;
using System.Collections.Generic;

namespace Service.Services.SeaCustomerShippingDetails
{
    /// <summary>
    /// 海運客戶託運明細表匯出結果。
    /// </summary>
    public sealed class SeaCustomerShippingDetailsExportResult
    {
        /// <summary>
        /// 初始化海運客戶託運明細表匯出結果。
        /// </summary>
        public SeaCustomerShippingDetailsExportResult()
        {
            Files = new List<SeaCustomerShippingDetailsDownloadFile>();
            Rows = new List<SeaCustomerShippingDetailsRow>();
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
        /// 下載檔案清單。
        /// </summary>
        public List<SeaCustomerShippingDetailsDownloadFile> Files { get; set; }

        /// <summary>
        /// 匯出明細資料。
        /// </summary>
        public List<SeaCustomerShippingDetailsRow> Rows { get; set; }
    }
}
