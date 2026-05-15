using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 授權文件主檔。
    /// </summary>
    [Table("AuthorizationForm", Schema = "dbo")]
    public sealed class AuthorizationFormEntity
    {
        /// <summary>
        /// 授權文件主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 文件名稱。
        /// </summary>
        [Required]
        [Column("FormName")]
        public string FormName { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}