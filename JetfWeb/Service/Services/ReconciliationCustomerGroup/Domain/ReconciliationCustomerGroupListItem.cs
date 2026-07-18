namespace Service.Services.ReconciliationCustomerGroup.Domain
{
    /// <summary>
    /// 代收銷帳客戶群組查詢結果。
    /// </summary>
    public sealed class ReconciliationCustomerGroupListItem
    {
        /// <summary>
        /// 客戶群組識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 運送類型代碼。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 運送類型名稱。
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 群組內客戶的顯示文字。
        /// </summary>
        public string CustomerDisplay { get; set; }
    }
}
