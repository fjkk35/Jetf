using Service.EnumTax;
using System;

namespace Service.Services.ReconciliationLogistics.Domain
{
    /// <summary>
    /// 重新銷帳查無物流貨號資料的條件。
    /// </summary>
    public sealed class ReconciliationLogisticsRetryRequest
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
        /// 物流公司。
        /// </summary>
        public ReconciliationLogisticsCompany? Company { get; set; }
    }
}
