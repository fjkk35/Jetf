namespace Service.Services.ReconciliationCustomerGroup.Domain
{
    /// <summary>
    /// 代收銷帳客戶群組查詢條件。
    /// </summary>
    public sealed class ReconciliationCustomerGroupQueryRequest
    {
        /// <summary>
        /// 運送類型代碼。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string GroupName { get; set; }
    }
}
