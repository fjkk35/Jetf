using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細 GB301 歷程。
    /// </summary>
    [Table("SeaClearanceDetailGb301", Schema = "dbo")]
    public sealed class SeaClearanceDetailGb301Entity
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
        /// 流程事件代碼字串。
        /// </summary>
        [Column("ProcEventCodeStr")]
        public string ProcEventCodeStr { get; set; }

        /// <summary>
        /// 流程描述。
        /// </summary>
        [Column("ProgDesc")]
        public string ProgDesc { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreateTime")]
        public DateTime? CreateTime { get; set; }
    }
}