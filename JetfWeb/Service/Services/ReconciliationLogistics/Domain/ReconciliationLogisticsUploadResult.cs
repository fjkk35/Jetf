using System.Collections.Generic;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳上傳結果。
    /// </summary>
    public sealed class ReconciliationLogisticsUploadResult
    {
        /// <summary>
        /// 建立物流銷帳上傳結果。
        /// </summary>
        public ReconciliationLogisticsUploadResult()
        {
            Data = new List<ReconciliationLogisticsUploadRow>();
            Results = new List<ReconciliationLogisticsResultItem>();
        }

        /// <summary>
        /// 上傳資料總筆數。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 驗證失敗筆數。
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 成功比對並更新費用明細或到付款資料的上傳筆數。
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 未比對到費用明細及到付款資料的上傳筆數。
        /// </summary>
        public int UnmatchedCount { get; set; }

        /// <summary>
        /// 已更新費用資料但仍需追蹤的異常筆數。
        /// </summary>
        public int ExceptionCount { get; set; }

        /// <summary>
        /// 實際更新的費用明細及到付款資料筆數。
        /// </summary>
        public int UpdatedDetailCount { get; set; }

        /// <summary>
        /// 執行結果訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 驗證失敗明細。
        /// </summary>
        public List<ReconciliationLogisticsUploadRow> Data { get; set; }

        /// <summary>
        /// 上傳完成後的物流銷帳結果。
        /// </summary>
        public List<ReconciliationLogisticsResultItem> Results { get; set; }

        /// <summary>
        /// Excel 暫存下載識別碼。
        /// </summary>
        public string FileGuid { get; set; }

        /// <summary>
        /// Excel 下載檔名。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Excel 產生失敗時的提醒訊息。
        /// </summary>
        public string ExcelErrorMessage { get; set; }
    }
}
