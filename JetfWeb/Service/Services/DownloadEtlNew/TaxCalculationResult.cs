namespace Service.Services.DownloadEtlNew
{
    /// <summary>
    /// 表示稅金計算後的代收拆分結果。
    /// </summary>
    internal sealed class TaxCalculationResult
    {
        /// <summary>
        /// 取得或設定物流代收金額。
        /// </summary>
        public int TransCod { get; set; }

        /// <summary>
        /// 取得或設定客戶代收金額。
        /// </summary>
        public int CustomerCod { get; set; }

        /// <summary>
        /// 取得或設定實際派件代收金額。
        /// </summary>
        public int ToDlvCod { get; set; }
    }
}