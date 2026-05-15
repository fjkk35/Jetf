using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Telegram 群組設定。
    /// </summary>
    [Table("TelegramGroup", Schema = "dbo")]
    public sealed class TelegramGroupEntity
    {
        /// <summary>
        /// 群組識別碼。
        /// </summary>
        [Key]
        [Column("GroupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// 群組名稱。
        /// </summary>
        [Column("GroupName")]
        public string GroupName { get; set; }

        /// <summary>
        /// Telegram Chat Id。
        /// </summary>
        [Column("ChatId")]
        public string ChatId { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CrtUser")]
        public string CrtUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}