using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 步驟條件設定。
    /// </summary>
    [Table("StepCondition", Schema = "dbo")]
    public sealed class StepConditionEntity
    {
        /// <summary>
        /// 條件主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 步驟主鍵。
        /// </summary>
        [Column("StepId")]
        public int? StepId { get; set; }

        /// <summary>
        /// 必要步驟明細主鍵。
        /// </summary>
        [Column("RequiredStepDetailId")]
        public int? RequiredStepDetailId { get; set; }
    }
}