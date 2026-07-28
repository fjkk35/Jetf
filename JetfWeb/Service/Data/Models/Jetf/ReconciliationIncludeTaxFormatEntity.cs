using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式。
    /// </summary>
    [Table("ReconciliationIncludeTaxFormat", Schema = "dbo")]
    public sealed class ReconciliationIncludeTaxFormatEntity
    {
        /// <summary>
        /// 格式識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 格式名稱。
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("FormatName")]
        public string FormatName { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 最後修改時間。
        /// </summary>
        [Column("UpdatedDate")]
        public DateTime UpdatedDate { get; set; }

        /// <summary>
        /// 格式欄位設定。
        /// </summary>
        [InverseProperty(nameof(ReconciliationIncludeTaxFormatColumnEntity.Format))]
        public ICollection<ReconciliationIncludeTaxFormatColumnEntity> Columns { get; set; }
    }
}
