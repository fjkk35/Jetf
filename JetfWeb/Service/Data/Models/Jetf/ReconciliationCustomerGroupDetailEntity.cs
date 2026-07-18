using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 代收銷帳客戶群組明細資料。
    /// </summary>
    [Table("ReconciliationCustomerGroupDetail", Schema = "dbo")]
    public sealed class ReconciliationCustomerGroupDetailEntity
    {
        /// <summary>
        /// 客戶群組明細識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 客戶群組識別碼。
        /// </summary>
        [ForeignKey(nameof(CustomerGroup))]
        [Column("CustomerGroupId")]
        public int CustomerGroupId { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("CustCode")]
        public string CustCode { get; set; }

        /// <summary>
        /// 所屬客戶群組。
        /// </summary>
        public ReconciliationCustomerGroupEntity CustomerGroup { get; set; }
    }
}
