namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富稅金作業執行結果
    /// </summary>
    public class JhfTaxExecutionResultModel
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// API 回傳代碼
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// API 回傳訊息
        /// </summary>
        public string Message { get; set; }
    }
}