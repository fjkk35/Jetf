namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 稅金單查詢帳號查詢條件。
    /// </summary>
    public class TaxPortalUserQueryRequest
    {
        /// <summary>
        /// 帳號。
        /// </summary>
        public string UserName { get; set; }
    }
}