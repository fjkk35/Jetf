namespace Service.Services.Ftz.Domain
{
    /// <summary>
    /// FTZ 未收單錯單資料。
    /// </summary>
    public class FtzPlinkErrorRow
    {
        /// <summary>
        /// 分號。
        /// </summary>
        public string Hawb { get; set; }

        /// <summary>
        /// 錯單原因。
        /// </summary>
        public string Reason { get; set; }
    }
}
