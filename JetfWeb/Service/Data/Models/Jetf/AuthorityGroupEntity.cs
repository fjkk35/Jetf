using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 權限群組主檔。
    /// </summary>
    [Table("AuthorityGroup", Schema = "dbo")]
    public sealed class AuthorityGroupEntity
    {
        /// <summary>
        /// 權限群組主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 群組名稱。
        /// </summary>
        [Column("GroupName")]
        public string GroupName { get; set; }

        /// <summary>
        /// 群組備註。
        /// </summary>
        [Column("Memo")]
        public string Memo { get; set; }
    }
}