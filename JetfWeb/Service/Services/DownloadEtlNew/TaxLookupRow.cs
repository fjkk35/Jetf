namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示稅單查詢後的中繼欄位。
    /// </summary>
    internal sealed class TaxLookupRow
    {
        /// <summary>
        /// 取得或設定主提單號。
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 取得或設定袋號。
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 取得或設定稅單號。
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人。
        /// </summary>
        public string TaxPayer { get; set; }

        /// <summary>
        /// 取得或設定納稅義務人證號。
        /// </summary>
        public string TaxRecId { get; set; }

        /// <summary>
        /// 取得或設定稅額字串。
        /// </summary>
        public string TaxAmount { get; set; }

        /// <summary>
        /// 取得或設定完稅價格。
        /// </summary>
        public int? TaxBase { get; set; }
    }
}