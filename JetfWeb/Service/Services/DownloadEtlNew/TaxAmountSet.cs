namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示稅金計算前的金額集合。
    /// </summary>
    internal sealed class TaxAmountSet
    {
        /// <summary>
        /// 取得或設定主筆稅額。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 取得或設定累加稅額。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 取得或設定代收金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 取得或設定手續費。
        /// </summary>
        public int Fee { get; set; }
    }
}