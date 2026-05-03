namespace Service.Services.ShipmentInboundLocationTransfer.Domain
{
    /// <summary>
    /// 儲位調撥查詢請求模型
    /// </summary>
    public class LocationTransferRequest
    {
        /// <summary>
        /// 儲位
        /// </summary>
        public string LocationCode { get; set; }

        /// <summary>
        /// 單號
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 流水號
        /// </summary>
        public string SeqNo { get; set; }
    }
}
