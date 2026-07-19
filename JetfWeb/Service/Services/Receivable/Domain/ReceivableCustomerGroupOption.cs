using System.Collections.Generic;

namespace Service.Services.Receivable.Domain
{
    /// <summary>
    /// 應收未收明細的客戶群組選項。
    /// </summary>
    public sealed class ReceivableCustomerGroupOption
    {
        /// <summary>
        /// 群組識別碼。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 客戶類型 SEA 或 AIR。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 群組名稱。
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 群組內的客戶代號。
        /// </summary>
        public List<string> CustCodes { get; set; }
    }
}
