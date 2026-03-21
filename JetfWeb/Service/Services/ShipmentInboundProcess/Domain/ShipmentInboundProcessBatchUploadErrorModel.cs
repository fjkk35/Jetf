namespace Service.Services.ShipmentInboundProcess.Domain
{
    public class ShipmentInboundProcessBatchUploadErrorModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 處理方式(中文)
        /// </summary>
        public string ProcessTypeText { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 失敗原因說明
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Excel 列號
        /// </summary>
        public int RowNo { get; set; }
    }
}
