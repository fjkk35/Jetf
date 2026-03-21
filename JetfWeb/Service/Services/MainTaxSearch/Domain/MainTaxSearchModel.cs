namespace Service.Services.MainTaxSearch.Domain
{
    /// <summary>
    /// 主號稅金查詢結果
    /// </summary>
    public class MainTaxSearchModel
    {
        /// <summary>
        /// 客戶名稱
        /// </summary>
        public string CUST_NAME { get; set; }

        /// <summary>
        /// 清關業者名稱
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        public string MAIN_NUMBER { get; set; }

        /// <summary>
        /// 稅金合計（TAX1 + TAX2）
        /// </summary>
        public decimal TotalTax { get; set; }
    }
}
