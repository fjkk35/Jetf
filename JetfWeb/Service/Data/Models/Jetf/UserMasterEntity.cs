using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 系統使用者主檔。
    /// </summary>
    [Table("USER_MASTER", Schema = "dbo")]
    public sealed class UserMasterEntity
    {
        /// <summary>
        /// 使用者帳號。
        /// </summary>
        [Key]
        [Column("USER_ID")]
        public string UserId { get; set; }

        /// <summary>
        /// 使用者密碼或雜湊值。
        /// </summary>
        [Column("USER_PASSWORD")]
        public string UserPassword { get; set; }

        /// <summary>
        /// 使用者名稱。
        /// </summary>
        [Column("USER_NAME")]
        public string UserName { get; set; }

        /// <summary>
        /// 使用者狀態。
        /// </summary>
        [Column("USER_STATUS")]
        public string UserStatus { get; set; }

        /// <summary>
        /// 舊版單一權限群組主鍵。
        /// </summary>
        [Column("AuthorityGroupId")]
        public int? AuthorityGroupId { get; set; }

        /// <summary>
        /// 最後更新人員。
        /// </summary>
        [Column("UPD_OPE")]
        public string UpdateOperator { get; set; }

        /// <summary>
        /// 最後更新時間。
        /// </summary>
        [Column("UPD_TIME")]
        public DateTime? UpdateTime { get; set; }
    }
}