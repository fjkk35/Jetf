namespace Service.Services.DeliveryAssistant.Domain
{
    /// <summary>
    /// 派送助理上傳整體結果
    /// </summary>
    public class DeliveryAssistantUploadResult
    {
        /// <summary>
        /// 託運資料 API 結果
        /// </summary>
        public DeliveryAssistantApiResult UploadOrderInfo { get; set; }

        /// <summary>
        /// 車次 API 結果
        /// </summary>
        public DeliveryAssistantApiResult EstablishDcShip { get; set; }
    }

    /// <summary>
    /// 單一 API 顯示結果
    /// </summary>
    public class DeliveryAssistantApiResult
    {
        /// <summary>
        /// 顯示標題
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// 結果代碼
        /// </summary>
        public string resultCode { get; set; }

        /// <summary>
        /// 顯示訊息
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 單筆資料
        /// </summary>
        public object row { get; set; }

        /// <summary>
        /// 明細資料
        /// </summary>
        public object rows { get; set; }
    }
}
