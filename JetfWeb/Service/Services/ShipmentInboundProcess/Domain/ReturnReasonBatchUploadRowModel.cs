namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 批量上傳退件原因資料列。
    /// </summary>
    public class ReturnReasonBatchUploadRowModel
    {
        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 退件原因。
        /// </summary>
        public string ReturnReason { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }
    }
}
