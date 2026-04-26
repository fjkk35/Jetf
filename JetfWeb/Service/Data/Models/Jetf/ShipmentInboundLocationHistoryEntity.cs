using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 貨件儲位異動紀錄。
    /// </summary>
    [Table("ShipmentInboundLocationHistory", Schema = "dbo")]
    public sealed class ShipmentInboundLocationHistoryEntity
    {
        /// <summary>
        /// 儲位異動紀錄主鍵。
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
        /// 原儲位代碼。
        /// </summary>
        [Column("OldLocationCode")]
        public string OldLocationCode { get; set; }

        /// <summary>
        /// 新儲位代碼。
        /// </summary>
        [Column("NewLocationCode")]
        public string NewLocationCode { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CreatedOpe")]
        public string CreatedOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
