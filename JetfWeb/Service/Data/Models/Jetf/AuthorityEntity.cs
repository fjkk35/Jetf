using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 系統權限定義主檔。
    /// </summary>
    [Table("Authority", Schema = "dbo")]
    public sealed class AuthorityEntity
    {
        /// <summary>
        /// 權限識別碼。
        /// </summary>
        [Key]
        [Column("Id")]
        public string Id { get; set; }

        /// <summary>
        /// 權限名稱。
        /// </summary>
        [Column("Text")]
        public string Text { get; set; }

        /// <summary>
        /// 權限所屬模組代碼。
        /// </summary>
        [Column("PartnerId")]
        public string PartnerId { get; set; }

        /// <summary>
        /// 顯示排序。
        /// </summary>
        [Column("Sort")]
        public int? Sort { get; set; }
    }
}