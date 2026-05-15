using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 流程步驟主檔。
    /// </summary>
    [Table("Step", Schema = "dbo")]
    public sealed class StepEntity
    {
        /// <summary>
        /// 步驟主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 步驟名稱。
        /// </summary>
        [Column("StepName")]
        public string StepName { get; set; }

        /// <summary>
        /// 下一步驟主鍵。
        /// </summary>
        [Column("NextStepId")]
        public int? NextStepId { get; set; }

        /// <summary>
        /// 條件類型。
        /// </summary>
        [Column("ConditionType")]
        public byte? ConditionType { get; set; }

        /// <summary>
        /// 是否可同時選擇多項。
        /// </summary>
        [Column("IsMultiple")]
        public bool? IsMultiple { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}