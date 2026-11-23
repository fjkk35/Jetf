namespace Service.Services.SearchTax.Domain
{
    /// <summary>
    /// 稅金查詢明細的配送進度。
    /// </summary>
    public sealed class SearchTaxCargoStatus
    {
        /// <summary>
        /// 作業時間（yyyy-MM-dd HH:mm:ss）。
        /// </summary>
        public string TransModifyTimeText { get; set; }

        /// <summary>
        /// 配送狀態。
        /// </summary>
        public string TransStatusDesc { get; set; }
    }
}
