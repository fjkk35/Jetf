using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 特殊客戶資料。
    /// </summary>
    [Table("customer_special", Schema = "dbo")]
    public sealed class CustomerSpecialEntity
    {
        /// <summary>
        /// 運輸類型。
        /// </summary>
        [Key]
        [Column("TRAN_TYPE", Order = 0)]
        public string TranType { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Key]
        [Column("CUST_NAME", Order = 1)]
        public string CustName { get; set; }

        /// <summary>
        /// 客戶第二名稱。
        /// </summary>
        [Column("CUST_NAME2")]
        public string CustName2 { get; set; }

        /// <summary>
        /// 電話。
        /// </summary>
        [Key]
        [Column("PHONE", Order = 2)]
        public string Phone { get; set; }

        /// <summary>
        /// 電子郵件。
        /// </summary>
        [Column("EMAIL")]
        public string Email { get; set; }

        /// <summary>
        /// 備註。
        /// </summary>
        [Column("REAMRK")]
        public string Remark { get; set; }
    }
}