using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 可用客戶主檔。
    /// </summary>
    [Table("SeaClearanceCustomer", Schema = "dbo")]
    public sealed class SeaClearanceCustomerEntity
    {
        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Key]
        [Column("Cust_Code")]
        public string CustCode { get; set; }

        /// <summary>
        /// 客戶名稱。
        /// </summary>
        [Column("Cust_Name")]
        public string CustName { get; set; }
    }
}