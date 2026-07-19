using System.Collections.Generic;

namespace Service.Services.ReconciliationCustomerSelection.Domain
{
    /// <summary>
    /// 代收銷帳作業的共用客戶選擇資料。
    /// </summary>
    public sealed class ReconciliationCustomerSelectionOptions
    {
        /// <summary>
        /// 海運客戶。
        /// </summary>
        public List<ReconciliationCustomerOption> SeaCustomers { get; set; }

        /// <summary>
        /// 空運客戶。
        /// </summary>
        public List<ReconciliationCustomerOption> AirCustomers { get; set; }

        /// <summary>
        /// 可套用的客戶群組。
        /// </summary>
        public List<ReconciliationCustomerGroupOption> Groups { get; set; }
    }
}
