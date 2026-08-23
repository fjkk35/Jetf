namespace Service.Services.ShipmentInboundWarehouseCodImport.Domain
{
    /// <summary>
    /// 倉庫代收上傳 Excel 的單筆資料。
    /// </summary>
    public sealed class ShipmentInboundWarehouseCodImportRow
    {
        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }

        /// <summary>
        /// Excel 原始託運單號。
        /// </summary>
        public string ShipmentNo { get; set; }

        /// <summary>
        /// 寫入 FEE_MASTER_COD 的 TRACKINGNO。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 寫入 FEE_MASTER_COD 的 DLV_INV。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// Excel 訂單編號。
        /// </summary>
        public string OrderNo { get; set; }

        /// <summary>
        /// Excel 客戶。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// Excel 類別。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Excel 廠商對應單號。
        /// </summary>
        public string VendorOrderNo { get; set; }

        /// <summary>
        /// Excel 代收款。
        /// </summary>
        public decimal? Cc { get; set; }

        /// <summary>
        /// Excel 代收款原始文字。
        /// </summary>
        public string CcText { get; set; }

        /// <summary>
        /// 資料驗證失敗原因。
        /// </summary>
        public string FailReason { get; set; }
    }
}
