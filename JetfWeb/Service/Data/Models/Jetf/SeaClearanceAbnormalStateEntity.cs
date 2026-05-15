using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細異常狀態紀錄。
    /// </summary>
    [Table("SeaClearanceAbnormalState", Schema = "dbo")]
    public sealed class SeaClearanceAbnormalStateEntity
    {
        /// <summary>
        /// 異常狀態紀錄主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("DataDate")]
        public DateTime? DataDate { get; set; }

        /// <summary>
        /// SeaClearance 明細主鍵。
        /// </summary>
        [Column("SeaClearanceDetailId")]
        public int? SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 異常狀態主鍵。
        /// </summary>
        [Column("AbnormalStateId")]
        public int? AbnormalStateId { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CrtUser")]
        public string CrtUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreateTime")]
        public DateTime? CreateTime { get; set; }
    }
}