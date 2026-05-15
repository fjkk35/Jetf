using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細備註紀錄。
    /// </summary>
    [Table("SeaClearanceRemark", Schema = "dbo")]
    public sealed class SeaClearanceRemarkEntity
    {
        /// <summary>
        /// 備註主鍵。
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
        /// 備註內容。
        /// </summary>
        [Column("Remark")]
        public string Remark { get; set; }

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