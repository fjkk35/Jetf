using System.Collections.Generic;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳分頁查詢結果。
    /// </summary>
    public sealed class ReconciliationLogisticsQueryResponse
    {
        /// <summary>
        /// 符合條件的總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當頁物流銷帳資料。
        /// </summary>
        public List<ReconciliationLogisticsListItem> Data { get; set; }
    }
}
