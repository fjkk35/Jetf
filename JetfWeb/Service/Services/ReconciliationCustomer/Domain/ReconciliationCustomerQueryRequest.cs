using System.Collections.Generic;

namespace Service.Services.ReconciliationCustomer.Domain
{
    /// <summary>
    /// 客戶銷帳查詢條件。
    /// </summary>
    public sealed class ReconciliationCustomerQueryRequest
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
        /// 以換行分隔的物流貨號。
        /// </summary>
        public string DlvInvText { get; set; }
    }
}
