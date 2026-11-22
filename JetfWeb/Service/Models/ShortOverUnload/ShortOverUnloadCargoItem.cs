namespace Service.Models.ShortOverUnload
{
    /// <summary>
    /// 溢卸裝機明細表列出的單一製單品項。
    /// </summary>
    public class ShortOverUnloadCargoItem
    {
        /// <summary>貨物名稱。</summary>
        public string ItemName { get; set; }

        /// <summary>數量。</summary>
        public int? Quantity { get; set; }

        /// <summary>單價金額。</summary>
        public decimal? UnitPrice { get; set; }
    }
}
