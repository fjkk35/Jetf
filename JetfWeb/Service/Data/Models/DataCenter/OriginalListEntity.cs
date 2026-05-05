using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// DATA_CENTER 原始貨件清單資料。
    /// </summary>
    [Table("ORIGINALLIST", Schema = "dbo")]
    public sealed class OriginalListEntity
    {
        [Key]
        [Column("ROW_ID")]
        public int Id { get; set; }

        /// <summary>
        /// 主號
        /// </summary>
        [Column("MAINNUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("TRACKINGNO")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 派送單號。
        /// </summary>
        [Column("DELIVERYNO")]
        public string DeliveryNo { get; set; }

        /// <summary>
        /// 收件人姓名。
        /// </summary>
        [Column("RECIPIENT")]
        public string Importer { get; set; }

        /// <summary>
        /// 收件人電話。
        /// </summary>
        [Column("RECPHONE")]
        public string ImporterPhone { get; set; }

        /// <summary>
        /// 收件人地址。
        /// </summary>
        [Column("RECADDRESS")]
        public string ImporterAddr { get; set; }

        /// <summary>
        /// 客戶代碼。
        /// </summary>
        [Column("DESPATCHNO")]
        public string CustCode { get; set; }

        /// <summary>
        /// 清關倉或派件代碼。
        /// </summary>
        [Column("CLEARANCEWAREHOUSING")]
        public int TransNo { get; set; }

        /// <summary>
        /// 到付款。
        /// </summary>
        [Column("CC")]
        public string CC { get; set; }
    }
}
