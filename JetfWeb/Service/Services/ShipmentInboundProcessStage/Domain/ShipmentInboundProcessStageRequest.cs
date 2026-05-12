namespace Service.Services.ShipmentInboundProcessStage.Domain
{
    /// <summary>
    /// 預先登記處理查詢條件。
    /// </summary>
    public class ShipmentInboundProcessStageRequest
    {
        /// <summary>
        /// 單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 目前頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}
