using System.Collections.Generic;

namespace Service.Services.ReconciliationCustomerGroup.Domain
{
    /// <summary>
    /// 代收銷帳客戶群組新增或修改資料。
    /// </summary>
    public sealed class ReconciliationCustomerGroupSaveRequest
    {
        /// <summary>
        /// 客戶群組識別碼；新增時為空值。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 運送類型代碼。
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 選取的客戶代碼。
        /// </summary>
        public List<string> CustCodes { get; set; }
    }
}
