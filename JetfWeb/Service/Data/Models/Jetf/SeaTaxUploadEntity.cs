using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// 海運稅金上傳資料。
    /// </summary>
    [Table("SEA_TAX_UPLOAD", Schema = "dbo")]
    public sealed class SeaTaxUploadEntity
    {
        /// <summary>
        /// 主號。
        /// </summary>
        [Key]
        [Column("MAIN_NUMBER", Order = 0)]
        public string MainNumber { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Key]
        [Column("CLEARANCE_NUMBER", Order = 1)]
        public string ClearanceNumber { get; set; }

        /// <summary>
        /// 報關類型。
        /// </summary>
        [Column("CLEARANCE_TYPE")]
        public string ClearanceType { get; set; }

        /// <summary>
        /// 分提單號。
        /// </summary>
        [Key]
        [Column("BL_NO", Order = 2)]
        public string BlNo { get; set; }

        /// <summary>
        /// 註冊號碼。
        /// </summary>
        [Column("REG_NO")]
        public string RegNo { get; set; }

        /// <summary>
        /// 艙單號碼。
        /// </summary>
        [Column("MAINFEST")]
        public string Mainfest { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Key]
        [Column("TAX_NUMBER", Order = 3)]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 稅金金額。
        /// </summary>
        [Key]
        [Column("TAX", Order = 4)]
        public string Tax { get; set; }

        /// <summary>
        /// 列印時間。
        /// </summary>
        [Column("PRT_TIME")]
        public DateTime? PrtTime { get; set; }

        /// <summary>
        /// 上傳時間。
        /// </summary>
        [Key]
        [Column("UPLOAD_TIME", Order = 5)]
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// 上傳人員。
        /// </summary>
        [Key]
        [Column("UPLOAD_OPE", Order = 6)]
        public string UploadOpe { get; set; }

        /// <summary>
        /// 納稅義務人。
        /// </summary>
        [Column("TAX_PAYER")]
        public string TaxPayer { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        [Column("TAX_RECID")]
        public string TaxRecId { get; set; }
    }
}