using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Service.Data
{
    /// <summary>
    /// TACT 稅單資料。
    /// </summary>
    [Table("ETL_TACT_TAX", Schema = "dbo")]
    public sealed class EtlTactTaxEntity
    {
        /// <summary>
        /// 資料流水號。
        /// </summary>
        [Key]
        [Column("ROW_ID")]
        public int RowId { get; set; }

        /// <summary>
        /// 進口日期。
        /// </summary>
        [Column("IMPORT_DATE")]
        public string ImportDate { get; set; }

        /// <summary>
        /// 清關類型。
        /// </summary>
        [Column("CLEARANCE_TYPE")]
        public string ClearanceType { get; set; }

        /// <summary>
        /// 清關單號。
        /// </summary>
        [Column("CLEARANCE_NO")]
        public string ClearanceNo { get; set; }

        /// <summary>
        /// 主號。
        /// </summary>
        [Column("MAIN_NUMBER")]
        public string MainNumber { get; set; }

        /// <summary>
        /// 袋號。
        /// </summary>
        [Column("BAG_NUMBER")]
        public string BagNumber { get; set; }

        /// <summary>
        /// 稅單號碼。
        /// </summary>
        [Column("TAX_NUMBER")]
        public string TaxNumber { get; set; }

        /// <summary>
        /// 納稅義務人證號。
        /// </summary>
        [Column("TAXPAYER_ID")]
        public string TaxpayerId { get; set; }

        /// <summary>
        /// 納稅義務人名稱。
        /// </summary>
        [Column("TAXPAYER")]
        public string Taxpayer { get; set; }

        /// <summary>
        /// 稅別代碼一。
        /// </summary>
        [Column("TAX1_CODE")]
        public string Tax1Code { get; set; }

        /// <summary>
        /// 稅額一。
        /// </summary>
        [Column("TAX1_FEE")]
        public int? Tax1Fee { get; set; }

        /// <summary>
        /// 稅別代碼二。
        /// </summary>
        [Column("TAX2_CODE")]
        public string Tax2Code { get; set; }

        /// <summary>
        /// 稅額二。
        /// </summary>
        [Column("TAX2_FEE")]
        public int? Tax2Fee { get; set; }

        /// <summary>
        /// 稅別代碼三。
        /// </summary>
        [Column("TAX3_CODE")]
        public string Tax3Code { get; set; }

        /// <summary>
        /// 稅額三。
        /// </summary>
        [Column("TAX3_FEE")]
        public int? Tax3Fee { get; set; }

        /// <summary>
        /// 稅別代碼四。
        /// </summary>
        [Column("TAX4_CODE")]
        public string Tax4Code { get; set; }

        /// <summary>
        /// 稅額四。
        /// </summary>
        [Column("TAX4_FEE")]
        public int? Tax4Fee { get; set; }

        /// <summary>
        /// 稅別代碼五。
        /// </summary>
        [Column("TAX5_CODE")]
        public string Tax5Code { get; set; }

        /// <summary>
        /// 稅額五。
        /// </summary>
        [Column("TAX5_FEE")]
        public int? Tax5Fee { get; set; }

        /// <summary>
        /// 稅別代碼六。
        /// </summary>
        [Column("TAX6_CODE")]
        public string Tax6Code { get; set; }

        /// <summary>
        /// 稅額六。
        /// </summary>
        [Column("TAX6_FEE")]
        public int? Tax6Fee { get; set; }

        /// <summary>
        /// 稅額總計。
        /// </summary>
        [Column("TAX_AMOUNT")]
        public int? TaxAmount { get; set; }

        /// <summary>
        /// 貨物名稱。
        /// </summary>
        [Column("CARGO_NAME")]
        public string CargoName { get; set; }

        /// <summary>
        /// 稅基。
        /// </summary>
        [Column("TAX_BASE")]
        public string TaxBase { get; set; }

        /// <summary>
        /// 狀態。
        /// </summary>
        [Column("STATUS")]
        public string Status { get; set; }

        /// <summary>
        /// 頻率註記。
        /// </summary>
        [Column("FREQ_SIGN")]
        public string FreqSign { get; set; }

        /// <summary>
        /// 來源檔名。
        /// </summary>
        [Column("SOURCE_FILE")]
        public string SourceFile { get; set; }

        /// <summary>
        /// 來源時間。
        /// </summary>
        [Column("SOURCE_TIME")]
        public DateTime SourceTime { get; set; }
    }
}