using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 空快代收銷帳資料。
    /// </summary>
    [Table("ReconciliationAir", Schema = "dbo")]
    public sealed class ReconciliationAirEntity
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 類型（FTZ / TACT）。
        /// </summary>
        [Column("Type")]
        public string Type { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MainNumber")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        [Column("Recipient")]
        public string Recipient { get; set; }

        /// <summary>
        /// 納稅義務人統一編號。
        /// </summary>
        [Column("TaxRecId")]
        public string TaxRecId { get; set; }

        /// <summary>
        /// 營業稅基。
        /// </summary>
        [Column("TaxBase")]
        public int TaxBase { get; set; }

        /// <summary>
        /// 稅費金額。
        /// </summary>
        [Column("Tax")]
        public int Tax { get; set; }

        /// <summary>
        /// 進口稅。
        /// </summary>
        [Column("ImportTax")]
        public int? ImportTax { get; set; }

        /// <summary>
        /// 營業稅。
        /// </summary>
        [Column("BusinessTax")]
        public int? BusinessTax { get; set; }

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
