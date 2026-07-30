using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 費用主檔物流貨號調整紀錄。
    /// </summary>
    [Table("FEE_MASTER_DLVINV_MODIFY", Schema = "dbo")]
    public sealed class FeeMasterDlvInvModifyEntity
    {
        /// <summary>
        /// 主鍵識別碼。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 舊物流貨號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("OldDlvInv")]
        public string OldDlvInv { get; set; }

        /// <summary>
        /// 新物流貨號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("NewDlvInv")]
        public string NewDlvInv { get; set; }

        /// <summary>
        /// 是否更新成功。
        /// </summary>
        [Column("IsSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        [Required]
        [StringLength(10)]
        [Column("CreatedUserId")]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
