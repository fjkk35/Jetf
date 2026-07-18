namespace Service.Services.ReconciliationCustomerGroup.Domain
{
    /// <summary>
    /// 代收銷帳客戶勾選選項。
    /// </summary>
    public sealed class ReconciliationCustomerOption
    {
        /// <summary>
        /// 客戶代碼。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }

        /// <summary>
        /// 是否已選入目前編輯的群組。
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// 是否已被其他群組使用而不可選取。
        /// </summary>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// 已加入的群組名稱。
        /// </summary>
        public string AssignedGroupName { get; set; }
    }
}
