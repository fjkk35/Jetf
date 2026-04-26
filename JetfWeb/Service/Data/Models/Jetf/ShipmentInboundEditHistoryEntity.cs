using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 貨件欄位編輯紀錄。
    /// </summary>
    [Table("ShipmentInboundEditHistory", Schema = "dbo")]
    public sealed class ShipmentInboundEditHistoryEntity
    {
        /// <summary>
        /// 編輯紀錄主鍵。
        /// </summary>
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 對應的入庫資料 Id。
        /// </summary>
        [Column("ShipmentInboundId")]
        public int ShipmentInboundId { get; set; }

        /// <summary>
        /// 被修改的欄位名稱。
        /// </summary>
        [Column("FieldName")]
        public string FieldName { get; set; }

        /// <summary>
        /// 修改前的值。
        /// </summary>
        [Column("OldValue")]
        public string OldValue { get; set; }

        /// <summary>
        /// 修改後的值。
        /// </summary>
        [Column("NewValue")]
        public string NewValue { get; set; }

        /// <summary>
        /// 編輯時間。
        /// </summary>
        [Column("EditTime")]
        public DateTime EditTime { get; set; }

        /// <summary>
        /// 編輯人員。
        /// </summary>
        [Column("EditUser")]
        public string EditUser { get; set; }
    }
}
