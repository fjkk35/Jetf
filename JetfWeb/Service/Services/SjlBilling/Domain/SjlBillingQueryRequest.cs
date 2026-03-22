namespace Service.Services.SjlBilling.Domain
{
    /// <summary>
    /// 捷利帳單查詢條件。
    /// </summary>
    public class SjlBillingQueryRequest
    {
        /// <summary>
        /// 查詢開始日期。
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// 查詢結束日期。
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }
    }
}