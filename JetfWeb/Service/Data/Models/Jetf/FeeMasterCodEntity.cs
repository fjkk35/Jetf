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
        public int Id { get; set; }

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
        /// 客戶代碼。
        /// </summary>
        [StringLength(20)]
        [Column("CUSTOMER")]
        public string Customer { get; set; }

        /// <summary>
        /// 空運袋號或海運分提單號。
        /// </summary>
        [StringLength(100)]
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

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
        [Column("DLV_INV")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 到付款金額。
        /// </summary>
        [Column("CC")]
        public decimal Cc { get; set; }

        /// <summary>
        /// 重出運費。
        /// </summary>
        [Column("FreightFee")]
        public int? FreightFee { get; set; }

        /// <summary>
        /// 重出手續費。
        /// </summary>
        [Column("Fee")]
        public int? Fee { get; set; }

        /// <summary>
        /// 應向物流代收金額。
        /// </summary>
        [Column("ToDlvCod")]
        public int? ToDlvCod { get; set; }

        /// <summary>
        /// 是否由貨件入庫出庫流程建立。
        /// </summary>
        [Column("IsShipmentInbound")]
        public bool IsShipmentInbound { get; set; }

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

        /// <summary>
        /// 已向物流公司收回的到付款金額。
        /// </summary>
        [Column("RECEIVED_CC")]
        public int? ReceivedCc { get; set; }

        /// <summary>
        /// 向物流公司收回到付款金額的時間。
        /// </summary>
        [Column("RECEIVED_CC_TIME")]
        public DateTime? ReceivedCcTime { get; set; }

        /// <summary>
        /// 到付款銷帳操作人員。
        /// </summary>
        [StringLength(10)]
        [Column("RECEIVED_CC_USERID")]
        public string ReceivedCcUserId { get; set; }

        /// <summary>
        /// 對應的物流銷帳紀錄識別碼。
        /// </summary>
        [Column("ReconciliationLogisticsId")]
        [ForeignKey(nameof(ReconciliationLogistics))]
        public int? ReconciliationLogisticsId { get; set; }

        /// <summary>
        /// 對應的物流銷帳紀錄。
        /// </summary>
        public ReconciliationLogisticsEntity ReconciliationLogistics { get; set; }
    }
}
