using System.Collections.Generic;

namespace Service.Services.SjlTaxResponse.Domain
{
    /// <summary>
    /// 手動回傳捷利稅金的批次處理結果。
    /// </summary>
    public class SjlTaxManualResultModel
    {
        /// <summary>
        /// 去除空白與重複後的輸入筆數。
        /// </summary>
        public int RequestedCount { get; set; }

        /// <summary>
        /// 查得的 FEE_MASTER 資料筆數。
        /// </summary>
        public int MatchedCount { get; set; }

        /// <summary>
        /// API 回覆成功筆數。
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 沒有稅單號碼而未呼叫 API 的筆數。
        /// </summary>
        public int NoTaxCount { get; set; }

        /// <summary>
        /// API 回覆失敗筆數。
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 每筆物流貨號的處理結果。
        /// </summary>
        public List<SjlTaxManualResultItemModel> Items { get; set; }

        /// <summary>
        /// 建立批次處理結果。
        /// </summary>
        public SjlTaxManualResultModel()
        {
            Items = new List<SjlTaxManualResultItemModel>();
        }
    }
}
