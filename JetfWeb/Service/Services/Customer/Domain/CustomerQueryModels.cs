using System.Collections.Generic;

namespace Service.Services.Customer.Domain
{
    /// <summary>
    /// 客戶查詢條件。
    /// </summary>
    public class CustomerQueryRequest
    {
        /// <summary>
        /// 運送類型。
        /// </summary>
        public string TranType { get; set; }

        /// <summary>
        /// 客戶代碼清單。
        /// </summary>
        public List<string> CustCodes { get; set; }

        /// <summary>
        /// 派件公司關鍵字。
        /// </summary>
        public string TransKeyword { get; set; }

        /// <summary>
        /// 是否包稅。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 物流公司代碼。
        /// </summary>
        public string CompanyNo { get; set; }

        /// <summary>
        /// 菜鳥尊榮服務。
        /// </summary>
        public bool? IsCainiaoP { get; set; }

        /// <summary>
        /// 頁碼。
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// 每頁筆數。
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// 客戶查詢結果。
    /// </summary>
    public class CustomerQueryResult
    {
        /// <summary>
        /// 總筆數。
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 查詢資料。
        /// </summary>
        public List<CustomerListItem> Data { get; set; }
    }

    /// <summary>
    /// 客戶清單資料。
    /// </summary>
    public class CustomerListItem
    {
        /// <summary>
        /// 主鍵編號。
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 運送類型。
        /// </summary>
        public string TranType { get; set; }

        /// <summary>
        /// 客戶編號。
        /// </summary>
        public string CustId { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 派件公司編號。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 是否包稅代碼。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 是否包稅文字。
        /// </summary>
        public string IncludeTaxName { get; set; }

        /// <summary>
        /// 物流公司代碼。
        /// </summary>
        public string CompanyNo { get; set; }

        /// <summary>
        /// 物流公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public int? CodFee { get; set; }

        /// <summary>
        /// 是否為菜鳥尊榮服務。
        /// </summary>
        public bool IsCainiaoP { get; set; }

        /// <summary>
        /// 菜鳥尊榮服務文字。
        /// </summary>
        public string IsCainiaoPText => IsCainiaoP ? "是" : "否";
    }

    /// <summary>
    /// 客戶新增與修改資料。
    /// </summary>
    public class CustomerUpsertModel
    {
        /// <summary>
        /// 主鍵編號。
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// 運送類型。
        /// </summary>
        public string TranType { get; set; }

        /// <summary>
        /// 客戶編號。
        /// </summary>
        public string CustId { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        public string Customer { get; set; }

        /// <summary>
        /// 派件公司編號。
        /// </summary>
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司。
        /// </summary>
        public string TransName { get; set; }

        /// <summary>
        /// 是否包稅代碼。
        /// </summary>
        public string IncludeTax { get; set; }

        /// <summary>
        /// 是否包稅文字。
        /// </summary>
        public string IncludeTaxName { get; set; }

        /// <summary>
        /// 物流公司代碼。
        /// </summary>
        public string CompanyNo { get; set; }

        /// <summary>
        /// 物流公司名稱。
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 手續費。
        /// </summary>
        public string CodFee { get; set; }

        /// <summary>
        /// 是否為菜鳥尊榮服務。
        /// </summary>
        public bool IsCainiaoP { get; set; }
    }

    /// <summary>
    /// 下拉選單項目。
    /// </summary>
    public class CustomerPageOption
    {
        /// <summary>
        /// 值。
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 顯示文字。
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// 客戶頁面表單選項。
    /// </summary>
    public class CustomerFormOptions
    {
        /// <summary>
        /// 運送類型選項。
        /// </summary>
        public List<CustomerPageOption> TranTypes { get; set; }

        /// <summary>
        /// 是否包稅選項。
        /// </summary>
        public List<CustomerPageOption> IncludeTaxes { get; set; }

        /// <summary>
        /// 物流公司選項。
        /// </summary>
        public List<CustomerPageOption> Companies { get; set; }
    }
}