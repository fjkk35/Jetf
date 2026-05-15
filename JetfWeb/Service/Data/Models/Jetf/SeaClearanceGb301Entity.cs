using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance GB301 放行條件紀錄。
    /// </summary>
    [Table("SeaClearanceGb301", Schema = "dbo")]
    public sealed class SeaClearanceGb301Entity
    {
        /// <summary>
        /// 紀錄主鍵。
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
        /// 放行條件代碼。
        /// </summary>
        [Column("RelCondCd")]
        public string RelCondCd { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreateTime")]
        public DateTime? CreateTime { get; set; }
    }
}