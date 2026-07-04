using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 代收銷帳發票資料。
    /// </summary>
    [Table("ReconciliationInvoice", Schema = "dbo")]
    public sealed class ReconciliationInvoiceEntity
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 發票類別。
        /// </summary>
        [Column("Type")]
        public string Type { get; set; }

        /// <summary>
        /// 開立日期。
        /// </summary>
        [Column("Date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// 發票號碼。
        /// </summary>
        [Column("Invoice")]
        public string Invoice { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 物流貨號。
        /// </summary>
        [Column("DlvInv")]
        public string DlvInv { get; set; }

        /// <summary>
        /// 修改人員。
        /// </summary>
        [Column("UpdatedOpe")]
        public string UpdatedOpe { get; set; }

        /// <summary>
        /// 修改時間。
        /// </summary>
        [Column("UpdatedTime")]
        public DateTime? UpdatedTime { get; set; }

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