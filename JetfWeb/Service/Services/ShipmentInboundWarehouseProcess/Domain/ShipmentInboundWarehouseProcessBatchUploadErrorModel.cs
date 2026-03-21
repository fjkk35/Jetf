namespace Service.Services.ShipmentInboundWarehouseProcess.Domain
{
    public class ShipmentInboundWarehouseProcessBatchUploadErrorModel
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 處理狀態(中文)
        /// </summary>
        public string WarehouseProcessTypeText { get; set; }

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
