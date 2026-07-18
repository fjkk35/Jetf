using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 代收銷帳客戶群組資料。
    /// </summary>
    [Table("ReconciliationCustomerGroup", Schema = "dbo")]
    public sealed class ReconciliationCustomerGroupEntity
    {
        /// <summary>
        /// 客戶群組識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 運送類型代碼。
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("Type")]
        public string Type { get; set; }

        /// <summary>
        /// 客戶群組名稱。
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("GroupName")]
        public string GroupName { get; set; }

        /// <summary>
        /// 客戶群組明細。
        /// </summary>
        [InverseProperty(nameof(ReconciliationCustomerGroupDetailEntity.CustomerGroup))]
        public ICollection<ReconciliationCustomerGroupDetailEntity> Details { get; set; }
    }
}
