using System.Collections.Generic;

namespace Service.Services.TaxPortalCustomerService.Domain
{
    /// <summary>
    /// 稅金單查詢客戶選項。
    /// </summary>
    public class TaxPortalCustomerOptionModel
    {
        /// <summary>
        /// 客戶群組代碼。
        /// </summary>
        public string CustomerType { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        public string CustomerTypeName { get; set; }

        /// <summary>
        /// 客戶代號。
        /// </summary>
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string CustName { get; set; }
    }

    /// <summary>
    /// 稅金單查詢客戶分組資料。
    /// </summary>
    public class TaxPortalCustomerGroupModel
    {
        /// <summary>
        /// 海運客戶清單。
        /// </summary>
        public List<TaxPortalCustomerOptionModel> SeaCustomers { get; set; }

        /// <summary>
        /// 空運客戶清單。
        /// </summary>
        public List<TaxPortalCustomerOptionModel> AirCustomers { get; set; }

        public TaxPortalCustomerGroupModel()
        {
            SeaCustomers = new List<TaxPortalCustomerOptionModel>();
            AirCustomers = new List<TaxPortalCustomerOptionModel>();
        }
    }
}