using System.Collections.Generic;

namespace Service.Services.DeliveryAssistant.Domain
{
    public class UploadOrderInfoResponse
    {
        /// <summary>
        /// 結果代碼，01:錯誤 10:正確
        /// </summary>
        public string resultCode { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string error { get; set; }

        /// <summary>
        /// 託運單號 List
        /// </summary>
        public List<UploadOrderInfoRow> rows { get; set; }
    }
}
