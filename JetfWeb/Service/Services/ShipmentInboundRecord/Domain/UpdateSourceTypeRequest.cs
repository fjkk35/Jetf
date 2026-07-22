namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 更新貨件來源請求。
    /// </summary>
    public class UpdateSourceTypeRequest
    {
        /// <summary>
        /// 貨件入庫 Id。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 貨件來源代碼。
        /// </summary>
        public byte? SourceType { get; set; }
    }
}
