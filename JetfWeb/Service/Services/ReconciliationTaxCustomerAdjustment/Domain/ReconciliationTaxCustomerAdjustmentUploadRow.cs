namespace Service.Services.ReconciliationTaxCustomerAdjustment.Domain
{
    /// <summary>
    /// 稅金客戶調整 Excel 上傳資料列。
    /// </summary>
    public sealed class ReconciliationTaxCustomerAdjustmentUploadRow
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
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 調整後的客戶代號。
        /// </summary>
        public string NewCustomerCode { get; set; }

        /// <summary>
        /// 是否更新成功。
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 處理狀態或錯誤原因。
        /// </summary>
        public string Status { get; set; }
    }
}
