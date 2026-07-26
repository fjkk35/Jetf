using System.Collections.Generic;

namespace Service.Services.ReceivableCod.Domain
{
    /// <summary>
    /// 到付款應收未收明細分頁查詢結果。
    /// </summary>
    public sealed class ReceivableCodQueryResponse
    {
        /// <summary>
        /// 符合查詢條件的總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當頁明細。
        /// </summary>
        public List<ReceivableCodListItem> Data { get; set; }
    }
}
