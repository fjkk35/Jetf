namespace Service.Services.AirMainComparison.Domain
{
    /// <summary>
    /// 空運主號未收單資料對應的 PLINK 錯單。
    /// </summary>
    public class AirMainPlinkErrorRow
    {
        /// <summary>
        /// 錯單單號。
        /// </summary>
        public string Hawb { get; set; }

        /// <summary>
        /// 錯單原因。
        /// </summary>
        public string Reason { get; set; }
    }
}
