using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 異常狀態明細對應。
    /// </summary>
    [Table("SeaClearanceAbnormalStateDetail", Schema = "dbo")]
    public sealed class SeaClearanceAbnormalStateDetailEntity
    {
        /// <summary>
        /// 對應主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 異常狀態主鍵。
        /// </summary>
        [Column("SeaClearanceAbnormalStateId")]
        public int? SeaClearanceAbnormalStateId { get; set; }

        /// <summary>
        /// 異常狀態明細主鍵。
        /// </summary>
        [Column("AbnormalStateDetailId")]
        public int? AbnormalStateDetailId { get; set; }
    }
}