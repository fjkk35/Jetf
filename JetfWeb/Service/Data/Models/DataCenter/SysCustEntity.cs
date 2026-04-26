using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// DATA_CENTER 客戶主檔資料。
    /// </summary>
    [Table("SYS_CUST", Schema = "dbo")]
    public sealed class SysCustEntity
    {
        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Key]
        [Column("Cust_Code", Order = 0)]
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶類型。
        /// </summary>
        [Key]
        [Column("Cust_Type", Order = 1)]
        public string CustType { get; set; }

        /// <summary>
        /// 舊客戶代碼。
        /// </summary>
        [Column("OLD_CODE")]
        public string OldCode { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("Cust_Name")]
        public string CustName { get; set; }
    }
}
