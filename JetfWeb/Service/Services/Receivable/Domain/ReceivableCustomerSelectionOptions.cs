using System.Collections.Generic;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細的客戶選擇彈窗資料。
    /// </summary>
    public sealed class ReceivableCustomerSelectionOptions
    {
        /// <summary>
        /// 海運客戶。
        /// </summary>
        public List<ReceivableCustomerOption> SeaCustomers { get; set; }

        /// <summary>
        /// 空運客戶。
        /// </summary>
        public List<ReceivableCustomerOption> AirCustomers { get; set; }

        /// <summary>
        /// 可套用的客戶群組。
        /// </summary>
        public List<ReceivableCustomerGroupOption> Groups { get; set; }
    }
}
