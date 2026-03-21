using Newtonsoft.Json;

namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富 API 共用請求模型
    /// </summary>
    public class JhfTaxApiRequestModel
    {
        /// <summary>
        /// API 服務代碼
        /// </summary>
        [JsonProperty("sid")]
        public string Sid { get; set; }

        /// <summary>
        /// Base64 內容
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// 發送時間
        /// </summary>
        [JsonProperty("dateTime")]
        public string DateTime { get; set; }
    }
}