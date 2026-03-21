using Newtonsoft.Json;
using System.Collections.Generic;

namespace Service.Services.Job.SjlJob.Domain
{
    /// <summary>
    /// 金祥富稅金傳送內容
    /// </summary>
    public class JhfTaxPayloadModel
    {
        /// <summary>
        /// 稅金清單
        /// </summary>
        [JsonProperty("taxList")]
        public List<JhfTaxPayloadItemModel> TaxList { get; set; }
    }

    /// <summary>
    /// 金祥富稅金傳送明細
    /// </summary>
    public class JhfTaxPayloadItemModel
    {
        /// <summary>
        /// 提袋號碼
        /// </summary>
        [JsonProperty("bagNo")]
        public string BagNo { get; set; }

        /// <summary>
        /// 稅單號碼
        /// </summary>
        [JsonProperty("taxNo")]
        public string TaxNo { get; set; }

        /// <summary>
        /// 稅額
        /// </summary>
        [JsonProperty("tax")]
        public decimal Tax { get; set; }
    }
}