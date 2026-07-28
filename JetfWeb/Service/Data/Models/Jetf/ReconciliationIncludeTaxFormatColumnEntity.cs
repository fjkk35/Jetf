using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Service.EnumTax;

namespace Service.Data
{
    /// <summary>
    /// 包稅客戶 Excel 匯出格式欄位。
    /// </summary>
    [Table("ReconciliationIncludeTaxFormatColumn", Schema = "dbo")]
    public sealed class ReconciliationIncludeTaxFormatColumnEntity
    {
        /// <summary>
        /// 欄位設定識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 所屬格式識別碼。
        /// </summary>
        [Column("FormatId")]
        [ForeignKey(nameof(Format))]
        public int FormatId { get; set; }

        /// <summary>
        /// 匯出欄位排序，數字越小越前面。
        /// </summary>
        [Column("SortOrder")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 匯出欄位名稱。
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("ColumnName")]
        public string ColumnName { get; set; }

        /// <summary>
        /// 欄位資料來源類型。
        /// </summary>
        [Column("SourceType")]
        public ReconciliationIncludeTaxColumnSourceType SourceType { get; set; }

        /// <summary>
        /// 對應的資料欄位代碼；固定值欄位為空白。
        /// </summary>
        [StringLength(50)]
        [Column("FieldKey")]
        public string FieldKey { get; set; }

        /// <summary>
        /// 固定值欄位的匯出內容；資料欄位可為空白。
        /// </summary>
        [StringLength(200)]
        [Column("DefaultValue")]
        public string DefaultValue { get; set; }

        /// <summary>
        /// 所屬格式。
        /// </summary>
        public ReconciliationIncludeTaxFormatEntity Format { get; set; }
    }
}
