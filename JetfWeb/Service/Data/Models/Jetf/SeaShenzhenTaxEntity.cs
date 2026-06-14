using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Service.EnumTax;

namespace Service.Data
{
    /// <summary>
    /// 新遞深圳稅單上傳資料。
    /// </summary>
    [Table("SeaShenzhenTax", Schema = "dbo")]
    public sealed class SeaShenzhenTaxEntity
    {
        /// <summary>
        /// 主鍵。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// 資料日期。
        /// </summary>
        [Column("DataDate")]
        public string DataDate { get; set; }

        /// <summary>
        /// 資料類型。
        /// </summary>
        [Column("DataType")]
        public SeaShenzhenTaxDataType DataType { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MainNumber")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 報單號碼。
        /// </summary>
        [Column("ClearanceNumber")]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 分號。
        /// </summary>
        [Column("TrackingNo")]
        public string TrackingNo { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TaxNumber")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅單金額。
        /// </summary>
        [Column("Tax")]
        public int Tax { get; set; }

        /// <summary>
        /// 納稅人。
        /// </summary>
        [Column("TaxPayer")]
        public string TaxPayer { get; set; }

        /// <summary>
        /// 統編。
        /// </summary>
        [Column("TaxRecId")]
        public string TaxRecId { get; set; }

        /// <summary>
        /// 深圳稅金轉檔主檔 Id。
        /// </summary>
        [Column("ShenzhenFeeMasterId")]
        public int? ShenzhenFeeMasterId { get; set; }

        /// <summary>
        /// 建立人員。
        /// </summary>
        [Column("CreatedUser")]
        public string CreatedUser { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }
    }
}
