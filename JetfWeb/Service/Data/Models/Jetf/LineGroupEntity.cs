using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// LINE 群組設定。
    /// </summary>
    [Table("LineGroup", Schema = "dbo")]
    public sealed class LineGroupEntity
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
        /// 群組推播權杖。
        /// </summary>
        [Column("Token")]
        public string Token { get; set; }

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