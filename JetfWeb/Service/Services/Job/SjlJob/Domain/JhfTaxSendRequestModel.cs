using Newtonsoft.Json;

namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富稅金傳送請求模型
    /// </summary>
    public class JhfTaxSendRequestModel : JhfTaxApiRequestModel
    {
        /// <summary>
        /// 存取權杖
        /// </summary>
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }
    }
}