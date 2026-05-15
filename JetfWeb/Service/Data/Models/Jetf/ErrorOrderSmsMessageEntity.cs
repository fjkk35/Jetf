using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 異常訂單簡訊模板。
    /// </summary>
    [Table("ErrorOrderSmsMessage", Schema = "dbo")]
    public sealed class ErrorOrderSmsMessageEntity
    {
        /// <summary>
        /// 模板主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 模板名稱。
        /// </summary>
        [Column("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 模板內容。
        /// </summary>
        [Column("Content")]
        public string Content { get; set; }

        /// <summary>
        /// 最後編輯人員。
        /// </summary>
        [Column("EditOpe")]
        public string EditOpe { get; set; }

        /// <summary>
        /// 最後編輯時間。
        /// </summary>
        [Column("EditDateTime")]
        public DateTime? EditDateTime { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}