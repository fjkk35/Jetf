using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 步驟明細對應。
    /// </summary>
    [Table("SeaClearanceStepDetail", Schema = "dbo")]
    public sealed class SeaClearanceStepDetailEntity
    {
        /// <summary>
        /// 對應主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 步驟主鍵。
        /// </summary>
        [Column("SeaClearanceStepId")]
        public int? SeaClearanceStepId { get; set; }

        /// <summary>
        /// 步驟明細主鍵。
        /// </summary>
        [Column("StepDetailId")]
        public int? StepDetailId { get; set; }
    }
}