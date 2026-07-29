using System.Collections.Generic;

namespace Service.Services.ReconciliationIncludeTaxDownload.Domain
{
    /// <summary>
    /// 包稅客戶明細下載的單一 Excel 檔案資料群組。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadFileGroup
    {
        /// <summary>
        /// 檔案名稱主體，會再套用檔案名稱安全化處理。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 此檔案包含的明細資料。
        /// </summary>
        public List<ReconciliationIncludeTaxDownloadRow> Rows { get; set; }
    }
}
