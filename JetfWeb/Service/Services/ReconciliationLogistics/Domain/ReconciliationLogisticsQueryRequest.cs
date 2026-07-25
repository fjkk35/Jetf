using Service.EnumTax;
using System;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 物流銷帳查詢條件。
    /// </summary>
    public sealed class ReconciliationLogisticsQueryRequest
    {
        /// <summary>
        /// 回款日期起日。
        /// </summary>
        public DateTime? RepaymentDateStart { get; set; }

        /// <summary>
        /// 回款日期迄日。
        /// </summary>
        public DateTime? RepaymentDateEnd { get; set; }

        /// <summary>
        /// 物流公司；未指定代表全部。
        /// </summary>
        public ReconciliationLogisticsCompany? Company { get; set; }

        /// <summary>
        /// 物流銷帳比對狀態；未指定代表全部。
        /// </summary>
        public ReconciliationLogisticsResultStatus? Status { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        public string DlvInv { get; set; }

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
