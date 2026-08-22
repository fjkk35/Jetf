using System.Collections.Generic;

namespace Service.Services.ReconciliationInvoice.Domain
{
    /// <summary>
    /// 代收銷帳發票上傳結果。
    /// </summary>
    public sealed class ReconciliationInvoiceUploadResult
    {
        /// <summary>
        /// 建構上傳結果。
        /// </summary>
        public ReconciliationInvoiceUploadResult()
        {
            Data = new List<ReconciliationInvoiceUploadRow>();
        }

        /// <summary>
        /// 總筆數。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 失敗筆數。
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 新增筆數。
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// 更新筆數。
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 刪除筆數。
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// 回傳訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 失敗明細。
        /// </summary>
        public List<ReconciliationInvoiceUploadRow> Data { get; set; }
    }
}
