namespace Service.Services.ReconciliationIncludeTaxDownload.Domain
{
    /// <summary>
    /// 包稅客戶明細下載使用的客戶群組資訊。
    /// </summary>
    public sealed class ReconciliationIncludeTaxDownloadCustomerGroup
    {
        /// <summary>
        /// 客戶群組識別碼。
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string GroupName { get; set; }
    }
}
