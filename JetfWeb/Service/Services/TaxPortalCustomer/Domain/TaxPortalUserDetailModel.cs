using System.Collections.Generic;

namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 稅金單查詢帳號明細。
    /// </summary>
    public class TaxPortalUserDetailModel
    {
        /// <summary>
        /// 帳號流水號。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 帳號。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 已選擇客戶。
        /// </summary>
        public List<TaxPortalCustomerOptionModel> SelectedCustomers { get; set; }

        public TaxPortalUserDetailModel()
        {
            SelectedCustomers = new List<TaxPortalCustomerOptionModel>();
        }
    }
}