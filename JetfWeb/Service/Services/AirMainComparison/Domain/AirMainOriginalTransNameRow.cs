namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號派件公司查詢所需的 ORIGINALLIST 欄位。
    /// </summary>
    public class AirMainOriginalTransNameRow
    {
        /// <summary>
        /// 分提單號或袋號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        public int? TransNo { get; set; }
    }
}
