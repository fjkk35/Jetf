using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 異常訂單發送批次主檔。
    /// </summary>
    [Table("ErrorOrderSend", Schema = "dbo")]
    public sealed class ErrorOrderSendEntity
    {
        /// <summary>
        /// 批次主鍵。
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
        /// 匯入檔案路徑。
        /// </summary>
        [Column("FilePath")]
        public string FilePath { get; set; }

        /// <summary>
        /// 總筆數。
        /// </summary>
        [Column("TotalCount")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// 簡訊筆數。
        /// </summary>
        [Column("PhoneCount")]
        public int? PhoneCount { get; set; }

        /// <summary>
        /// LINE 筆數。
        /// </summary>
        [Column("LineCount")]
        public int? LineCount { get; set; }

        /// <summary>
        /// 是否已發送。
        /// </summary>
        [Column("IsSend")]
        public string IsSend { get; set; }

        /// <summary>
        /// 發送人員。
        /// </summary>
        [Column("SendOpe")]
        public string SendOpe { get; set; }

        /// <summary>
        /// 發送時間。
        /// </summary>
        [Column("SendDateTime")]
        public DateTime? SendDateTime { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        [Column("UploadOpe")]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 是否刪除。
        /// </summary>
        [Column("IsDelete")]
        public bool? IsDelete { get; set; }

        /// <summary>
        /// 刪除時間。
        /// </summary>
        [Column("DeleteDateTime")]
        public DateTime? DeleteDateTime { get; set; }

        /// <summary>
        /// 刪除人員。
        /// </summary>
        [Column("DeleteOpe")]
        public string DeleteOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CrtDateTime")]
        public DateTime? CrtDateTime { get; set; }
    }
}