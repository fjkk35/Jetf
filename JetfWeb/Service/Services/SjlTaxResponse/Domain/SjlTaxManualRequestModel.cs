namespace Service.Services.SjlTaxResponse.Domain
{
    /// <summary>
    /// 手動回傳捷利稅金的查詢條件。
    /// </summary>
    public class SjlTaxManualRequestModel
    {
        /// <summary>
        /// 稅金類型，支援海運或空運。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 以換行分隔的物流貨號。
        /// </summary>
        public string DeliveryNumbers { get; set; }
    }
}
