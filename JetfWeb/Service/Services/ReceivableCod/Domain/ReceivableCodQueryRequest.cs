using Service.EnumTax;
using System.Collections.Generic;

namespace Service.Services.ReceivableCod.Domain
{
    /// <summary>
    /// 到付款應收未收明細查詢條件。
    /// </summary>
    public sealed class ReceivableCodQueryRequest
    {
        /// <summary>
        /// 出倉開始日期，格式為 yyyy-MM-dd。
        /// </summary>
        public string SignOutDateStart { get; set; }

        /// <summary>
        /// 出倉結束日期，格式為 yyyy-MM-dd。
        /// </summary>
        public string SignOutDateEnd { get; set; }

        /// <summary>
        /// 選取的客戶代號。
        /// </summary>
        public List<string> CustomerCodes { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

        /// <summary>
        /// 收回狀態；未指定時查詢全部。
        /// </summary>
        public ReceivableStatus? Status { get; set; }

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
