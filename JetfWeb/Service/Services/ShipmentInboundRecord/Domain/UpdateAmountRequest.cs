namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 更新金額請求
    /// </summary>
    public class UpdateAmountRequest
    {
        /// <summary>
        /// 貨件入庫 Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public int NewValue { get; set; }
    }
}
