using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 費用主檔客戶調整上傳紀錄。
    /// </summary>
    [Table("FEE_MASTER_CUSTOMER_MODIFY", Schema = "dbo")]
    public sealed class FeeMasterCustomerModifyEntity
    {
        /// <summary>
        /// 紀錄識別碼。
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
        /// 物流貨號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("DlvInv")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 調整後的客戶代號。
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("NewCustomerCode")]
        public string NewCustomerCode { get; set; }

        /// <summary>
        /// 是否更新成功。
        /// </summary>
        [Column("IsSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 建立紀錄的使用者代號。
        /// </summary>
        [Required]
        [StringLength(10)]
        [Column("CreatedUserId")]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
