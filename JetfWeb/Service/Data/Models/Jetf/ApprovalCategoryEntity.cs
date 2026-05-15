using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 簽審類別主檔。
    /// </summary>
    [Table("ApprovalCategory", Schema = "dbo")]
    public sealed class ApprovalCategoryEntity
    {
        /// <summary>
        /// 簽審類別主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 簽審類別名稱。
        /// </summary>
        [Required]
        [Column("CategoryName")]
        public string CategoryName { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}