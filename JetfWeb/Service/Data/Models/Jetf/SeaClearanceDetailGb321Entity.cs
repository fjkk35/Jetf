using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細 GB321 歷程。
    /// </summary>
    [Table("SeaClearanceDetailGb321", Schema = "dbo")]
    public sealed class SeaClearanceDetailGb321Entity
    {
        /// <summary>
        /// 歷程主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 明細主鍵。
        /// </summary>
        [Column("SeaClearanceDetailId")]
        public int? SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 處理時間。
        /// </summary>
        [Column("ProDateTime")]
        public DateTime? ProDateTime { get; set; }

        /// <summary>
        /// 處理類型。
        /// </summary>
        [Column("ProType")]
        public string ProType { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreateTime")]
        public DateTime? CreateTime { get; set; }
    }
}