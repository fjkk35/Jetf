using System.Collections.Generic;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細分頁查詢結果。
    /// </summary>
    public sealed class ReceivableQueryResponse
    {
        /// <summary>
        /// 符合條件的總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 當頁明細。
        /// </summary>
        public List<ReceivableListItem> Data { get; set; }
    }
}
