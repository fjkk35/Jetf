namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 稅金單查詢帳號列表。
    /// </summary>
    public class TaxPortalUserSummaryModel
    {
        /// <summary>
        /// 帳號流水號。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 帳號。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 客戶摘要。
        /// </summary>
        public string CustomerSummary { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 客戶數量。
        /// </summary>
        public int CustomerCount { get; set; }
    }
}