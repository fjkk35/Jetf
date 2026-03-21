namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 退件原因批量上傳資料列模型
    /// </summary>
    public class ReturnReasonBatchUploadRowModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 退件原因
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// Excel 列號
        /// </summary>
        public int RowNo { get; set; }
    }
}
