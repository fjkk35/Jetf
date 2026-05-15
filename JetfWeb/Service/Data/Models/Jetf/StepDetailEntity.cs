using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 流程步驟明細主檔。
    /// </summary>
    [Table("StepDetail", Schema = "dbo")]
    public sealed class StepDetailEntity
    {
        /// <summary>
        /// 步驟明細主鍵。
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
        /// 步驟明細名稱。
        /// </summary>
        [Column("StepDetailName")]
        public string StepDetailName { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}