using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 使用者與權限群組對應資料。
    /// </summary>
    [Table("UserAuthorityGroup", Schema = "dbo")]
    public sealed class UserAuthorityGroupEntity
    {
        /// <summary>
        /// 對應資料主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 使用者帳號。
        /// </summary>
        [Required]
        [Column("UserId")]
        public string UserId { get; set; }

        /// <summary>
        /// 權限群組主鍵。
        /// </summary>
        [Column("AuthorityGroupId")]
        public int AuthorityGroupId { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}