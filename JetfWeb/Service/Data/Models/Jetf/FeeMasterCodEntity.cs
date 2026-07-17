using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// JETF 空運及海運到付款彙整資料。
    /// </summary>
    [Table("FEE_MASTER_COD", Schema = "dbo")]
    public sealed class FeeMasterCodEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public long Id { get; set; }

        /// <summary>
        /// 資料來源類型，AIR 表示空運，SEA 表示海運。
        /// </summary>
        [Required]
        [StringLength(3)]
        [Column("SOURCE_TYPE")]
        public string SourceType { get; set; }

        /// <summary>
        /// 原始清關資料類型。
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("DATA_TYPE")]
        public string DataType { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 空運袋號或海運分提單號。
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("BAGNO")]
        public string BagNo { get; set; }

        /// <summary>
        /// 空運追蹤號；海運與袋號相同。
        /// </summary>
        [StringLength(100)]
        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 空運配送單號或海運物流貨號。
        /// </summary>
        [StringLength(100)]
        [Column("JETF_SERIAL")]
        public string JetfSerial { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("CC")]
        public decimal Cc { get; set; }

        /// <summary>
        /// 出倉時間。
        /// </summary>
        [Column("SIGN_OUT_TIME")]
        public DateTime SignOutTime { get; set; }

        /// <summary>
        /// 資料建立時間。
        /// </summary>
        [Column("CREATED_TIME")]
        public DateTime CreatedTime { get; set; }
    }
}
