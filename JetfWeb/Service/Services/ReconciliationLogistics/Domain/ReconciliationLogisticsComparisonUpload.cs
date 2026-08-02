using System.Collections.Generic;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳比對明細上傳檔案的原始欄位與資料列。
    /// </summary>
    public sealed class ReconciliationLogisticsComparisonUpload
    {
        /// <summary>
        /// 上傳檔案欄位名稱，維持原始欄位順序。
        /// </summary>
        public List<string> Headers { get; set; } = new List<string>();

        /// <summary>
        /// 上傳檔案資料列。
        /// </summary>
        public List<ReconciliationLogisticsComparisonUploadRow> Rows { get; set; } =
            new List<ReconciliationLogisticsComparisonUploadRow>();
    }
}
