using Newtonsoft.Json;

namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富 API 回應模型
    /// </summary>
    public class JhfTaxResponseModel
    {
        /// <summary>
        /// 主提單號
        /// </summary>
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號
        /// </summary>
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅額
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// API 回傳資料
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// API 回傳代碼
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// API 回傳訊息
        /// </summary>
        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}