namespace Service.Services.SeaShenzhenOriginal.Domain
{
    /// <summary>
    /// 新遞物流代收檔下載及到付款轉入使用的來源資料。
    /// </summary>
    public sealed class SeaShenzhenFeeDownloadRow
    {
        /// <summary>深圳費用資料識別碼。</summary>
        public int Id { get; set; }

        /// <summary>客戶代碼。</summary>
        public string Customer { get; set; }

        /// <summary>清關袋號。</summary>
        public string TrackingNo { get; set; }

        /// <summary>物流貨號。</summary>
        public string DlvInv { get; set; }

        /// <summary>到付款金額。</summary>
        public int Cod { get; set; }

        /// <summary>手續費。</summary>
        public int Fee { get; set; }

        /// <summary>下載檔中應向物流代收的總金額。</summary>
        public int ToDlvCod { get; set; }

        /// <summary>收件人。</summary>
        public string Recipient { get; set; }

        /// <summary>收件人電話。</summary>
        public string RecPhone { get; set; }

        /// <summary>派件公司。</summary>
        public string DlvCom { get; set; }

        /// <summary>稅金支付方式。</summary>
        public string IncludeTax { get; set; }
    }
}
