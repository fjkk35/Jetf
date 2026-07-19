namespace Service.Services.ReconciliationCustomerSelection.Domain
{
    /// <summary>
    /// 代收銷帳作業的可選客戶。
    /// </summary>
    public sealed class ReconciliationCustomerOption
    {
        /// <summary>
        /// 客戶類型 SEA 或 AIR。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶中文名稱。
        /// </summary>
        public string CustName { get; set; }
    }
}
