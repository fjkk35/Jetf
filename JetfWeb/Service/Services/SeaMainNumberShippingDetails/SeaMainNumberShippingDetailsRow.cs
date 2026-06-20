namespace Service.Services.SeaMainNumberShippingDetails
{
    /// <summary>
    /// 海運主號託運明細表(無稅金)資料列。
    /// </summary>
    public sealed class SeaMainNumberShippingDetailsRow
    {
        /// <summary>
        /// 主號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 訂單號。
        /// </summary>
        public string BlNo { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string DespatchName { get; set; }

        /// <summary>
        /// 收件人姓名。
        /// </summary>
        public string Importer { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        public string ImPhoneNo { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        public string ImAdd { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public double? Cc { get; set; }

        /// <summary>
        /// 毛重。
        /// </summary>
        public decimal? Gw { get; set; }

        /// <summary>
        /// 淨重。
        /// </summary>
        public decimal? Nw { get; set; }

        /// <summary>
        /// 商品數量。
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// 託運備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string JetfSerial { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }
    }
}
