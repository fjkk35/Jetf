using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 費用主檔明細金額修改紀錄。
    /// </summary>
    [Table("FEE_MASTER_DETAIL_MODIFY_LOG", Schema = "dbo")]
    public sealed class FeeMasterDetailModifyLogEntity
    {
        /// <summary>
        /// 修改紀錄識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>
        /// 對應的費用主檔明細識別碼。
        /// </summary>
        [Column("FEE_MASTER_DETAIL_ID")]
        public int FeeMasterDetailId { get; set; }

        /// <summary>
        /// 修改的金額欄位名稱。
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("FIELD_NAME")]
        public string FieldName { get; set; }

        /// <summary>
        /// 修改前金額。
        /// </summary>
        [Column("OLD_VALUE")]
        public int OldValue { get; set; }

        /// <summary>
        /// 修改後金額。
        /// </summary>
        [Column("NEW_VALUE")]
        public int NewValue { get; set; }

        /// <summary>
        /// 修改人員。
        /// </summary>
        [Required]
        [StringLength(10)]
        [Column("MODIFIED_USER_ID")]
        public string ModifiedUserId { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("MODIFIED_TIME")]
        public DateTime ModifiedTime { get; set; }
    }
}
