namespace Service.Services.ReconciliationCustomerGroup.Domain
{
    /// <summary>
    /// 代收銷帳客戶群組下拉選項。
    /// </summary>
    public sealed class ReconciliationCustomerGroupOption
    {
        /// <summary>
        /// 客戶群組識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string GroupName { get; set; }
    }
}
