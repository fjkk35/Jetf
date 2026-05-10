using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 客戶與派件公司主檔資料。
    /// </summary>
    [Table("customer_master", Schema = "dbo")]
    public sealed class CustomerMasterEntity
    {
        /// <summary>
        /// 主鍵 Id。
        /// </summary>
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 運輸類型，例如海運或空運。
        /// </summary>
        [Column("TRAN_TYPE")]
        public string TranType { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("CUST_ID")]
        public string CustId { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("CUSTOMER")]
        public string Customer { get; set; }

        /// <summary>
        /// 派件公司代碼。
        /// </summary>
        [Column("TRANS_NO")]
        public string TransNo { get; set; }

        /// <summary>
        /// 派件公司名稱。
        /// </summary>
        [Column("TRANS_NAME")]
        public string TransName { get; set; }

        /// <summary>
        /// 派件公司群組。
        /// </summary>
        [Column("TRANS_GROUP")]
        public string TransGroup { get; set; }

        /// <summary>
        /// 是否包稅代碼。
        /// </summary>
        [Column("INCLUDE_TAX")]
        public string IncludeTax { get; set; }

        /// <summary>
        /// 是否包稅名稱。
        /// </summary>
        [Column("INCLUDE_TAX_NAME")]
        public string IncludeTaxName { get; set; }

        /// <summary>
        /// 廠商代碼。
        /// </summary>
        [Column("COMPANY_NO")]
        public string CompanyNo { get; set; }

        /// <summary>
        /// 廠商名稱。
        /// </summary>
        [Column("COMPANY")]
        public string Company { get; set; }

        /// <summary>
        /// 到付款手續費。
        /// </summary>
        [Column("COD_FEE")]
        public int? CodFee { get; set; }

        /// <summary>
        /// 是否為菜鳥尊榮服務。
        /// </summary>
        [Column("ISCAINIAOP")]
        public bool? IsCainiaoP { get; set; }

        /// <summary>
        /// 最後更新時間。
        /// </summary>
        [Column("UPDATE_TIME")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 最後更新人員。
        /// </summary>
        [Column("UPDATE_OPE")]
        public string UpdateOpe { get; set; }
    }
}
