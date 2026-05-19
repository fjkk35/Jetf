namespace Service.Models.Tax
{
    /// <summary>
    /// 稅額計算所需的標準化輸入資料。
    /// </summary>
    public sealed class TaxCalculationInput
    {
        /// <summary>
        /// 第一段稅額。
        /// </summary>
        public int Tax1 { get; set; }

        /// <summary>
        /// 第二段稅額。
        /// </summary>
        public int Tax2 { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        public int Cod { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int Fee { get; set; }
    }
}