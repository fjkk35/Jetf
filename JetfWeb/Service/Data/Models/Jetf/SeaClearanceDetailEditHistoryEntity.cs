using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// SeaClearance 明細欄位異動歷程。
    /// </summary>
    [Table("SeaClearanceDetailEditHistory", Schema = "dbo")]
    public sealed class SeaClearanceDetailEditHistoryEntity
    {
        /// <summary>
        /// 歷程主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// SeaClearance 明細主鍵。
        /// </summary>
        [Column("SeaClearanceDetailId")]
        public int? SeaClearanceDetailId { get; set; }

        /// <summary>
        /// 欄位名稱。
        /// </summary>
        [Column("FieldName")]
        public string FieldName { get; set; }

        /// <summary>
        /// 舊值。
        /// </summary>
        [Column("OldValue")]
        public string OldValue { get; set; }

        /// <summary>
        /// 新值。
        /// </summary>
        [Column("NewValue")]
        public string NewValue { get; set; }

        /// <summary>
        /// 編輯備註。
        /// </summary>
        [Column("Memo")]
        public string Memo { get; set; }

        /// <summary>
        /// 編輯時間。
        /// </summary>
        [Column("EditTime")]
        public DateTime? EditTime { get; set; }

        /// <summary>
        /// 編輯人員。
        /// </summary>
        [Column("EditUser")]
        public string EditUser { get; set; }
    }
}