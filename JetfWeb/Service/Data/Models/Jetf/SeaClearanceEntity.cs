using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 匯入主檔。
    /// </summary>
    [Table("SeaClearance", Schema = "dbo")]
    public sealed class SeaClearanceEntity
    {
        /// <summary>
        /// 主檔識別碼。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 匯入檔名。
        /// </summary>
        [Column("FileName")]
        public string FileName { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        [Column("UploadOpe")]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}