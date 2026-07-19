using System;

namespace Service.Services.ReconciliationCustomer.Domain
{
    /// <summary>
    /// 客戶銷帳執行結果。
    /// </summary>
    public sealed class ReconciliationCustomerConfirmResult
    {
        /// <summary>
        /// 已更新的明細筆數。
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// 本次銷帳金額。
        /// </summary>
        public long ReceivedAmount { get; set; }

        /// <summary>
        /// 本次銷帳時間。
        /// </summary>
        public DateTime ReceivedTime { get; set; }
    }
}
