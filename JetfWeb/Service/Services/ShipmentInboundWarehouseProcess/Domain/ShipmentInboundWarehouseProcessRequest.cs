namespace Service.Services.ShipmentInboundWarehouseProcess.Domain
{
    /// <summary>
    /// 倉庫處理狀態查詢請求
    /// </summary>
    public class ShipmentInboundWarehouseProcessRequest
    {
        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }
    }
}
