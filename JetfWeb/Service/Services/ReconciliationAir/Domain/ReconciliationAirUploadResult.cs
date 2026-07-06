using System.Collections.Generic;

namespace Service.Services.ReconciliationAir.Domain
{
    /// <summary>
    /// 空快代收銷帳上傳結果。
    /// </summary>
    public sealed class ReconciliationAirUploadResult
    {
        /// <summary>
        /// 建構上傳結果。
        /// </summary>
        public ReconciliationAirUploadResult()
        {
            Data = new List<ReconciliationAirUploadRow>();
        }

        /// <summary>
        /// 總筆數。
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 失敗筆數。
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 新增筆數。
        /// </summary>
        public int CreatedCount { get; set; }

        /// <summary>
        /// 更新筆數。
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 回傳訊息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 失敗明細。
        /// </summary>
        public List<ReconciliationAirUploadRow> Data { get; set; }
    }
}
