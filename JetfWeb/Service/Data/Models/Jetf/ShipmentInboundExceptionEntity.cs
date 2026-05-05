using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// Jetf 貨件異常資料。
    /// </summary>
    [Table("ShipmentInboundException", Schema = "dbo")]
    public class ShipmentInboundExceptionEntity
    {
        /// <summary>
        /// 異常資料主鍵。
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
        /// 對應的流水編號。
        /// </summary>
        [Column("SeqNo")]
        public string SeqNo { get; set; }

        /// <summary>
        /// 異常原因。
        /// </summary>
        [Column("ExceptionReasonId")]
        public int? ExceptionReasonId { get; set; }

        [ForeignKey(nameof(ExceptionReasonId))]
        public virtual ShipmentInboundExceptionReasonEntity ExceptionReason { get; set; }

        /// <summary>
        /// 異常圖片檔案路徑。
        /// </summary>
        [Column("FilePath")]
        public string FilePath { get; set; }

        /// <summary>
        /// 上傳操作人員。
        /// </summary>
        [Column("UploadOpe")]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime? CreatedTime { get; set; }
    }
}
