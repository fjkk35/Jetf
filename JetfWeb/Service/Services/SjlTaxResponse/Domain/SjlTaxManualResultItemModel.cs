namespace Service.Services.SjlTaxResponse.Domain
{
    /// <summary>
    /// 單筆手動回傳捷利稅金的處理結果。
    /// </summary>
    public class SjlTaxManualResultItemModel
    {
        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DeliveryNumber { get; set; }

        /// <summary>
        /// 處理結果，例如成功、失敗或無稅單號。
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// 捷利 API 回覆代碼。
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 處理訊息。
        /// </summary>
        public string Message { get; set; }
    }
}
