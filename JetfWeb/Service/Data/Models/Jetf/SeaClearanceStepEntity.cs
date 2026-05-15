using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細步驟歷程。
    /// </summary>
    [Table("SeaClearanceStep", Schema = "dbo")]
    public sealed class SeaClearanceStepEntity
    {
        /// <summary>
        /// 步驟歷程主鍵。
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
        /// 步驟主鍵。
        /// </summary>
        [Column("StepId")]
        public int? StepId { get; set; }

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