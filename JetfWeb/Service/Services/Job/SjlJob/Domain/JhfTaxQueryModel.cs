namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富稅金查詢結果
    /// </summary>
    public class JhfTaxQueryModel
    {
        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅額
        /// </summary>
        public decimal TaxAmount { get; set; }
    }
}