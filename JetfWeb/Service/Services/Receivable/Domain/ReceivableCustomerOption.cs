namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細的可選客戶。
    /// </summary>
    public sealed class ReceivableCustomerOption
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
