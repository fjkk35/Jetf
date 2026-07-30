using System.Collections.Generic;

namespace Service.Services.ReconciliationTaxDlvInvAdjustment.Domain
{
    /// <summary>
    /// 稅金物流貨號調整上傳結果。
    /// </summary>
    public sealed class ReconciliationTaxDlvInvAdjustmentUploadResult
    {
        /// <summary>
        /// 建立上傳結果。
        /// </summary>
        public ReconciliationTaxDlvInvAdjustmentUploadResult()
        {
            Data = new List<ReconciliationTaxDlvInvAdjustmentUploadRow>();
        }

        /// <summary>
        /// 上傳結果訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 每列處理結果。
        /// </summary>
        public List<ReconciliationTaxDlvInvAdjustmentUploadRow> Data { get; set; }
    }
}
