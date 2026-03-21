namespace Service.Services.ShipmentInboundProcess.Domain
{
    /// <summary>
    /// 退件原因批量上傳錯誤模型
    /// </summary>
    public class ReturnReasonBatchUploadErrorModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 失敗原因描述
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Excel 列號
        /// </summary>
        public int RowNo { get; set; }
    }
}
