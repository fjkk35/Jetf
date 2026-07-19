namespace Service.Services.ReconciliationCustomer.Domain
{
    /// <summary>
    /// 客戶銷帳確認資料。
    /// </summary>
    public sealed class ReconciliationCustomerConfirmRequest
    {
        /// <summary>
        /// 目前畫面的查詢條件。
        /// </summary>
        public ReconciliationCustomerQueryRequest Query { get; set; }

        /// <summary>
        /// 使用者輸入的銷帳金額。
        /// </summary>
        public long Amount { get; set; }
    }
}
