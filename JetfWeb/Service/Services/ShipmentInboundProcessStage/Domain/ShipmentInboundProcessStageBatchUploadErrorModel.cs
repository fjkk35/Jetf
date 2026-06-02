namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記退件原因批次上傳錯誤資料。
    /// </summary>
    public class ShipmentInboundProcessStageBatchUploadErrorModel
    {
        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 錯誤原因。
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Excel 列號。
        /// </summary>
        public int RowNo { get; set; }
    }
}