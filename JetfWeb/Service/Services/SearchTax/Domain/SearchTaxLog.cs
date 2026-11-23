namespace Service.Services.SearchTax.Domain
{
    /// <summary>
    /// 貨況查詢紀錄。
    /// </summary>
    public sealed class SearchTaxLog
    {
        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 查詢時間。
        /// </summary>
        public string SearchTime { get; set; }

        /// <summary>
        /// 查詢人員。
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 查詢 IP。
        /// </summary>
        public string UserIp { get; set; }
    }
}
