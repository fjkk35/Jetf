using System.Collections.Generic;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細上傳檔案的原始資料列。
    /// </summary>
    public sealed class ReconciliationLogisticsComparisonUploadRow
    {
        /// <summary>
        /// Excel 或 CSV 顯示列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 原始檔案欄位值，順序對應上傳檔案欄位名稱。
        /// </summary>
        public List<string> Values { get; set; } = new List<string>();
    }
}
