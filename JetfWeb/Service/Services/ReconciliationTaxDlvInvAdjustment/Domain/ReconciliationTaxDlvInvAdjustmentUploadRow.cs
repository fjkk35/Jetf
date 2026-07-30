namespace Service.Services.ReconciliationTaxDlvInvAdjustment.Domain
{
    /// <summary>
    /// 稅金物流貨號調整上傳列資料。
    /// </summary>
    public sealed class ReconciliationTaxDlvInvAdjustmentUploadRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 舊物流貨號。
        /// </summary>
        public string OldDlvInv { get; set; }

        /// <summary>
        /// 新物流貨號。
        /// </summary>
        public string NewDlvInv { get; set; }

        /// <summary>
        /// 是否更新成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 處理結果。
        /// </summary>
        public string Status { get; set; }
    }
}
