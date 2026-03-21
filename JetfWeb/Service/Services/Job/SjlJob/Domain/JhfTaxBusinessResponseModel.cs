using Newtonsoft.Json;

namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富業務回傳模型
    /// </summary>
    public class JhfTaxBusinessResponseModel
    {
        /// <summary>
        /// 業務訊息
        /// </summary>
        [JsonProperty("msg")]
        public string Msg { get; set; }

        /// <summary>
        /// 業務狀態碼
        /// </summary>
        [JsonProperty("code")]
        public int? Code { get; set; }
    }
}