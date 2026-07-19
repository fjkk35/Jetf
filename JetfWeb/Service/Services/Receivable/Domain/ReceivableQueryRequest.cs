using Service.EnumTax;
using System.Collections.Generic;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細查詢條件。
    /// </summary>
    public sealed class ReceivableQueryRequest
    {
        /// <summary>
        /// 出倉日期起日，格式為 yyyy-MM-dd。
        /// </summary>
        public string OutDateStart { get; set; }

        /// <summary>
        /// 出倉日期迄日，格式為 yyyy-MM-dd。
        /// </summary>
        public string OutDateEnd { get; set; }

        /// <summary>
        /// 選取的客戶代號。
        /// </summary>
        public List<string> CustomerCodes { get; set; }

        /// <summary>
        /// 回收狀態；未指定代表全部。
        /// </summary>
        public ReceivableStatus? Status { get; set; }

        /// <summary>
        /// 收取對象；未指定代表全部。
        /// </summary>
        public ReceivableCollectionType? CollectionType { get; set; }

        /// <summary>
        /// 頁碼，從 1 開始。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }
}
