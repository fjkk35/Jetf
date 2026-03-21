using System.Collections.Generic;

namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 新增稅金單查詢帳號請求。
    /// </summary>
    public class TaxPortalUserCreateRequest
    {
        /// <summary>
        /// 帳號。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 選擇的客戶代號清單。
        /// </summary>
        public List<string> SelectedCustCodes { get; set; }
    }

    /// <summary>
    /// 修改稅金單查詢帳號請求。
    /// </summary>
    public class TaxPortalUserUpdateRequest
    {
        /// <summary>
        /// 帳號流水號。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 選擇的客戶代號清單。
        /// </summary>
        public List<string> SelectedCustCodes { get; set; }

        /// <summary>
        /// 新的明文密碼。
        /// </summary>
        public string NewPassword { get; set; }
    }
}