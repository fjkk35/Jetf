namespace Service.Services.ShipmentInboundRecord.Domain
{
    /// <summary>
    /// 不明貨件可選的派件公司下拉資料。
    /// </summary>
    public class ShipmentInboundUnknownShipmentTransOptionModel
    {
        /// <summary>
        /// 前端下拉選單使用的唯一鍵值。
        /// </summary>
        public string OptionKey { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// 空運資料可能為空值。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 前端下拉選單顯示文字。
        /// </summary>
        public string DisplayText => string.IsNullOrWhiteSpace(TransNo)
            ? TransName
            : $"{TransName} ({TransNo})";
    }
}