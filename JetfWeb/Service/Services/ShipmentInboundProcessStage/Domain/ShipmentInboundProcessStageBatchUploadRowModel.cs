namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記退件原因批次上傳資料列。
    /// </summary>
    public class ShipmentInboundProcessStageBatchUploadRowModel
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