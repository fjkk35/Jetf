namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 更新貨件單號請求
    /// </summary>
    public class UpdateTrackingNoRequest
    {
        /// <summary>
        /// 貨件 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 新單號
        /// </summary>
        public string NewTrackingNo { get; set; }
    }
}