namespace Service.Services.ReconciliationCustomer.Domain
{
    /// <summary>
    /// 客戶銷帳查詢結果。
    /// </summary>
    public sealed class ReconciliationCustomerQueryResult
    {
        /// <summary>
        /// 符合條件的應收金額合計。
        /// </summary>
        public long ReceivableAmount { get; set; }
    }
}
